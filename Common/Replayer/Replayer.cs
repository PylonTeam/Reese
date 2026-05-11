using System;
using System.IO;
using System.Linq;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
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

        try
        {
            SetReplayLoadingStatus("Opening replay");
            PendingReplayPath = replayPath;

            using ReplayFile replayFile = ReplayFile.Read(ReplayFile.OpenReadShared(replayPath));
            ActiveMetadata = replayFile.Metadata;
            ActiveDurationTicks = replayFile.Metadata.DurationTicks;

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
        ActiveDurationTicks = Math.Max(ActiveDurationTicks, replayFile.Metadata.DurationTicks);
        CurrentReplaySocket = new ReplaySocket(this, replayFile);
        Netplay.Connection.Socket = CurrentReplaySocket;
        Netplay.Connection.Socket.Connect(new ReplayRemoteAddress());
        SetReplayLoadingStatus("Replay stream ready; waiting for first packet");
    }

    public void AdvancePlaybackTick()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        if (ActiveDurationTicks > 0 && Ticks >= ActiveDurationTicks)
            return;

        Ticks++;
        //if ((Ticks % 60) == 0)
        //Log.Info("Client replay tick: " + Ticks);
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

        bool resetStream = targetTick < replayer.Ticks;
        if (resetStream && CurrentReplaySocket?.ResetToStart() != true)
        {
            SetReplayStatus("Unable to seek replay", $"Unable to seek back to {FormatTick(targetTick)}", warn: true);
            return;
        }

        if (!resetStream && targetTick == replayer.Ticks)
            return;

        replayer.Ticks = targetTick;
        CurrentReplaySocket?.ClearWaitLog();

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        SetReplayStatus("Seeking replay...", $"Seeking to {FormatTick(targetTick)}");
    }

    public static void SeekToStart()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        var replayer = ModContent.GetInstance<Replayer>();
        if (CurrentReplaySocket?.ResetToStart() != true)
        {
            SetReplayStatus("Unable to seek replay", "Unable to return to the replay start", warn: true);
            return;
        }

        replayer.Ticks = 0;

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        SetReplayStatus("Seeking replay...", "Returned to replay start");
    }

    public static void SeekToEnd()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        uint duration = ActiveDurationTicks;
        if (duration == 0)
            return;

        SeekToTick(duration);
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(0f);
        SetReplayStatus("Seeking replay...", "Seeking to replay end");
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

    private static void SetReplayLoadingStatus(string developerMessage, bool warn = false)
    {
        Main.statusText = warn ? "Replay loading failed" : "Loading replay...";
        LogReplayFlow(developerMessage, warn);
    }

    private static void SetReplayStatus(string userMessage, string developerMessage, bool warn = false)
    {
        Main.statusText = userMessage;
        LogReplayFlow(developerMessage, warn);
    }

    private static void LogReplayFlow(string message, bool warn = false)
    {
        string text = $"Replay flow: {message}";

        if (warn)
            Log.Warn(text);
        else
            Log.Info(text);
    }

    private static void MarkReplayEnded()
    {
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);
        SetReplayStatus(ReplayEndedStatusText, "Replay ended");

        if (Netplay.Connection != null)
        {
            Netplay.Connection.IsActive = false;
            Netplay.Connection.StatusText = string.Empty;
        }
    }

    private static void PauseAtReplayEnd()
    {
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(0f);
        SetReplayStatus(ReplayEndedStatusText, "Replay reached EOF and paused at final frame");

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = ReplayEndedStatusText;
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
        private bool _finished;
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
                PauseAtEnd();

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
                //Log.Debug($"Ignored replay client packets: count={stats.PacketCount}, malformed={stats.MalformedPacketDataCount}, ids={topMessages}");
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
                PauseAtEnd();

            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            if (_closed)
                return false;

            if (_finished)
                return false;

            if (replayFile.EndOfFile)
            {
                PauseAtEnd();
                return false;
            }

            if (ticker.Ticks < replayFile.Tick)
            {
                if (!_reportedWaitingForTick)
                {
                    LogReplayFlow($"Waiting for replay tick {replayFile.Tick}; current tick is {ticker.Ticks}");
                    _reportedWaitingForTick = true;
                }

                return false;
            }

            bool hasData = replayFile.NumberOfPacketDataBytesRemaining > 0;
            if (hasData && !_reportedFirstPacket)
            {
                LogReplayFlow($"Feeding replay packets at tick {ticker.Ticks}");
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

        public bool ResetToStart()
        {
            if (_closed)
                return false;

            try
            {
                replayFile.ResetRead();
                _finished = false;
                _reportedWaitingForTick = false;
                _reportedFirstPacket = false;
                return true;
            }
            catch (Exception e)
            {
                Log.Warn("Failed to return replay stream to start: " + e);
                return false;
            }
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

        private void PauseAtEnd(string reason = "playback reached EOF")
        {
            if (_closed || _finished)
                return;

            _finished = true;
            var replayer = ModContent.GetInstance<Replayer>();
            replayer.Ticks = Math.Max(replayer.Ticks, ActiveDurationTicks);
            Log.Info($"Replay reached EOF; pausing at final frame ({reason})");
            PauseAtReplayEnd();
        }
    }
}
