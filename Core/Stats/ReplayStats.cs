using Reese.Common.MainMenu;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reese.Core.Stats;

/// <summary>
/// Stats for the <see cref="ReplayListItem"/>
/// Uses <see cref="StatDrawer"/>
/// </summary>
public static class ReplayStats
{
    public static ReplayStatSnapshot BuildMainMenuWorldNameStat(string worldName)
    {
        worldName = string.IsNullOrWhiteSpace(worldName) ? "-" : worldName.Trim();
        return new ReplayStatSnapshot("World", worldName, $"World: {worldName}", Ass.IconBiome, null);
    }

    public static ReplayStatSnapshot BuildMainMenuDateStat(DateTime date)
    {
        if (date == DateTime.MinValue)
            return new ReplayStatSnapshot("Created", "Unknown", "Unknown date", null, null);

        // September 28th is the "stress test" date for UI layouts
        //date = new DateTime(2026, 9, 28, 12, 34, 56);

        string display = date.ToString("d MMM HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        //string display = date.ToString("dd/M  HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string hover = $"Date created: {date.ToString("d MMM yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)}";

        return new ReplayStatSnapshot("Created", display, hover, null, null);
    }

    public static ReplayStatSnapshot BuildMainMenuLengthStat(uint durationTicks)
    {
        if (durationTicks == 0)
            return new ReplayStatSnapshot("Length", "Unknown", "Unknown length", null, null);

        string display = FormatDurationText(durationTicks);
        return new ReplayStatSnapshot("Length", display, $"Length: {FormatLengthHoverText(durationTicks)}", null, null);
    }

    public static ReplayStatSnapshot BuildMainMenuSizeStat(long bytes)
    {
        if (bytes <= 0)
            return new ReplayStatSnapshot("Size", "Unknown", "Unknown size", null, null);

        string display = FormatFileSizeText(bytes);
        return new ReplayStatSnapshot("Size", display, $"Size: {display}", null, null);
    }
    private static string FormatDurationText(uint durationTicks)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(durationTicks / 60d);

        return timeSpan.TotalHours >= 1d
            ? $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}"
            : $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }
    public static ReplayStatSnapshot BuildMainMenuModsStat(string[] modNames)
    {
        modNames ??= [];

        if (modNames.Length == 0)
            return new ReplayStatSnapshot("Mods", "Unknown", "Unknown mods used", null, null);

        string display = modNames.Length == 1 ? "1 mod" : $"{modNames.Length} mods";
        string hover = BuildModsHoverText(modNames);

        return new ReplayStatSnapshot("Mods", display, hover, null, null);
    }

    private static string BuildModsHoverText(string[] modNames)
    {
        if (modNames == null || modNames.Length == 0)
            return "Unknown mods";

        string[] replayMods = modNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => !string.Equals(x, "ModLoader", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (replayMods.Length == 0)
            return "Unknown mods";

        HashSet<string> enabledMods = ModLoader.Mods
            .Select(x => x?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !string.Equals(x, "ModLoader", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] enabledReplayMods = replayMods
            .Where(x => enabledMods.Contains(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] missingMods = replayMods
            .Where(x => !enabledMods.Contains(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string text = "Mods:";

        foreach (string modName in enabledReplayMods)
        {
            //text += $"\n[mi:{modName}][c/55ff55:{modName}]";
            text += $"\n[mi:{modName}]{modName}";
        }

        if (missingMods.Length > 0)
        {
            text += "\n\nMissing mods:";

            foreach (string modName in missingMods)
                text += $"\n[mi:{modName}][c/ff5555:{modName} (disabled)]";
        }

        return text;
    }

    private static string FormatLengthHoverText(uint durationTicks)
    {
        TimeSpan length = TimeSpan.FromSeconds(durationTicks / 60d);
        int hours = (int)length.TotalHours;
        int minutes = length.Minutes;
        int seconds = length.Seconds;
        string text = "";

        AddLengthPart(ref text, hours, "hour");
        AddLengthPart(ref text, minutes, "minute");

        if (seconds > 0 || text.Length == 0)
            AddLengthPart(ref text, seconds, "second");

        return text;
    }

    private static string FormatFileSizeText(long bytes)
    {
#if DEBUG
        //bytes = 1024000; // 1000 KB
        //bytes = 10240000; // 10 000 KB
        //bytes = 102400000; // 100 000 KB
        //bytes = 1024000000; // 1 000 000 KB
#endif
        double kilobytes = bytes / 1024d;

        if (kilobytes < 1d)
            return "<1 KB";

        //return kilobytes.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
        return kilobytes.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ") + " KB";

        //double megabytes = bytes / 1024d / 1024d;

        //if (megabytes >= 100d)
        //    return (megabytes / 1024d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " GB";

        //if (megabytes < 0.1d)
        //    return "<0.1 MB";

        //return megabytes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
    }

    private static void AddLengthPart(ref string text, int value, string unit)
    {
        if (value <= 0)
            return;

        if (text.Length > 0)
            text += ", ";

        text += $"{value} {unit}{(value == 1 ? "" : "s")}";
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
}

public sealed class ReplayStatDefinition
{
    public string Id { get; }
    public string Label { get; }
    public Func<Player, Asset<Texture2D>> GetIcon { get; }
    public Func<Player, string> GetText { get; }
    public Func<Player, string> GetHoverText { get; }
    public Func<Player, Rectangle?> GetIconFrame { get; }

    public ReplayStatSnapshot Build(Player player)
    {
        string text = GetText(player);
        string hoverText = GetHoverText == null ? $"{Label}: {text}" : GetHoverText(player);
        Rectangle? iconFrame = GetIconFrame == null ? null : GetIconFrame(player);

        return new ReplayStatSnapshot(Label, text, hoverText, GetIcon(player), iconFrame);
    }
}

public readonly record struct ReplayStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);
