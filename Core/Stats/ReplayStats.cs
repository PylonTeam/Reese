using Reese.Common.MainMenu;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Reese.Core.Stats;

/// <summary>
/// Uses <see cref="StatDrawer"/> to draw stats in the <see cref="ReplayListItem"/>
/// </summary>

public static class ReplayStats
{
    public static ReplayStatSnapshot WorldName(string worldName)
    {
        string text = string.IsNullOrWhiteSpace(worldName) ? "-" : worldName.Trim();
        return new("World", text, $"World: {text}", Ass.IconBiome, null);
    }

    public static ReplayStatSnapshot Created(DateTime date)
    {
        if (date == DateTime.MinValue)
            return Unknown("Created", "Unknown date");

        string text = date.ToString("d MMM HH:mm", CultureInfo.InvariantCulture);
        string hoverText = $"Date created: {date.ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture)}";

        return new("Created", text, hoverText, null, null);
    }

    public static ReplayStatSnapshot Length(uint durationTicks)
    {
        if (durationTicks == 0)
            return Unknown("Length", "Unknown length");

        string text = FormatDurationText(durationTicks);
        return new("Length", text, $"Length: {FormatLengthHoverText(durationTicks)}", null, null);
    }

    public static ReplayStatSnapshot Mods(string[] modNames)
    {
        string[] replayMods = CleanModNames(modNames);

        if (replayMods.Length == 0)
            return Unknown("Mods", "Unknown mods used");

        string text = replayMods.Length == 1 ? "1 mod" : $"{replayMods.Length} mods";
        return new("Mods", text, BuildModsHoverText(replayMods), null, null);
    }

    public static ReplayStatSnapshot Size(long bytes)
    {
        if (bytes <= 0)
            return Unknown("Size", "Unknown size");

        string text = FormatFileSizeText(bytes);
        return new("Size", text, $"Size: {text}", null, null);
    }

    public static string GetCurrentWorldName()
    {
        return string.IsNullOrWhiteSpace(Main.worldName) ? "Unknown" : Main.worldName.Trim();
    }

    public static string[] GetCurrentModNames()
    {
        return ModLoader.Mods
            .Select(x => x?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ReplayStatSnapshot Unknown(string label, string hoverText)
    {
        return new(label, "Unknown", hoverText, null, null);
    }

    private static string FormatDurationText(uint durationTicks)
    {
        TimeSpan time = TimeSpan.FromSeconds(durationTicks / 60d);

        if (time.TotalHours >= 1d)
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}";

        return $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private static string FormatLengthHoverText(uint durationTicks)
    {
        TimeSpan time = TimeSpan.FromSeconds(durationTicks / 60d);
        List<string> parts = [];

        AddLengthPart(parts, (int)time.TotalHours, "hour");
        AddLengthPart(parts, time.Minutes, "minute");

        if (time.Seconds > 0 || parts.Count == 0)
            AddLengthPart(parts, time.Seconds, "second");

        return string.Join(", ", parts);
    }

    private static void AddLengthPart(List<string> parts, int value, string unit)
    {
        if (value <= 0)
            return;

        parts.Add($"{value} {unit}{(value == 1 ? "" : "s")}");
    }

    private static string FormatFileSizeText(long bytes)
    {
        double kilobytes = bytes / 1024d;

        if (kilobytes < 1d)
            return "<1 KB";

        return kilobytes.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") + " KB";
    }

    private static string[] CleanModNames(string[] modNames)
    {
        if (modNames is null || modNames.Length == 0)
            return [];

        return modNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => !string.Equals(x, "ModLoader", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildModsHoverText(string[] replayMods)
    {
        if (replayMods.Length == 0)
            return "Unknown mods";

        HashSet<string> enabledMods = ModLoader.Mods
            .Select(x => x?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !string.Equals(x, "ModLoader", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] enabledReplayMods = replayMods.Where(enabledMods.Contains).ToArray();
        string[] missingMods = replayMods.Where(x => !enabledMods.Contains(x)).ToArray();

        List<string> lines = ["Mods:"];

        foreach (string modName in enabledReplayMods)
            lines.Add($"[mi:{modName}]{modName}");

        if (missingMods.Length > 0)
        {
            lines.Add("");
            lines.Add("Missing mods:");

            foreach (string modName in missingMods)
                lines.Add($"[mi:{modName}][c/ff5555:{modName} (disabled)]");
        }

        return string.Join("\n", lines);
    }
}

public readonly record struct ReplayStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);