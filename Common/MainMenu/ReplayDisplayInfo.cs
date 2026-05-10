using Reese.Common.Replayer;
using Reese.Core.Debug;
using System;
using System.Globalization;
using System.IO;

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
    public string MetadataTooltip { get; init; }

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
            ReplayMetadata metadata = replayFile.Metadata ?? ReplayMetadata.Legacy();

            if (DateTime.TryParse(metadata.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime createdUtc))
                date = createdUtc.ToLocalTime();

            string worldName = string.IsNullOrWhiteSpace(metadata.WorldName) ? InferWorldName(fileName) : metadata.WorldName;
            string playerName = string.IsNullOrWhiteSpace(metadata.PlayerName) ? "-" : metadata.PlayerName;
            int tickRate = metadata.TickRate > 0 ? metadata.TickRate : 60;

            return new ReplayDisplayInfo
            {
                FullPath = path,
                FileName = fileName,
                WorldName = Compact(worldName),
                PlayerName = Compact(playerName),
                Duration = BuildDuration(metadata.DurationTicks, tickRate),
                DurationTicks = metadata.DurationTicks,
                Date = date,
                FileSizeBytes = fileSizeBytes,
                MetadataTooltip = BuildMetadataTooltip(fileName, metadata, fileSizeBytes)
            };
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to read replay metadata for {fileName}: {e.Message}");

            string worldName = InferWorldName(fileName);
            return new ReplayDisplayInfo
            {
                FullPath = path,
                FileName = fileName,
                WorldName = Compact(worldName),
                PlayerName = "-",
                Duration = TimeSpan.Zero,
                DurationTicks = 0,
                Date = date,
                FileSizeBytes = fileSizeBytes,
                MetadataTooltip = $"File: {fileName}\nSize: {FormatFileSizeText(fileSizeBytes)}\nError: {e.Message}"
            };
        }
    }

    private static string BuildMetadataTooltip(string fileName, ReplayMetadata metadata, long fileSizeBytes)
    {
        string[] mods = metadata.ModNames ?? [];
        string modText = mods.Length == 0 ? "Mods: -" : $"Mods: {mods.Length:N0} loaded";

        return string.Join("\n",
        [
            $"File: {fileName}",
            $"Player: {EmptyToDash(metadata.PlayerName)}",
            $"World: {EmptyToDash(metadata.WorldName)}",
            $"Length: {FormatDurationText(BuildDuration(metadata.DurationTicks, metadata.TickRate > 0 ? metadata.TickRate : 60))}",
            $"Size: {FormatFileSizeText(fileSizeBytes)}",
            modText
        ]);
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

    private static string InferWorldName(string fileName)
    {
        string name = Path.GetFileNameWithoutExtension(fileName);

        if (name.StartsWith("SP_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("MP_", StringComparison.OrdinalIgnoreCase))
            name = name[3..];

        const int timestampLength = 19;
        if (name.Length > timestampLength + 1)
        {
            int timestampStart = name.Length - timestampLength;
            if (timestampStart > 0 && name[timestampStart - 1] == '_' && LooksLikeTimestamp(name[timestampStart..]))
                name = name[..(timestampStart - 1)];
        }

        return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
    }

    private static bool LooksLikeTimestamp(string value)
    {
        return value.Length == 19 &&
            char.IsDigit(value[0]) &&
            char.IsDigit(value[1]) &&
            char.IsDigit(value[2]) &&
            char.IsDigit(value[3]) &&
            value[4] == '-' &&
            value[7] == '-' &&
            value[10] == '_' &&
            value[13] == '-' &&
            value[16] == '-';
    }

    private static string Compact(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        return value.Length <= 13 ? value : value[..12] + ".";
    }

    private static string EmptyToDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}