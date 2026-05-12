using Reese.Common.Replayer.ReplayHud.ReplaySpectate.Stats;
using ReLogic.Content;
using System;
using System.Linq;

namespace Reese.Common.MainMenu;

/// <summary>
/// Stats for the <see cref="ReplayListItem"/>
/// Uses <see cref="StatDrawer"/>
/// </summary>
public static class ReplayStats
{
    public static ReplayStatSnapshot BuildMainMenuWorldNameStat(string worldName)
    {
        worldName = string.IsNullOrWhiteSpace(worldName) ? "-" : worldName.Trim();
        return new ReplayStatSnapshot("World", worldName, $"World: {worldName}", Ass.Icon_Biome, null);
    }

    public static ReplayStatSnapshot BuildMainMenuDateStat(DateTime date)
    {
        if (date == DateTime.MinValue)
            return new ReplayStatSnapshot("Created", "Unknown", "Unknown date", Ass.Icon_Watch, null);

        // September 28th is the "stress test" date for UI layouts
        //date = new DateTime(2026, 9, 28, 12, 34, 56);

        string display = date.ToString("d MMM HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        //string display = date.ToString("dd/M  HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string hover = $"Date created: {date.ToString("d MMM yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)}";

        return new ReplayStatSnapshot("Created", display, hover, Ass.Icon_Watch, null);
    }

    public static ReplayStatSnapshot BuildMainMenuLengthStat(uint durationTicks)
    {
        if (durationTicks == 0)
            return new ReplayStatSnapshot("Length", "Unknown", "Unknown length", Ass.Icon_Watch, null);

        string display = FormatDurationText(durationTicks);
        return new ReplayStatSnapshot("Length", display, $"Length: {FormatLengthHoverText(durationTicks)}", Ass.Icon_Watch, null);
    }

    public static ReplayStatSnapshot BuildMainMenuSizeStat(long bytes)
    {
        if (bytes <= 0)
            return new ReplayStatSnapshot("Size", "Unknown", "Unknown size", Ass.Icon_Watch, null);

        string display = FormatFileSizeText(bytes);
        return new ReplayStatSnapshot("Size", display, $"Size: {display}", Ass.Icon_Watch, null);
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
            return new ReplayStatSnapshot("Mods", "Unknown", "Unknown mods used", Ass.Icon_CheckmarkGreen, null);

        string display = modNames.Length == 1 ? "1 mod" : $"{modNames.Length} mods";
        string hover = BuildModsHoverText(modNames);

        return new ReplayStatSnapshot("Mods", display, hover, Ass.Icon_CheckmarkGreen, null);
    }

    private static string BuildModsHoverText(string[] modNames)
    {
        if (modNames == null || modNames.Length == 0)
            return "Unknown mods";

        string text = "Mods:";

        foreach (string modName in modNames)
        {
            if (string.IsNullOrWhiteSpace(modName))
                continue;

            text += $"\n[mi:{modName}]{modName.Trim()}";
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
        double megabytes = bytes / 1024d / 1024d;

        if (megabytes >= 100d)
            return (megabytes / 1024d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " GB";

        if (megabytes < 0.1d)
            return "<0.1 MB";

        return megabytes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
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
