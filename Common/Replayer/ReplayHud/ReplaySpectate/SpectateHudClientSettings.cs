namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal enum SpectateHudSortMode
{
    Teams,
    Id,
    Alphabetical,
    Distance
}

internal static class SpectateHudClientSettings
{
    public static int RowsVisible { get; private set; } = 2;
    public static SpectateHudSortMode SortMode { get; private set; } = SpectateHudSortMode.Teams;
    public static string SortModeDisplayName => GetSortModeDisplayName(SortMode);
    public static bool ShowPlayer { get; private set; } = true;
    public static bool ShowPlayerNameAndDistance { get; private set; } = true;
    public static bool ShowPlayerName => ShowPlayerNameAndDistance;
    public static bool ShowPlayerDistance => ShowPlayerNameAndDistance;
    public static bool ShowDescription { get; private set; } = true;
    public static int Revision { get; private set; }

    public static void CycleRowsVisible()
    {
        RowsVisible = RowsVisible % 3 + 1;
        Touch();
    }

    public static void CycleSortMode()
    {
        SortMode = SortMode switch
        {
            SpectateHudSortMode.Teams => SpectateHudSortMode.Id,
            SpectateHudSortMode.Id => SpectateHudSortMode.Alphabetical,
            SpectateHudSortMode.Alphabetical => SpectateHudSortMode.Distance,
            _ => SpectateHudSortMode.Teams
        };

        Touch();
    }

    public static void ToggleShowPlayer() { ShowPlayer = !ShowPlayer; Touch(); }
    public static void ToggleShowPlayerNameAndDistance() { ShowPlayerNameAndDistance = !ShowPlayerNameAndDistance; Touch(); }
    public static void ToggleShowDescription() { ShowDescription = !ShowDescription; Touch(); }

    private static string GetSortModeDisplayName(SpectateHudSortMode sortMode)
    {
        return sortMode switch
        {
            SpectateHudSortMode.Teams => "Teams",
            SpectateHudSortMode.Id => "ID",
            SpectateHudSortMode.Alphabetical => "Alphabetical",
            SpectateHudSortMode.Distance => "Distance",
            _ => "Teams"
        };
    }

    private static void Touch()
    {
        Revision++;
    }
}
