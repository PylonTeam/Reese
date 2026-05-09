using Reese.Common.Replayer;
using Reese.Core.Debug;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Reese.Common.MainMenu;

internal sealed class ReplayDisplayInfo
{
    public string FileName { get; init; }
    public string WorldName { get; init; }
    public string WorldNameRaw { get; init; }
    public string PlayerName { get; init; }
    public string PlayerNameRaw { get; init; }
    public string DurationText { get; init; }
    public DateTime Date { get; init; }
    public string MetadataTooltip { get; init; }
    public ReplayPlayerSnapshot PlayerSnapshot { get; init; }

    public static ReplayDisplayInfo FromFile(string path)
    {
        string fileName = Path.GetFileName(path);
        string worldName = InferWorldName(fileName);
        string playerName = "-";
        string durationText = "--:--:--";
        DateTime date = File.GetLastWriteTime(path);
        string metadataTooltip = string.Empty;
        ReplayPlayerSnapshot playerSnapshot = null;

        try
        {
            ReplayInspectionReport report = ReplayInspector.Inspect(path);
            ReplayMetadata metadata = report.Metadata;
            metadataTooltip = BuildMetadataTooltip(report);

            if (!string.IsNullOrWhiteSpace(metadata?.WorldName))
                worldName = metadata.WorldName;

            if (!string.IsNullOrWhiteSpace(metadata?.PlayerName))
                playerName = metadata.PlayerName;

            if (DateTime.TryParse(metadata?.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime createdUtc))
                date = createdUtc.ToLocalTime();

            playerSnapshot = metadata?.PlayerSnapshot;

            uint durationTicks = metadata?.DurationTicks > 0 ? metadata.DurationTicks : report.DurationTicks;
            int tickRate = metadata?.TickRate > 0 ? metadata.TickRate : 60;
            durationText = FormatDuration(durationTicks, tickRate);
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to read replay metadata for {fileName}: {e.Message}");
        }

        return new ReplayDisplayInfo
        {
            FileName = fileName,
            WorldName = Compact(worldName),
            WorldNameRaw = worldName,
            PlayerName = Compact(playerName),
            PlayerNameRaw = playerName,
            DurationText = durationText,
            Date = date,
            MetadataTooltip = metadataTooltip,
            PlayerSnapshot = playerSnapshot
        };
    }

    private static string BuildMetadataTooltip(ReplayInspectionReport report)
    {
        ReplayMetadata metadata = report.Metadata;
        int tickRate = metadata?.TickRate > 0 ? metadata.TickRate : 60;
        string finalized = metadata?.Finalized == true ? "Yes" : "No";
        string cleanEof = report.HasCleanEndMarker ? "Yes" : "No";
        string endReason = string.IsNullOrWhiteSpace(metadata?.EndReason) ? "Unknown" : metadata.EndReason;
        string[] modNames = metadata?.ModNames ?? [];

        List<string> lines =
        [
            $"Format: v{report.FormatVersion}",
            $"World ID: {metadata?.WorldId ?? 0}",
            $"Tick rate: {tickRate}",
            $"Blocks: {report.BlockCount:N0}",
            $"Packets: {report.PacketCount:N0}",
            $"Malformed packets: {report.MalformedPacketDataCount:N0}",
            $"Clean EOF: {cleanEof}",
            $"Finalized: {finalized}",
            $"End reason: {endReason}",
            $"Mod Count: {modNames.Length:N0}"
        ];

        if (modNames.Length > 0)
            lines.Add($"Mod Names: {FormatModNames(modNames)}");

        if (!string.IsNullOrWhiteSpace(report.Error))
            lines.Add($"Error: {report.Error}");

        return string.Join("\n", lines);
    }

    private static string FormatModNames(string[] modNames)
    {
        const int maxShown = 8;
        string text = string.Join(", ", modNames.Length > maxShown ? modNames[..maxShown] : modNames);
        return modNames.Length > maxShown ? $"{text}, +{modNames.Length - maxShown:N0} more" : text;
    }

    private static string FormatDuration(uint ticks, int tickRate)
    {
        if (ticks == 0 || tickRate <= 0)
            return "00:00:00";

        var elapsed = TimeSpan.FromSeconds(ticks / (double)tickRate);
        return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
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
}
