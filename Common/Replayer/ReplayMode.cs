namespace Reese.Common.Replayer;

public static class ReplayMode
{
    public static bool IsReplayPlayback => ReplaySession.IsReplayPlayback;

    public static bool HasValidLocalPlayer()
    {
        if (!IsReplayPlayback || Main.gameMenu || Main.myPlayer is < 0 or >= Main.maxPlayers)
            return false;

        Player local = Main.LocalPlayer;
        return local?.active == true && local.whoAmI == Main.myPlayer;
    }

    public static Player GetLocalPlayer()
    {
        return HasValidLocalPlayer() ? Main.LocalPlayer : null;
    }

    public static bool IsInReplayMode(Player player)
    {
        return IsReplayPlayback &&
               player?.active == true &&
               player.whoAmI == Main.myPlayer &&
               player.whoAmI != ReplaySession.RecordClientIndex;
    }

    public static bool IsInPlayerMode(Player player)
    {
        return player?.active == true &&
               !IsInReplayMode(player) &&
               player.whoAmI != ReplaySession.RecordClientIndex;
    }
}
