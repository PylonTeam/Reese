using Reese.Common.Replayer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reese.Core.Debug;

#if DEBUG
internal static class DebugReplayerDiagnostics
{
    private static readonly Dictionary<int, int> incomingMessageCounts = [];
    private static readonly Dictionary<int, int> ignoredOutgoingMessageCounts = [];

    private static DateTime sessionStartedUtc;
    private static uint lastTick;
    private static DateTime lastTickSampleUtc;
    private static double ticksPerSecond;

    internal static bool IsActive { get; private set; }
    internal static bool SocketActive { get; private set; }
    internal static bool WaitingForTick { get; private set; }
    internal static bool ReachedEof { get; private set; }

    internal static uint CurrentTick { get; private set; }
    internal static uint DurationTicks { get; private set; }
    internal static uint WaitingForReplayTick { get; private set; }

    internal static int ReceiveCalls { get; private set; }
    internal static int ZeroByteReceives { get; private set; }
    internal static int IncomingPackets { get; private set; }
    internal static long IncomingPacketBytes { get; private set; }
    internal static int IncomingMalformedPacketData { get; private set; }

    internal static int IgnoredOutgoingPackets { get; private set; }
    internal static int IgnoredOutgoingMalformedPacketData { get; private set; }
    internal static string LastIgnoredOutgoingSummary { get; private set; } = "none";

    internal static int SeekCount { get; private set; }
    internal static uint LastSeekTargetTick { get; private set; }
    internal static int ResetToStartCount { get; private set; }

    internal static string LastEvent { get; private set; } = "<none>";
    internal static string LastWarning { get; private set; } = "<none>";
    internal static string LastError { get; private set; } = "<none>";

    internal static TimeSpan SessionAge => sessionStartedUtc == default ? TimeSpan.Zero : DateTime.UtcNow - sessionStartedUtc;
    internal static double TicksPerSecond => ticksPerSecond;
    internal static float Progress => DurationTicks == 0 ? 0f : CurrentTick / (float)DurationTicks;

    internal static void Start(uint durationTicks)
    {
        IsActive = true;
        SocketActive = false;
        WaitingForTick = false;
        ReachedEof = false;

        sessionStartedUtc = DateTime.UtcNow;
        lastTickSampleUtc = sessionStartedUtc;
        lastTick = 0;
        ticksPerSecond = 0d;

        CurrentTick = 0;
        DurationTicks = durationTicks;
        WaitingForReplayTick = 0;

        ReceiveCalls = 0;
        ZeroByteReceives = 0;
        IncomingPackets = 0;
        IncomingPacketBytes = 0;
        IncomingMalformedPacketData = 0;

        IgnoredOutgoingPackets = 0;
        IgnoredOutgoingMalformedPacketData = 0;
        LastIgnoredOutgoingSummary = "none";

        SeekCount = 0;
        LastSeekTargetTick = 0;
        ResetToStartCount = 0;

        LastEvent = "Playback started";
        LastWarning = "<none>";
        LastError = "<none>";

        incomingMessageCounts.Clear();
        ignoredOutgoingMessageCounts.Clear();
    }

    internal static void Stop(string reason)
    {
        IsActive = false;
        SocketActive = false;
        LastEvent = string.IsNullOrWhiteSpace(reason) ? "Playback stopped" : "Playback stopped: " + reason;
    }

    internal static void Tick(uint tick, uint durationTicks)
    {
        CurrentTick = tick;
        DurationTicks = durationTicks;

        DateTime now = DateTime.UtcNow;
        double seconds = (now - lastTickSampleUtc).TotalSeconds;

        if (seconds < 0.5d)
            return;

        ticksPerSecond = (tick - lastTick) / Math.Max(seconds, 0.001d);
        lastTick = tick;
        lastTickSampleUtc = now;
    }

    internal static void SocketOpened()
    {
        SocketActive = true;
        LastEvent = "Replay socket opened";
    }

    internal static void SocketClosed(string reason)
    {
        SocketActive = false;
        LastEvent = string.IsNullOrWhiteSpace(reason) ? "Replay socket closed" : "Replay socket closed: " + reason;
    }

    internal static void Waiting(uint replayTick, uint currentTick)
    {
        WaitingForTick = true;
        WaitingForReplayTick = replayTick;
        LastEvent = $"Waiting for replay tick {replayTick}; current tick is {currentTick}";
    }

    internal static void Feeding(uint tick)
    {
        WaitingForTick = false;
        LastEvent = "Feeding packets at tick " + tick;
    }

    internal static void Receive(int byteCount)
    {
        ReceiveCalls++;

        if (byteCount <= 0)
        {
            ZeroByteReceives++;
            return;
        }

        IncomingPacketBytes += byteCount;
    }

    internal static void RecordIncomingPacketStats(ReplayPacketStats stats)
    {
        IncomingPackets += stats.PacketCount;
        IncomingMalformedPacketData += stats.MalformedPacketDataCount;

        foreach ((int messageId, int count) in stats.MessageCounts)
        {
            incomingMessageCounts.TryGetValue(messageId, out int existing);
            incomingMessageCounts[messageId] = existing + count;
        }

        if (stats.MalformedPacketDataCount > 0)
            Warn("Malformed incoming packet data: " + stats.MalformedPacketDataCount);
    }

    internal static void RecordIgnoredOutgoingPacketStats(ReplayPacketStats stats)
    {
        IgnoredOutgoingPackets += stats.PacketCount;
        IgnoredOutgoingMalformedPacketData += stats.MalformedPacketDataCount;
        LastIgnoredOutgoingSummary = FormatTopMessages(stats.MessageCounts);

        foreach ((int messageId, int count) in stats.MessageCounts)
        {
            ignoredOutgoingMessageCounts.TryGetValue(messageId, out int existing);
            ignoredOutgoingMessageCounts[messageId] = existing + count;
        }

        if (stats.MalformedPacketDataCount > 0)
            LastIgnoredOutgoingSummary += $" malformed={stats.MalformedPacketDataCount}";
    }

    internal static void Seek(uint targetTick)
    {
        SeekCount++;
        LastSeekTargetTick = targetTick;
        LastEvent = "Seek to tick " + targetTick;
    }

    internal static void ResetToStart()
    {
        ResetToStartCount++;
        LastEvent = "Replay stream reset to start";
    }

    internal static void Eof(string reason)
    {
        ReachedEof = true;
        WaitingForTick = false;
        LastEvent = string.IsNullOrWhiteSpace(reason) ? "Playback reached EOF" : "Playback reached EOF: " + reason;
    }

    internal static void Warn(string message)
    {
        LastWarning = string.IsNullOrWhiteSpace(message) ? "<none>" : message;
        LastEvent = "Warning: " + LastWarning;
    }

    internal static void Error(string message)
    {
        LastError = string.IsNullOrWhiteSpace(message) ? "<none>" : message;
        LastEvent = "Error: " + LastError;
    }

    internal static string GetTopIncomingMessages(int take = 6)
    {
        return FormatTopMessages(incomingMessageCounts, take);
    }

    internal static string GetTopIgnoredOutgoingMessages(int take = 6)
    {
        return FormatTopMessages(ignoredOutgoingMessageCounts, take);
    }

    private static string FormatTopMessages(IReadOnlyDictionary<int, int> counts, int take = 6)
    {
        return counts.Count == 0
            ? "none"
            : string.Join(", ", counts.OrderByDescending(x => x.Value).Take(take).Select(x => $"{x.Key}:{x.Value}"));
    }
}
#endif