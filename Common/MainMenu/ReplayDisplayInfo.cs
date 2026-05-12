using Reese.Core.Debug;
using System;
using System.Globalization;
using System.IO;

namespace Reese.Common.MainMenu;

public sealed class ReplayDisplayInfo
{
    public string FullPath { get; init; }
    public string FileName { get; init; }
    public string WorldName { get; init; }
    public TimeSpan Duration { get; init; }
    public uint DurationTicks { get; init; }
    public DateTime Date { get; init; }
    public long FileSizeBytes { get; init; }
    public bool FileExists { get; init; }
    public bool HasMetadata { get; init; }

    public string DurationText => HasMetadata ? FormatDurationText(Duration) : "Error";
    public string FileSizeText => FileExists ? FormatFileSizeText(FileSizeBytes) : "Error";
    public string DateText => FileExists && Date != DateTime.MinValue ? Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "Error";

    public static ReplayDisplayInfo FromFile(string path)
    {
        bool fileExists = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        string fileName = string.IsNullOrWhiteSpace(path) ? "Error" : Path.GetFileName(path);
        long fileSizeBytes = fileExists ? new FileInfo(path).Length : 0;
        DateTime date = fileExists ? File.GetLastWriteTime(path) : DateTime.MinValue;

        if (!fileExists)
            Log.Warn($"Replay file missing: {path}");

        return new ReplayDisplayInfo
        {
            FullPath = path ?? string.Empty,
            FileName = EmptyToError(fileName),
            WorldName = "Error",
            Duration = TimeSpan.Zero,
            DurationTicks = 0,
            Date = date,
            FileSizeBytes = fileSizeBytes,
            FileExists = fileExists,
            HasMetadata = false
        };
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

    private static string EmptyToError(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Error" : value.Trim();
    }
}