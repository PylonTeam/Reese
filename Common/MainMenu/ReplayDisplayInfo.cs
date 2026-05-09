using Reese.Core.Debug;
using System;
using System.IO;

namespace Reese.Common.MainMenu;

internal sealed class ReplayDisplayInfo
{
    public string FileName { get; init; }
    public string WorldName { get; init; }
    public string PlayerName { get; init; }
    public string PlayerNameRaw { get; init; }
    public string DurationText { get; init; }

    public static ReplayDisplayInfo FromFile(string path)
    {
        string fileName = Path.GetFileName(path);
        string worldName = InferWorldName(fileName);
        string playerName = "-";
        string durationText = "--:--";

        try
        {
            ReplayInspectionReport report = ReplayInspector.Inspect(path);
            ReplayMetadata metadata = report.Metadata;

            if (!string.IsNullOrWhiteSpace(metadata?.WorldName))
                worldName = metadata.WorldName;

            if (!string.IsNullOrWhiteSpace(metadata?.PlayerName))
                playerName = metadata.PlayerName;

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
            PlayerName = Compact(playerName),
            PlayerNameRaw = playerName,
            DurationText = durationText
        };
    }

    private static string FormatDuration(uint ticks, int tickRate)
    {
        if (ticks == 0 || tickRate <= 0)
            return "00:00";

        var elapsed = TimeSpan.FromSeconds(ticks / (double)tickRate);
        if (elapsed.TotalHours >= 1d)
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

        return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
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
