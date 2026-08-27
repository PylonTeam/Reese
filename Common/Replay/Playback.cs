using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    public static PlaybackSocket Socket => Netplay.Connection.Socket as PlaybackSocket;
    public static bool IsPlaying => Socket is { Closed: false };

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
        var sock = new PlaybackSocket(ModContent.GetInstance<PlaybackTimeScale>(), replay);
        new Thread(PlaybackClientLoop)
        {
            Name = "Playback Client Thread",
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
        if (IsPlaying)
        {
            replay = Socket.Replay;
            return true;
        }

        replay = null;
        return false;
    }

    private static void PlaybackClientLoop(object context)
    {
        var socket = (PlaybackSocket)context;
        Netplay.ClientLoopSetup(socket.GetRemoteAddress());
        Netplay.Connection.Socket = socket;
        Main.statusText = "Replay starting";
        Main.menuMode = MenuID.MultiplayerJoining;

        try
        {
            Netplay.InnerClientLoop();
        }
        finally
        {
            // should have already been Disposed because the socket was Closed, but just in case.
            Socket?.Replay.Dispose();
        }
    }

    public static IList<ReplayFile> EnumerateReplays(string dir, bool footerNow)
    {
        var replays = new List<ReplayFile>();

        foreach (var path in Directory.EnumerateFiles(dir, "*.reese", new EnumerationOptions() { IgnoreInaccessible = true }))
        {
            FileStream fs;
            ReplayFile replay;

            try
            {
                fs = File.OpenRead(path);
            }
            catch (Exception e)
            {
                Log.Warn($"unable to open enumerated replay {path}: {e}");
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
        if (Socket == null)
            return false;

        if (messageType == MessageID.Kick)
        {
            var reason = NetworkText.Deserialize(reader);
            Main.NewText($"[i:{ModContent.ItemType<CameraItem>()}] Replay disconnected: {reason}", Color.Red);

            return true;
        }

        return false;
    }

    private class PlaybackRemoteAddress : RemoteAddress
    {
        public override string GetIdentifier() => "ReesePlayback";
        public override string GetFriendlyName() => "Reese Playback";
        public override bool IsLocalHost() => true;

        public override string ToString() => GetFriendlyName();
    }

    public class PlaybackSocket(ITicker ticker, ReplayFile replay) : ISocket
    {
        private static readonly PlaybackRemoteAddress RemoteAddress = new();

        public bool Closed { get; private set; }

        public ITicker Ticker => ticker;
        public ReplayFile Replay => replay;

        public void Close()
        {
            if (Closed)
                return;

            Log.Info("Closing replay socket");
            Replay.Dispose();
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
            ResetTimeoutTimer();
            var numberOfBytesRead = Replay.ReadData(data.AsSpan()[offset..(offset + size)]);
            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            ResetTimeoutTimer();

            // FIXME: no! check connection state?
            if (Replay.IsBaselining)
                return true;

            if (Ticker.Tick >= Replay.Tick)
            {
                if (Replay.IsDataBuffered)
                    return true;

                if (Replay.Terminated)
                    return false;
            }

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
}
