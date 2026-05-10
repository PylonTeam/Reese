using Reese.Common.Replayer;
using Reese.Core.Debug;
using System;
using System.Globalization;
using System.IO;

namespace Reese.Common.MainMenu;

internal sealed class ReplayDisplayInfo
{
    public string FullPath { get; init; }
    public string FileName { get; init; }
    public string WorldName { get; init; }
    public string PlayerName { get; init; }
    public TimeSpan Duration { get; init; }
    public uint DurationTicks { get; init; }
    public DateTime Date { get; init; }
    public long FileSizeBytes { get; init; }
    public string PreviewImagePath { get; init; }

    public string DurationText => FormatDurationText(Duration);
    public string FileSizeText => FormatFileSizeText(FileSizeBytes);

    public static ReplayDisplayInfo FromFile(string path)
    {
        string fileName = Path.GetFileName(path);
        long fileSizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0;
        DateTime date = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;

        try
        {
            using ReplayFile replayFile = ReplayFile.Read(ReplayFile.OpenReadShared(path));
            ReplayMetadata metadata = replayFile.Metadata ?? new ReplayMetadata();

            if (DateTime.TryParse(metadata.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime createdUtc))
                date = createdUtc.ToLocalTime();

            int tickRate = metadata.TickRate > 0 ? metadata.TickRate : 60;

            return new ReplayDisplayInfo
            {
                FullPath = path,
                FileName = fileName,
                WorldName = EmptyToDash(metadata.WorldName),
                PlayerName = EmptyToDash(metadata.PlayerName),
                Duration = BuildDuration(metadata.DurationTicks, tickRate),
                DurationTicks = metadata.DurationTicks,
                Date = date,
                FileSizeBytes = fileSizeBytes,
                PreviewImagePath = ReplayPreviewImages.GetPreviewPath(path)
            };
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to read replay metadata for {fileName}: {e.Message}");

            return new ReplayDisplayInfo
            {
                FullPath = path,
                FileName = fileName,
                WorldName = "-",
                PlayerName = "-",
                Duration = TimeSpan.Zero,
                DurationTicks = 0,
                Date = date,
                FileSizeBytes = fileSizeBytes,
                PreviewImagePath = ReplayPreviewImages.GetPreviewPath(path)
            };
        }
    }

    private static TimeSpan BuildDuration(uint ticks, int tickRate)
    {
        return ticks == 0 || tickRate <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(ticks / (double)tickRate);
    }

    private static string FormatDurationText(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private static string FormatFileSizeText(long bytes)
    {
        long kilobytes = Math.Max(1, (long)Math.Ceiling(bytes / 1024d));
        return kilobytes.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ") + " KB";
    }

    private static string EmptyToDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}