using Reese.Common.Replayer;
using Reese.Core.Debug;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Reese.Common.MainMenu;

internal sealed class ReplayDisplayInfo
{
    public string FullPath { get; init; }
    public string FileName { get; init; }
    public string WorldName { get; init; }
    public string WorldNameRaw { get; init; }
    public string PlayerName { get; init; }
    public string PlayerNameRaw { get; init; }
    public TimeSpan Duration { get; init; }
    public string DurationText => FormatDurationText(Duration);
    public uint DurationTicks { get; init; }
    public DateTime Date { get; init; }
    public long FileSizeBytes { get; init; }
    public string FileSizeText => FormatFileSizeText(FileSizeBytes);
    public string MetadataTooltip { get; init; }
    public ReplayPlayerSnapshot PlayerSnapshot { get; init; }

    public static ReplayDisplayInfo FromFile(string path)
    {
        try
        {
            ReplayInspectionReport report = ReplayInspector.InspectMetadata(path);
            return string.IsNullOrWhiteSpace(report.Error) ? FromReport(path, report) : FromFallback(path, report.Error);
        }
        catch (Exception e)
        {
            string fileName = Path.GetFileName(path);
            Log.Warn($"Failed to read replay metadata for {fileName}: {e.Message}");
            return FromFallback(path, e.Message);
        }
    }

    internal static ReplayDisplayInfo FromFallback(string path, string error)
    {
        string fileName = Path.GetFileName(path);
        string worldName = InferWorldName(fileName);
        long fileSizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0;

        return new ReplayDisplayInfo
        {
            FullPath = path,
            FileName = fileName,
            WorldName = Compact(worldName),
            WorldNameRaw = worldName,
            PlayerName = "-",
            PlayerNameRaw = "-",
            Duration = TimeSpan.Zero,
            DurationTicks = 0,
            Date = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue,
            FileSizeBytes = fileSizeBytes,
            MetadataTooltip = $"Name: {fileName}\nError: {error}",
            PlayerSnapshot = null
        };
    }

    private static string BuildMetadataTooltip(ReplayInspectionReport report)
    {
        ReplayMetadata metadata = report.Metadata;
        string[] modNames = metadata?.ModNames?.Where(m => m != "ModLoader").ToArray() ?? [];

        List<string> lines =
        [
            $"Name: {Path.GetFileName(report.Path)}"
        ];

        if (!string.IsNullOrWhiteSpace(metadata?.PlayerName))
            lines.Add($"Player: {metadata.PlayerName}");

        if (!string.IsNullOrWhiteSpace(metadata?.WorldName))
            lines.Add($"World: {metadata.WorldName}");

        if (metadata?.TickRate > 0)
            lines.Add($"Tick rate: {metadata.TickRate}");

        if (metadata?.DurationTicks > 0)
            lines.Add($"Ticks: {metadata.DurationTicks:N0}");

        if (modNames.Length > 0)
            lines.Add($"Mods ({modNames.Length}): {FormatModNames(modNames)}");

        if (!string.IsNullOrWhiteSpace(report.Error))
            lines.Add($"Error: {report.Error}");

        return string.Join("\n", lines);
    }

    //private static string BuildMetadataTooltip(ReplayInspectionReport report)
    //{
    //    ReplayMetadata metadata = report.Metadata;
    //    int tickRate = metadata?.TickRate > 0 ? metadata.TickRate : 60;
    //    string finalized = metadata?.Finalized == true ? "Yes" : "No";
    //    string cleanEof = report.HasCleanEndMarker ? "Yes" : "No";
    //    string endReason = string.IsNullOrWhiteSpace(metadata?.EndReason) ? "Unknown" : metadata.EndReason;
    //    string[] modNames = metadata?.ModNames?.Where(m => m != "ModLoader").ToArray() ?? [];

    //    List<string> lines =
    //    [
    //        $"Name: {Path.GetFileName(report.Path)}",
    //        //$"Format: v{report.FormatVersion}",
    //        //$"World ID: {metadata?.WorldId ?? 0}",
    //        //$"Tick rate: {tickRate}",
    //        //$"Blocks: {report.BlockCount:N0}",
    //        $"Packets: {report.PacketCount:N0}",
    //        //$"Malformed packets: {report.MalformedPacketDataCount:N0}",
    //        //$"Clean EOF: {cleanEof}",
    //        //$"Finalized: {finalized}",
    //        //$"End reason: {endReason}",
    //        //$"Mod Count: {modNames.Length:N0}"
    //    ];

    //    if (modNames.Length > 0)
    //        lines.Add($"Mods ({modNames.Length}): {FormatModNames(modNames)}");

    //    if (!string.IsNullOrWhiteSpace(report.Error))
    //        lines.Add($"Error: {report.Error}");

    //    return string.Join("\n", lines);
    //}

    private static string FormatModNames(string[] modNames)
    {
        const int maxShown = 8;
        string text = string.Join(", ", modNames.Length > maxShown ? modNames[..maxShown] : modNames);
        return modNames.Length > maxShown ? $"{text}, +{modNames.Length - maxShown:N0} more" : text;
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

        if (name.StartsWith("SP_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("MP_", StringComparison.OrdinalIgnoreCase))
        {
            name = name[3..];
        }

        const int timestampLength = 19; // yyyy-MM-dd_HH-mm-ss
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

    public static ReplayDisplayInfo FromReport(string path, ReplayInspectionReport report)
    {
        string fileName = Path.GetFileName(path);
        string worldName = InferWorldName(fileName);
        string playerName = "-";
        DateTime date = File.GetLastWriteTime(path);
        long fileSizeBytes = new FileInfo(path).Length;
        ReplayMetadata metadata = report.Metadata;

        if (!string.IsNullOrWhiteSpace(metadata?.WorldName))
            worldName = metadata.WorldName;

        if (!string.IsNullOrWhiteSpace(metadata?.PlayerName))
            playerName = metadata.PlayerName;

        if (DateTime.TryParse(metadata?.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime createdUtc))
            date = createdUtc.ToLocalTime();

        uint durationTicks = metadata?.DurationTicks > 0 ? metadata.DurationTicks : report.DurationTicks;
        int tickRate = metadata?.TickRate > 0 ? metadata.TickRate : 60;

        return new ReplayDisplayInfo
        {
            FullPath = path,
            FileName = fileName,
            WorldName = Compact(worldName),
            WorldNameRaw = worldName,
            PlayerName = Compact(playerName),
            PlayerNameRaw = playerName,
            Duration = BuildDuration(durationTicks, tickRate),
            DurationTicks = durationTicks,
            Date = date,
            FileSizeBytes = fileSizeBytes,
            MetadataTooltip = BuildMetadataTooltip(report),
            PlayerSnapshot = metadata?.PlayerSnapshot
        };
    }
}
