using Reese.Common.Replayer.ReplayHud.ReplaySpectate.Stats;
using System;

namespace Reese.Common.MainMenu.UI;

/// <summary>
/// Ugh, somehow this is a PlayerStatSnapshot but it's fine because they share common properties and it avoids a lot of duplicate code. Yes, this is gross, but it's only used in the main menu and it works, so whatever.
/// </summary>
public static class MainMenuReplayStats
{
    #region Main menu stats
    public static PlayerStatSnapshot BuildMainMenuWorldNameStat(string worldName)
    {
        worldName = string.IsNullOrWhiteSpace(worldName) ? "-" : worldName.Trim();
        return new PlayerStatSnapshot("World", worldName, $"World: {worldName}", Ass.Icon_Biome, null);
    }

    public static PlayerStatSnapshot BuildMainMenuDateStat(DateTime date)
    {
        if (date == DateTime.MinValue)
            return new PlayerStatSnapshot("Created", "Unknown", "Unknown date", Ass.Icon_Watch, null);

        //string display = date.ToString("d MMM HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string display = date.ToString("dd/M  HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string hover = $"Date created: {date.ToString("d MMM yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)}";

        return new PlayerStatSnapshot("Created", display, hover, Ass.Icon_Watch, null);
    }

    public static PlayerStatSnapshot BuildMainMenuLengthStat(uint durationTicks)
    {
        if (durationTicks == 0)
            return new PlayerStatSnapshot("Length", "Unknown", "Unknown length", Ass.Icon_Watch, null);

        string display = FormatDurationText(durationTicks);
        return new PlayerStatSnapshot("Length", display, $"Length: {FormatLengthHoverText(durationTicks)}", Ass.Icon_Watch, null);
    }

    public static PlayerStatSnapshot BuildMainMenuSizeStat(long bytes)
    {
        if (bytes <= 0)
            return new PlayerStatSnapshot("Size", "Unknown", "Unknown size", Ass.Icon_Watch, null);

        string display = FormatFileSizeText(bytes);
        return new PlayerStatSnapshot("Size", display, $"Size: {display}", Ass.Icon_Watch, null);
    }
    private static string FormatDurationText(uint durationTicks)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(durationTicks / 60d);

        return timeSpan.TotalHours >= 1d
            ? $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}"
            : $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }
    public static PlayerStatSnapshot BuildMainMenuModsStat(string[] modNames)
    {
        modNames ??= Array.Empty<string>();

        if (modNames.Length == 0)
            return new PlayerStatSnapshot("Mods", "Unknown", "Unknown mods used", Ass.Icon_CheckmarkGreen, null);

        string display = modNames.Length == 1 ? "1 mod" : $"{modNames.Length} mods";
        string hover = BuildModsHoverText(modNames);

        return new PlayerStatSnapshot("Mods", display, hover, Ass.Icon_CheckmarkGreen, null);
    }

    private static string BuildModsHoverText(string[] modNames)
    {
        if (modNames == null || modNames.Length == 0)
            return "Unknown mods";

        string text = "Mods used in replay:";

        foreach (string modName in modNames)
        {
            if (string.IsNullOrWhiteSpace(modName))
                continue;

            text += "\n" + modName.Trim();
        }

        return text == "Mods used in replay:" ? "Mods used in replay:\n-" : text;
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
    #endregion

}
