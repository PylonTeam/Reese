namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;

internal enum SpectatorPlayerDrawMode
{
    FullPlayer,
    PlayerHeads,
    None
}

internal static class SpectatorClientSettings
{
    public static SpectatorPlayerDrawMode DrawPlayers { get; set; } = SpectatorPlayerDrawMode.FullPlayer;
    public static bool RightClickTeleport { get; set; } = true;

    public static void CycleDrawPlayers()
    {
        DrawPlayers = DrawPlayers switch
        {
            SpectatorPlayerDrawMode.FullPlayer => SpectatorPlayerDrawMode.PlayerHeads,
            SpectatorPlayerDrawMode.PlayerHeads => SpectatorPlayerDrawMode.None,
            _ => SpectatorPlayerDrawMode.FullPlayer
        };
    }

    public static string DrawPlayersLabel => DrawPlayers switch
    {
        SpectatorPlayerDrawMode.FullPlayer => "Full Player",
        SpectatorPlayerDrawMode.PlayerHeads => "Player Heads",
        _ => "None"
    };

    public static void ToggleRightClickTeleport() => RightClickTeleport = !RightClickTeleport;
}
