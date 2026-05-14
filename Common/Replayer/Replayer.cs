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

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
public class Replayer : ModSystem, ITicker
{
    private delegate void HighFpsSupportConfigEnsureValidateStateDelegate(object self);

    public uint Ticks { get; private set; }

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
    }

    public override void Unload()
    {
        On_Netplay.ClientLoopSetup -= OnClientLoopSetup;
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (address.GetIdentifier() == "10.2.3.4")
        {
            Ticks = 0;
            Log.Info("Connecting to magic replay IP thingy!");
            Netplay.Connection = new RemoteServer();
            Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
            //Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(File.OpenRead("record.bin")));

            string replayPath = ReplayPlayback.CurrentPath;

            if (string.IsNullOrWhiteSpace(replayPath) || !File.Exists(replayPath))
            {
                Log.Error($"Replay path missing or invalid: {replayPath ?? "<null>"}");
                ReplayPlayback.End("missing replay path");
                Main.menuMode = 0;
                return;
            }

            Log.Info($"Opening replay file: {replayPath}");

            FileStream stream = File.Open(replayPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(stream));
        }
    }

    public void AdvancePlaybackTick()
    {
        if (!ReplayPlayback.IsReplayPlayback)
            return;

        if (ReplayPlayback.DurationTicks > 0 && Ticks >= ReplayPlayback.DurationTicks)
            return;

        // Advance tick!
        Ticks++;

        // Logging at 1 tick, 5 seconds, 10 seconds, and every 30 minutes thereafter
        if (Ticks == 1 || Ticks == 300 || Ticks == 600 || Ticks % (30 * 60 * 60) == 0)
        {
            Log.Chat("Client replay tick: " + Ticks);
        }
    }

    internal void SetTicks(uint ticks)
    {
        Ticks = ticks;
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
        private readonly object replayFileLock = new();

        public void Close()
        {
            Log.Info("Closing replay socket");
            lock (replayFileLock)
                replayFile.Dispose();

            ReplayPlayback.End("replay socket closed");
        }

        public bool ResetToStart()
        {
            try
            {
                // Tell your ReplayFile to seek its internal stream back to the start
                // This usually involves: stream.Position = 0 (or after the header)
                lock (replayFileLock)
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
            int numberOfBytesRead = 0;

            lock (replayFileLock)
            {
                ResetTimeoutTimer();

                if (!replayFile.ReachedTerminator && ticker.Ticks >= replayFile.Tick && replayFile.NumberOfPacketDataBytesRemaining > 0)
                    numberOfBytesRead = replayFile.ReadPacketData(data.AsSpan()[offset..(offset + size)]);
            }

            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            lock (replayFileLock)
            {
                ResetTimeoutTimer();
                return !replayFile.ReachedTerminator &&
                       ticker.Ticks >= replayFile.Tick &&
                       replayFile.NumberOfPacketDataBytesRemaining > 0;
            }
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

        public RemoteAddress GetRemoteAddress() => _remoteAddress;
    }
}
