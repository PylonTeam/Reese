namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal static class SpectateHudClientSettings
{
    public static int RowsVisible { get; private set; } = 2;
    public static int PlayersPerRow { get; private set; } = 8;
    public static bool ShowPlayer { get; private set; } = true;
    public static bool ShowPlayerName { get; private set; } = true;
    public static bool ShowPlayerDistance { get; private set; } = true;
    public static bool ShowPlayerDetails { get; private set; } = true;
    public static bool ShowDescription { get; private set; } = true;
    public static int Revision { get; private set; }

    public static void CycleRowsVisible()
    {
        RowsVisible = RowsVisible % 3 + 1;
        Touch();
    }

    public static void CyclePlayersPerRow()
    {
        PlayersPerRow = PlayersPerRow % 8 + 1;
        Touch();
    }

    public static void ToggleShowPlayer() { ShowPlayer = !ShowPlayer; Touch(); }
    public static void ToggleShowPlayerName() { ShowPlayerName = !ShowPlayerName; Touch(); }
    public static void ToggleShowPlayerDistance() { ShowPlayerDistance = !ShowPlayerDistance; Touch(); }
    public static void ToggleShowPlayerDetails() { ShowPlayerDetails = !ShowPlayerDetails; Touch(); }
    public static void ToggleShowDescription() { ShowDescription = !ShowDescription; Touch(); }

    private static void Touch()
    {
        Revision++;
    }
}
