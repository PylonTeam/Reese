using System;
using System.Collections.Generic;
using Reese.Common.Replay.ReplayEvents;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Common.Replayer;

public sealed class ReplayMetadata
{
    public string FullPath { get; init; }
    public string FileName { get; init; }
    public string ReplayName { get; init; }
    public string WorldName { get; init; }
    public uint DurationTicks { get; init; }
    public DateTime DateCreated { get; init; }
    public DateTime LastWriteTimeUtc { get; init; }
    public long SizeBytes { get; init; }
    public string[] ModNames { get; init; }
    public ReplayFileFlags Flags { get; init; }
    public ReplayTimelineEvent[] Events { get; init; } = [];

    public bool IsNew => Flags.HasFlag(ReplayFileFlags.New);
    public bool HasWatched => Flags.HasFlag(ReplayFileFlags.Watched);
    public bool IsFavorite => Flags.HasFlag(ReplayFileFlags.Favorite);

    /// <summary>
    /// Reads file info
    /// Read replay header if possible
    /// Scan duration if possible
    /// Returns: metadata to display.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static ReplayMetadata FromFile(string path)
    {
        bool fileExists = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        string fileName = fileExists ? Path.GetFileName(path) : "Error";

        if (!fileExists)
            Log.Warn($"Replay file missing: {path}");

        uint durationTicks = 0;
        string worldName = null;
        string[] modNames = null;
        ReplayFileFlags flags = ReplayFileFlags.None;
        ReplayTimelineEvent[] events = [];

        bool hasSummary = fileExists && ReplayFile.TryReadCatalogInfo(path, out durationTicks, out worldName, out modNames, out flags);
        if (fileExists)
            ReplayFile.TryReadEvents(path, out events);

        FileInfo fileInfo = fileExists ? new FileInfo(path) : null;

        return new ReplayMetadata
        {
            FullPath = path ?? string.Empty,
            FileName = fileName,
            ReplayName = EmptyToError(Path.GetFileNameWithoutExtension(fileName)),
            WorldName = EmptyToUnknown(worldName),
            DurationTicks = hasSummary ? durationTicks : 0,
            DateCreated = fileInfo?.LastWriteTime ?? DateTime.MinValue,
            LastWriteTimeUtc = fileInfo?.LastWriteTimeUtc ?? DateTime.MinValue,
            SizeBytes = fileInfo?.Length ?? 0,
            ModNames = modNames,
            Flags = flags,
            Events = events ?? []
        };
    }

    public ReplayMetadata WithFlags(ReplayFileFlags flags)
    {
        return new ReplayMetadata
        {
            FullPath = FullPath,
            FileName = FileName,
            ReplayName = ReplayName,
            WorldName = WorldName,
            DurationTicks = DurationTicks,
            DateCreated = DateCreated,
            LastWriteTimeUtc = LastWriteTimeUtc,
            SizeBytes = SizeBytes,
            ModNames = ModNames,
            Flags = flags,
            Events = Events
        };
    }

    private static string EmptyToUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
    }


    private static string EmptyToError(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Error" : value.Trim();
    }
}
