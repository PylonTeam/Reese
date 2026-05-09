using System;
using System.IO;
using System.Linq;
using Reese.Common.TimeScaleTool;
using Reese.Core.Debug;
using Terraria;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
public class Replayer : ModSystem, ITicker
{
    public const string ReplayEndedStatusText = "Replay ended";

    public uint Ticks { get; private set; }
    public static uint ActiveDurationTicks { get; private set; }
    public static ReplayMetadata ActiveMetadata { get; private set; }
    public static bool IsPlaybackSocketActive => CurrentReplaySocket is { IsClosed: false };

    private static string PendingReplayPath;
    private static ReplayInspectionReport PendingReplayReport;
    private static ReplaySocket CurrentReplaySocket;

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
    }

    public override void Unload()
    {
        On_Netplay.ClientLoopSetup -= OnClientLoopSetup;
    }

    public static void BeginPlayback(string replayPath)
    {
        if (!File.Exists(replayPath))
            throw new FileNotFoundException("Replay file not found", replayPath);

        SetReplayLoadingStatus($"Inspecting replay: {Path.GetFileName(replayPath)}");
        var report = ReplayInspector.Inspect(replayPath);
        Log.Info(report.ToLogString());

        try
        {
            PendingReplayPath = replayPath;
            PendingReplayReport = report;
            ActiveDurationTicks = report.DurationTicks;
            ActiveMetadata = report.Metadata;
            ReplaySession.BeginPlayback(replayPath);

            SetReplayLoadingStatus("Starting replay client loop");
            Netplay.SetRemoteIP("127.0.0.1");
            Main.autoPass = true;
            Netplay.StartTcpClient();
            Main.menuMode = 10;
        }
        catch
        {
            PendingReplayPath = null;
            PendingReplayReport = null;
            ActiveDurationTicks = 0;
            ActiveMetadata = null;
            ReplaySession.End("playback launch failed");
            throw;
        }
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        if (!string.IsNullOrWhiteSpace(PendingReplayPath))
        {
            var stagePath = PendingReplayPath;
            PendingReplayPath = null;
            SetReplayLoadingStatus("Preparing Terraria client state for replay");
            orig(new ReplayRemoteAddress());
            StartReplayClientLoop(stagePath);
            return;
        }

        orig(address);
    }

    private void StartReplayClientLoop(string stagePath)
    {
        if (!File.Exists(stagePath))
        {
            Netplay.Disconnect = true;
            SetReplayLoadingStatus($"Replay file not found: {stagePath}", warn: true);
            ReplaySession.End("missing replay file");
            return;
        }

        Ticks = 0;
        SetReplayLoadingStatus("Opening replay packet stream");
        Netplay.Connection ??= new RemoteServer();
        Netplay.Connection.ReadBuffer ??= new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
        var replayFile = ReplayFile.Read(ReplayFile.OpenReadShared(stagePath));
        ActiveMetadata = replayFile.Metadata;
        ActiveDurationTicks = Math.Max(ActiveDurationTicks, PendingReplayReport?.DurationTicks ?? replayFile.Metadata.DurationTicks);
        CurrentReplaySocket = new ReplaySocket(this, replayFile);
        Netplay.Connection.Socket = CurrentReplaySocket;
        Netplay.Connection.Socket.Connect(new ReplayRemoteAddress());
        PendingReplayReport = null;
        SetReplayLoadingStatus("Replay stream ready; waiting for first packet");
    }

    public void AdvancePlaybackTick()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        if (ActiveDurationTicks > 0 && Ticks >= ActiveDurationTicks)
            return;

        Ticks++;
        if ((Ticks % 60) == 0)
            Log.Info("Client replay tick: " + Ticks);
    }

    public static uint CurrentTick => ModContent.GetInstance<Replayer>().Ticks;

    public static void SeekToTick(uint targetTick)
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        var replayer = ModContent.GetInstance<Replayer>();
        uint duration = ActiveDurationTicks;
        if (duration > 0)
            targetTick = Math.Min(targetTick, duration);

        if (targetTick < replayer.Ticks)
        {
            SetReplayLoadingStatus($"Backward seek requested ({FormatTick(targetTick)}), but this replay stream can only seek forward right now", warn: true);
            return;
        }

        if (targetTick == replayer.Ticks)
            return;

        replayer.Ticks = targetTick;
        CurrentReplaySocket?.ClearWaitLog();
        SetReplayLoadingStatus($"Seeking forward to {FormatTick(targetTick)}");
    }

    public static void StopPlayback(string reason = "manual replay stop")
    {
        if (CurrentReplaySocket is { IsClosed: false } socket)
        {
            socket.FinishPlayback(reason);
            return;
        }

        if (!ReplaySession.IsReplayPlayback)
            return;

        MarkReplayEnded();
        Netplay.Disconnect = true;
        ReplaySession.End(reason);
    }

    private static void SetReplayLoadingStatus(string message, bool warn = false)
    {
        string status = $"Replay loading: {message}";
        Main.statusText = status;

        if (warn)
            Log.Warn(status);
        else
            Log.Info(status);
    }

    private static void MarkReplayEnded()
    {
        ModContent.GetInstance<TimeScaleSystem>().SetTimeScale(1f);
        Main.statusText = ReplayEndedStatusText;

        if (Netplay.Connection != null)
        {
            Netplay.Connection.IsActive = false;
            Netplay.Connection.StatusText = string.Empty;
        }
    }

    private static string FormatTick(uint tick)
    {
        var span = TimeSpan.FromSeconds(tick / 60d);
        return span.TotalHours >= 1d
            ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes:00}:{span.Seconds:00}";
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
        private bool _closed;
        private bool _reportedWaitingForTick;
        private bool _reportedFirstPacket;

        public bool IsClosed => _closed;

        public void Close()
        {
            if (_closed)
                return;

            _closed = true;
            Log.Info("Closing replay socket");
            replayFile.Dispose();
            if (ReferenceEquals(CurrentReplaySocket, this))
                CurrentReplaySocket = null;

            ReplaySession.End("playback socket closed");
        }

        public bool IsConnected()
        {
            if (replayFile.EndOfFile)
                FinishPlayback();

            return !_closed;
        }

        public void Connect(RemoteAddress address)
        {
            Log.Info($"Replay connect to {address}");
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            var stats = ReplayPacketStats.FromPacketData(data.AsSpan(offset, size));
            if (stats.PacketCount > 0 || stats.MalformedPacketDataCount > 0)
            {
                string topMessages = string.Join(", ", stats.MessageCounts
                    .OrderByDescending(x => x.Value)
                    .Take(5)
                    .Select(x => $"{x.Key}:{x.Value}"));
                Log.Debug($"Ignored replay client packets: count={stats.PacketCount}, malformed={stats.MalformedPacketDataCount}, ids={topMessages}");
            }

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
            if (numberOfBytesRead == 0 && replayFile.EndOfFile)
                FinishPlayback();

            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            if (_closed)
                return false;

            if (replayFile.EndOfFile)
            {
                FinishPlayback();
                return false;
            }

            if (ticker.Ticks < replayFile.Tick)
            {
                if (!_reportedWaitingForTick)
                {
                    SetReplayLoadingStatus($"Waiting for tick {replayFile.Tick} (current {ticker.Ticks})");
                    _reportedWaitingForTick = true;
                }

                return false;
            }

            bool hasData = replayFile.NumberOfPacketDataBytesRemaining > 0;
            if (hasData && !_reportedFirstPacket)
            {
                SetReplayLoadingStatus($"Feeding replay packets at tick {ticker.Ticks}");
                _reportedFirstPacket = true;
            }

            return hasData;
        }

        public void SendQueuedPackets()
        {
        }

        public void ClearWaitLog()
        {
            _reportedWaitingForTick = false;
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The replaying socket cannot listen");

        public void StopListening() => throw new InvalidOperationException("The replaying socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;

        public void FinishPlayback(string reason = "playback reached EOF")
        {
            if (_closed)
                return;

            _closed = true;
            Log.Info("Replay finished");
            replayFile.Dispose();
            if (ReferenceEquals(CurrentReplaySocket, this))
                CurrentReplaySocket = null;

            MarkReplayEnded();
            Netplay.Disconnect = true;
            ReplaySession.End(reason);
        }
    }
}
