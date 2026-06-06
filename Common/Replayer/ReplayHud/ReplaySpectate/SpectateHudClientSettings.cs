using Terraria.Localization;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal enum SpectateHudSortMode
{
    Alphabetical,
    Distance,
    Health,
    Id,
    Teams,
}

internal enum EntityHudMode
{
    Full,
    Head,
    Detailed,
}

internal static class SpectateHudClientSettings
{
    public static int RowsVisible { get; private set; } = 2;
    public static SpectateHudSortMode SortMode { get; private set; } = SpectateHudSortMode.Teams;
    public static string SortModeDisplayName => GetSortModeDisplayName(SortMode);
    public static EntityHudMode EntityHudMode { get; private set; } = EntityHudMode.Full;
    public static string EntityHudModeDisplayName => Loc.Get("ReplayHud.Settings.EntityHudMode." + EntityHudMode);
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
            SpectateHudSortMode.Alphabetical => SpectateHudSortMode.Distance,
            SpectateHudSortMode.Distance => SpectateHudSortMode.Health,
            SpectateHudSortMode.Health => SpectateHudSortMode.Id,
            SpectateHudSortMode.Id => SpectateHudSortMode.Teams,
            _ => SpectateHudSortMode.Alphabetical
        };

        Touch();
    }

    public static void CycleEntityHudMode()
    {
        EntityHudMode = EntityHudMode switch
        {
            EntityHudMode.Full => EntityHudMode.Head,
            EntityHudMode.Head => EntityHudMode.Detailed,
            _ => EntityHudMode.Full
        };

        Revision++;
    }

    public static void ToggleShowPlayer() { ShowPlayer = !ShowPlayer; Touch(); }
    public static void ToggleShowPlayerNameAndDistance() { ShowPlayerNameAndDistance = !ShowPlayerNameAndDistance; Touch(); }
    public static void ToggleShowDescription() { ShowDescription = !ShowDescription; Touch(); }

    private static string GetSortModeDisplayName(SpectateHudSortMode sortMode)
    {
        return sortMode switch
        {
            SpectateHudSortMode.Alphabetical => Language.GetTextValue("BestiaryInfo.Sort_Alphabetical"),
            SpectateHudSortMode.Distance => Loc.Get("ReplayHud.Settings.SortMode.Distance"),
            SpectateHudSortMode.Health => Language.GetTextValue("BestiaryInfo.Life"),
            SpectateHudSortMode.Id => Language.GetTextValue("BestiaryInfo.Sort_ID"),
            SpectateHudSortMode.Teams => Loc.Get("ReplayHud.Settings.SortMode.Teams"),
            _ => Loc.Get("ReplayHud.Settings.SortMode.Teams")
        };
    }

    //private static string GetEntityHudModeDisplayName(EntityHudMode mode)
    //{
    //    return mode switch
    //    {
    //        EntityHudMode.Full => "Full",
    //        EntityHudMode.Head => "Head",
    //        EntityHudMode.Detailed => "Detailed",
    //        _ => "Full"
    //    };
    //}

    private static void Touch()
    {
        Revision++;
    }
}
