using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Reese.Common.Replayer;
using Reese.Content;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese.Common.Replay;

[Autoload(Side = ModSide.Client)]
public class Playback : ModSystem
{
    private const bool UseDeltaSectionsAfterSignOn = true;
    public static PlaybackSocket Socket => Netplay.Connection.Socket as PlaybackSocket;
    public static bool IsPlaying => Socket is { Closed: false };

    public override void Load()
    {
        if (UseDeltaSectionsAfterSignOn)
        {
            On_NetMessage.DecompressTileBlock_Inner += (orig, reader, xStart, yStart, width, height) =>
            {
                // during playback after fully spawned, let's ignore tiles within sections we already have (perform deltas).
                // if you do want any of these tiles to apply, make sure the section manager reflects that need
                // and mark the sections as not loaded.
                if (IsPlayingReplay(out ReplayFile replay)
                    && Netplay.Connection.State == 10
                    && replay.IsBaselining
                    && Main.sectionManager.TilesLoaded(xStart, yStart, xStart + width, yStart + height))
                    return;

                orig(reader, xStart, yStart, width, height);
            };
        }
    }

    public override void Unload()
    {
        // make sure we don't leave a ref to our socket lying around
        if (Socket != null)
        {
            Socket.Close();
            Netplay.Connection.Socket = new TcpSocket();
        }
    }

    public static void Start(ReplayFile replay)
    {
        var sock = new PlaybackSocket(replay);
        new Thread(PlaybackClientLoop)
        {
            Name = "Playback Thread",
            IsBackground = true
        }.Start(sock);
    }

    // FIXME: BIG STINKY HERE - we rely on Netplay.Disconnect and IsPlaying socket closed status, but those two fight each other on different threads!!
    public static void Stop()
    {
        if (!IsPlaying)
            return;

        Main.statusText = "Playback stopped";
        // if we want to go straight to title, we have to do a little more ourselves.
        SystemLoader.OnWorldUnload();
        Main.gameMenu = true;
        Main.SwitchNetMode(0);
        Player.ClearPlayerTempInfo();
        SoundEngine.StopTrackedSounds();
        Main.menuMode = MenuID.Title;

        // must be last.
        Netplay.Disconnect = true;
    }

    public static bool IsPlayingReplay(out ReplayFile replay)
    {
        if (IsPlayingReplay(out PlaybackSocket sock))
        {
            replay = sock.Replay;
            return true;
        }

        replay = null;
        return false;
    }

    public static bool IsPlayingReplay(out PlaybackSocket sock)
    {
        if (IsPlaying)
        {
            sock = Socket;
            return true;
        }

        sock = null;
        return false;
    }

    public static void Goto(uint tick)
    {
        if (!IsPlayingReplay(out PlaybackSocket sock))
            return;

        var pts = ModContent.GetInstance<PlaybackTimeScale>();
        var replay = sock.Replay;
        lock (replay)
        {
            var cache = replay.MetaBaselines.Cache;
            if (cache.Count == 0)
                return;

            // find the nearest baseline before the target tick.
            ReplayFile.MetaBlockFooterBaselines.Entry bl = default;
            foreach (var ent in replay.MetaBaselines.Cache)
            {
                if (ent.Tick <= tick)
                    bl = ent;
                else
                    break;
            }

            replay.Goto(bl);

            const int serverBufIdx = 256;
            // we could have interrupted a packet transfer! make sure we ignore whatever is currently sitting in there.
            // probably do not need the lock.
            var netBuf = NetMessage.buffer[serverBufIdx];
            lock (netBuf)
            {
                netBuf.totalData = 0;
                netBuf.checkBytes = false;
                netBuf.spamCount = 0;
            }

            if (UseDeltaSectionsAfterSignOn && Netplay.Connection.State == 10)
            {
                // make sure the client truly applies the sections that we anticipate from a baseline.
                for (var i = 0; i < Main.sectionManager.data.Length; i++)
                {
                    var flags = Main.sectionManager.data[i];
                    flags[WorldSections.BitIndex_SectionLoaded] = false;
                    flags[WorldSections.BitIndex_SectionFramed] = false;
                    Main.sectionManager.data[i] = flags;
                }
            }

            sock.Tick = (uint)bl.Tick;
            pts.FastForwardTicks = tick - (uint)bl.Tick;

            // TODO: move this code
            ReplayPlayback.ResetReplayState();
        }
    }

