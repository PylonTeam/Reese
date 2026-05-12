using log4net;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese;

[Autoload(Side = ModSide.Client)]
public class Replayer : ModSystem, ITicker
{
    private delegate void HighFpsSupportConfigEnsureValidateStateDelegate(object self);

    public uint Ticks { get; private set; }

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;

        //IL_Main.DoUpdate += il =>
        //{
        //    var cursor = new ILCursor(il);
        //    cursor.GotoNext(i => i.MatchStsfld<Main>("drawSkip"));
        //    // cursor.Index += 1;
        //    cursor.EmitDelegate(() =>
        //    {
        //        if (!ReplayPlayback.IsReplayPlayback || !Main.gameMenu)
        //            return;

        //        Ticks++;
        //        if ((Ticks % 60) == 0)
        //            Mod.Logger.Info("Tick: " + Ticks);
        //    });
        //};
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (address.GetIdentifier() == "10.2.3.4")
        {
            Ticks = 0;
            Mod.Logger.Info("Connecting to magic replay IP thingy!");
            Netplay.Connection = new RemoteServer();
            Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
            //Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(File.OpenRead("record.bin")));

            string replayPath = ReplayPlayback.CurrentPath;

            if (string.IsNullOrWhiteSpace(replayPath) || !File.Exists(replayPath))
            {
                Mod.Logger.Error($"Replay path missing or invalid: {replayPath ?? "<null>"}");
                ReplayPlayback.End("missing replay path");
                Main.menuMode = 0;
                return;
            }

            Mod.Logger.Info($"Opening replay file: {replayPath}");

            FileStream stream = File.Open(replayPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(stream));
        }
    }

    public void AdvancePlaybackTick()
    {
        if (!ReplayPlayback.IsReplayPlayback)
            return;

        Ticks++;
        if ((Ticks % (60*5)) == 0)
        {
            Log.Info("Client replay tick: " + Ticks);
        }
    }

    private class ReplayRemoteAddress : RemoteAddress
    {
        public override string GetIdentifier() => "Replaying";
        public override string GetFriendlyName() => "Replaying";
        public override bool IsLocalHost() => true;

        public override string ToString() => GetFriendlyName();
    }

    public class ReplaySocket(ITicker ticker, ReplayFile replayFile) : ISocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReplaySocket));
        private readonly ReplayRemoteAddress _remoteAddress = new();

        public void Close()
        {
            Log.Info("Closing replay socket");
            replayFile.Dispose();
            ReplayPlayback.End("replay socket closed");
        }

        public bool ResetToStart()
        {
            try
            {
                // Tell your ReplayFile to seek its internal stream back to the start
                // This usually involves: stream.Position = 0 (or after the header)
                replayFile.Reset();
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to reset replay stream: {e.Message}");
                return false;
            }
        }

        public bool IsConnected() => true;

        public void Connect(RemoteAddress address)
        {
            Log.Info($"Replay connect to {address}");
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            // Outgoing client packets (Hello, etc.) are discarded — we're in replay mode.
            // But we must invoke the callback or the client loop hangs waiting for confirmation.
            callback?.Invoke(state);
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
        {
            if (!IsDataAvailable())
            {
                callback(state, 0);
                return;
            }

            var numberOfBytesRead = replayFile.ReadPacketData(data.AsSpan()[offset..(offset + size)]);
            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            return ticker.Ticks >= replayFile.Tick && replayFile.NumberOfPacketDataBytesRemaining > 0;
        }

        public void SendQueuedPackets()
        {
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The replaying socket cannot listen");

        public void StopListening() => throw new InvalidOperationException("The replaying socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;
    }

    #region Seeking
    private static ReplaySocket CurrentReplaySocket => Netplay.Connection?.Socket as ReplaySocket;
    public static void SeekToTick(uint targetTick)
    {
        if (!ReplayPlayback.IsReplayPlayback)
            return;

        var replayer = ModContent.GetInstance<Replayer>();

        uint duration = ReplayPlayback.DurationTicks;
        if (duration > 0)
            targetTick = Math.Min(targetTick, duration);

        // If seeking backward, we must reset the stream
        bool resetStream = targetTick < replayer.Ticks;
        if (resetStream)
        {
            if (CurrentReplaySocket?.ResetToStart() != true)
            {
                Log.Chat("Unable to seek backward: Stream reset failed.");
                return;
            }
        }

        if (!resetStream && targetTick == replayer.Ticks)
            return;

        replayer.Ticks = targetTick;

        Netplay.Connection?.StatusText = string.Empty;

        Log.Chat($"Seeking to tick {targetTick}...");
    }

    public static void SeekToStart()
    {
        SeekToTick(0);
    }

    public static void SeekToEnd()
    {
        SeekToTick(ReplayPlayback.DurationTicks);
    }
    #endregion
}