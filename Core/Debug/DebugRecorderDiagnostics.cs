using Reese.Common.Replayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reese.Core.Debug;

#if DEBUG
internal static class DebugRecorderDiagnostics
{
    private static readonly Dictionary<int, int> messageCounts = [];

    private static DateTime sessionStartedUtc;
    private static long lastFileBytes;
    private static long lastSampleFileBytes;
    private static DateTime lastSampleUtc;
    private static double bytesPerSecond;

    internal static bool IsActive { get; private set; }
    internal static uint CurrentTick { get; private set; }
    internal static uint LastWriteTick { get; private set; }
    internal static uint LastTickDelta { get; private set; }

    internal static int BlocksWritten { get; private set; }
    internal static int PacketsWritten { get; private set; }
    internal static long PacketBytesWritten { get; private set; }
    internal static long FileBytesWritten
    {
        get
        {
            UpdateFileBytes();
            return lastFileBytes;
        }
    }

    internal static int BaselineBlockBytes { get; private set; }
    internal static int LastBlockBytes { get; private set; }
    internal static int MaxBlockBytes { get; private set; }
    internal static int ZeroDeltaBlocks { get; private set; }

    internal static int MalformedPacketData { get; private set; }
    internal static int TrailingPacketBytes { get; private set; }

    internal static double BytesPerSecond
    {
        get
        {
            UpdateFileBytes();
            return bytesPerSecond;
        }
    }

    internal static double PacketsPerSecond => PacketsWritten / Math.Max(SessionAge.TotalSeconds, 0.001d);
    internal static double BlocksPerSecond => BlocksWritten / Math.Max(SessionAge.TotalSeconds, 0.001d);

    internal static string LastEvent { get; private set; } = "<none>";
    internal static string LastWarning { get; private set; } = "<none>";
    internal static string LastError { get; private set; } = "<none>";
    internal static TimeSpan SessionAge => sessionStartedUtc == default ? TimeSpan.Zero : DateTime.UtcNow - sessionStartedUtc;

    internal static void Start()
    {
        IsActive = true;
        sessionStartedUtc = DateTime.UtcNow;
        lastSampleUtc = sessionStartedUtc;
        lastFileBytes = 0;
        lastSampleFileBytes = 0;
        bytesPerSecond = 0d;

        CurrentTick = 0;
        LastWriteTick = 0;
        LastTickDelta = 0;

        BlocksWritten = 0;
        PacketsWritten = 0;
        PacketBytesWritten = 0;
        BaselineBlockBytes = 0;
        LastBlockBytes = 0;
        MaxBlockBytes = 0;
        ZeroDeltaBlocks = 0;

        MalformedPacketData = 0;
        TrailingPacketBytes = 0;

        LastEvent = "Recording started";
        LastWarning = "<none>";
        LastError = "<none>";
        messageCounts.Clear();
    }

    internal static void Stop(string reason)
    {
        IsActive = false;
        LastEvent = string.IsNullOrWhiteSpace(reason) ? "Recording stopped" : "Recording stopped: " + reason;
        UpdateFileBytes();
    }

    internal static void Tick(uint tick)
    {
        CurrentTick = tick;
        UpdateFileBytes();
    }

    internal static void RecordBlock(uint tickDelta, int byteCount, uint writeTick)
    {
        BlocksWritten++;
        LastTickDelta = tickDelta;
        LastBlockBytes = byteCount;
        LastWriteTick = writeTick;
        PacketBytesWritten += byteCount;
        MaxBlockBytes = Math.Max(MaxBlockBytes, byteCount);

        if (BlocksWritten == 1)
            BaselineBlockBytes = byteCount;

        if (tickDelta == 0)
            ZeroDeltaBlocks++;

        LastEvent = $"Block written: tick={writeTick}, delta={tickDelta}, bytes={byteCount}";
        UpdateFileBytes();
    }

    internal static void RecordPacketStats(ReplayPacketStats stats)
    {
        PacketsWritten += stats.PacketCount;
        MalformedPacketData += stats.MalformedPacketDataCount;

        foreach ((int messageId, int count) in stats.MessageCounts)
        {
            messageCounts.TryGetValue(messageId, out int existing);
            messageCounts[messageId] = existing + count;
        }

        if (stats.MalformedPacketDataCount > 0)
            Warn("Malformed packet data: " + stats.MalformedPacketDataCount);
    }

    internal static void SetTrailingPacketBytes(int byteCount)
    {
        TrailingPacketBytes = byteCount;

        if (byteCount > 0)
            Warn("Trailing packet bytes: " + byteCount);
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

    internal static string GetTopMessages(int take = 6)
    {
        return messageCounts.Count == 0
            ? "none"
            : string.Join(", ", messageCounts.OrderByDescending(x => x.Value).Take(take).Select(x => $"{x.Key}:{x.Value}"));
    }

    private static void UpdateFileBytes()
    {
        string path = ReplaySession.CurrentPath;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            lastFileBytes = 0;
            return;
        }

        lastFileBytes = new FileInfo(path).Length;

        DateTime now = DateTime.UtcNow;
        double seconds = (now - lastSampleUtc).TotalSeconds;

        if (seconds < 0.5d)
            return;

        bytesPerSecond = (lastFileBytes - lastSampleFileBytes) / Math.Max(seconds, 0.001d);
        lastSampleFileBytes = lastFileBytes;
        lastSampleUtc = now;
    }
}
#endif