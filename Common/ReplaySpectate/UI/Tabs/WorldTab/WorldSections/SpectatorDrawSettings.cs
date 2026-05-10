namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;

internal static class SpectatorDrawSettings
{
    public static bool IsDrawPlayersOn { get; set; } = true;
    public static bool IsDrawGhostsOn { get; set; } = true;
    public static bool IsDrawProjectilesOn { get; set; } = true;
    public static bool IsDrawNPCsOn { get; set; } = true;
    public static bool IsDrawItemsOn { get; set; } = true;

    public static void TogglePlayers() => IsDrawPlayersOn = !IsDrawPlayersOn;
    public static void ToggleGhosts() => IsDrawGhostsOn = !IsDrawGhostsOn;
    public static void ToggleProjectiles() => IsDrawProjectilesOn = !IsDrawProjectilesOn;
    public static void ToggleNPCs() => IsDrawNPCsOn = !IsDrawNPCsOn;
    public static void ToggleItems() => IsDrawItemsOn = !IsDrawItemsOn;
}
