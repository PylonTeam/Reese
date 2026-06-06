using log4net;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Reese.Common.MainMenu;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Log.Info("OnClientLoopSetup");

        bool isReplayAddress = address.GetIdentifier() == "10.2.3.4";
        if (isReplayAddress && IsReplayLaunchCancelled())
        {
            AbortCancelledReplayLaunch("replay launch cancelled before client setup");
            return;
        }

        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (!isReplayAddress)
            return;

        if (IsReplayLaunchCancelled())
        {
            AbortCancelledReplayLaunch("replay launch cancelled before socket setup");
            return;
        }

        Ticks = 0;
        Log.Info("Connecting to magic replay IP thingy!");
        Netplay.Connection = new RemoteServer();
        Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
        //Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(File.OpenRead("record.bin")));

        string replayPath = ReplayPlayback.CurrentPath;
        string fileName = Path.GetFileNameWithoutExtension(replayPath);

        if (!File.Exists(replayPath))
        {
            string fileNotExistMessage = Loc.Get("MainMenu.ReplayStartup.NotFound");
            Log.Error(fileNotExistMessage);
            ReplayPlayback.End(fileNotExistMessage);
            Main.statusText = fileNotExistMessage;
            Main.menuMode = 0; // back to main menu
            return;
        }

        if (string.IsNullOrWhiteSpace(replayPath))
        {
            string replayNull = Loc.Get("MainMenu.ReplayStartup.NotFoundOrNull", fileName);
            Log.Error(replayNull);
            ReplayPlayback.End(replayNull);
            Main.statusText = replayNull;
            Main.menuMode = 0; // back to main menu
            return;
        }

        Log.Info($"Opening replay file: {fileName}");

        FileStream stream = File.Open(replayPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ReplayFile replayFile = ReplayFile.Read(stream);

        if (IsReplayLaunchCancelled())
        {
            replayFile.Dispose();
            AbortCancelledReplayLaunch("replay launch cancelled after replay file read");
            return;
        }

        Netplay.Connection.Socket = new ReplaySocket(this, replayFile);
    }

    private static bool IsReplayLaunchCancelled()
    {
        return !ReplayPlayback.IsReplayLaunchActive ||
               ReplayPlayback.LaunchCancelled() ||
               !ModContent.GetInstance<MainMenuSystem>().IsLaunchingReplay;
    }

    private static void AbortCancelledReplayLaunch(string reason)
    {
        Log.Info($"{reason}, aborting.");
        ReplayPlayback.End(reason);
        Netplay.Disconnect = true;
        Main.QueueMainThreadAction(() =>
        {
            ModContent.GetInstance<MainMenuSystem>().CancelReplayLaunch();
            Main.menuMode = 0;
        });
    }

    public void AdvancePlaybackTick()
    {
        if (!ReplayPlayback.IsReplayPlayback)
            return;

        if (ReplayPlayback.IsSeeking && Ticks >= ReplayPlayback.SeekTargetTick)
        {
            ReplayPlayback.TryCompleteSeek(Ticks);
            return;
        }

        if (ReplayPlayback.DurationTicks > 0 && Ticks >= ReplayPlayback.DurationTicks)
            return;

        // Advance tick!
        Ticks++;
        ReplayPlayback.NotifyPlaybackTickAdvanced(Ticks);

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
        private readonly ReplayRemoteAddress _remoteAddress = new();
        private readonly object replayFileLock = new();
        public IReadOnlyList<ReplayBaselineEntry> Baselines
        {
            get
            {
                lock (replayFileLock)
                    return replayFile.Baselines.ToArray();
            }
        }

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

        public ReplayBaselineEntry? GetNearestBaselineBefore(uint targetTick)
        {
            lock (replayFileLock)
                return replayFile.GetNearestBaselineBefore(targetTick);
        }

        public bool SeekToBaseline(ReplayBaselineEntry baseline)
        {
            try
            {
                lock (replayFileLock)
                    return replayFile.SeekToBaseline(baseline);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to seek replay stream to baseline at tick {baseline.Tick}: {e.Message}");
                return false;
            }
        }

        public bool HasPendingDataAtOrBefore(uint targetTick)
        {
            lock (replayFileLock)
                return replayFile.HasPendingDataAtOrBefore(targetTick);
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