    private static void PlaybackClientLoop(object context)
    {
        var socket = (PlaybackSocket)context;
        Netplay.ClientLoopSetup(socket.GetRemoteAddress());
        Netplay.Connection.Socket = socket;
        Main.statusText = Loc.Get("MainMenu.ReplayStartup.Connecting");
        Main.menuMode = MenuID.MultiplayerJoining;

        try
        {
            Netplay.InnerClientLoop();
        }
        finally
        {
            // should have already been Disposed because the socket was Closed, but just in case.
            if (IsPlayingReplay(out ReplayFile replay))
            {
                lock (replay)
                    replay.Dispose();
            }
        }
    }

    public static IList<ReplayFile> EnumerateReplays(bool footerNow = true)
    {
        var replays = new List<ReplayFile>();

        var opts = new EnumerationOptions { IgnoreInaccessible = true };
        foreach (var path in Directory.EnumerateFiles(ReplayPaths.GetFolder(), "*.reese", opts))
        {
            FileStream fs;
            ReplayFile replay;

            try
            {
                fs = File.OpenRead(path);
            }
            catch (Exception e)
            {
                Log.Warn($"failed to open enumerated replay {path}: {e}");
                continue;
            }

            try
            {
                replay = ReplayFile.Read(fs, footerNow);
            }
            catch (Exception e)
            {
                Log.Warn($"failed to read enumerated replay {path}: {e}");
                continue;
            }

            replays.Add(replay);
        }

        return replays;
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (!IsPlaying)
            return false;

        if (messageType == MessageID.Kick)
        {
            var reason = NetworkText.Deserialize(reader);
            Main.NewText($"[i:{ModContent.ItemType<CameraItem>()}] Replay disconnected: {reason}", Color.Red);

            return true;
        }

        return false;
    }
}

public class PlaybackRemoteAddress : RemoteAddress
{
    public override string GetIdentifier() => "ReesePlayback";
    public override string GetFriendlyName() => "Reese Playback";
    public override bool IsLocalHost() => true;

    public override string ToString() => GetFriendlyName();
}

public class PlaybackSocket(ReplayFile replay) : ISocket
{
    private static readonly PlaybackRemoteAddress RemoteAddress = new();

    public uint Tick { get; set; }
    public bool Closed { get; private set; }

    public ReplayFile Replay => replay;

    public void Close()
    {
        if (Closed)
            return;

        lock (Replay)
        {
            Log.Info("Closing replay socket");
            Replay.Dispose();
        }

        Closed = true;
    }

    public bool IsConnected() => !Closed;

    public void Connect(RemoteAddress address)
    {
        Log.Info($"Replay connect to {address}");
    }

    public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
    {
        // Outgoing client packets (Hello, etc.) are discarded � we're in replay mode.
        // But we must invoke the callback or the client loop hangs waiting for confirmation.
        callback?.Invoke(state);
    }

    public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
    {
        try
        {
            ResetTimeoutTimer();
            var numberOfBytesRead = Replay.ReadData(data.AsSpan()[offset..(offset + size)]);
            callback(state, numberOfBytesRead);
        }
        finally
        {
            Monitor.Exit(Replay);
        }
    }

    /// <remarks>on a return value of <see langword="true"/>, <see cref="AsyncReceive"/> MUST be invoked.</remarks>
    public bool IsDataAvailable()
    {
        Monitor.Enter(Replay);
        ResetTimeoutTimer();

        if (Closed)
        {
            Monitor.Exit(Replay);
            return false;
        }

        if (Replay.IsDataBuffered)
        {
            // before we reach state 6 (player spawned), we always want data,
            // so sign on data and the first baseline is consumed properly.
            if (Netplay.Connection.State < 6)
                return true;

            var value = Tick >= Replay.Tick;
            if (!value) Monitor.Exit(Replay);

            return value;
        }

        Monitor.Exit(Replay);
        return false;
    }

    public void SendQueuedPackets()
    {
        ResetTimeoutTimer();
    }

    public static void ResetTimeoutTimer()
    {
        if (Netplay.Connection != null)
            Netplay.Connection.TimeOutTimer = 0;
    }

    public bool StartListening(SocketConnectionAccepted callback) =>
        throw new InvalidOperationException("The replaying socket cannot listen");

    public void StopListening() => throw new InvalidOperationException("The replaying socket cannot listen");

    public RemoteAddress GetRemoteAddress() => RemoteAddress;
}