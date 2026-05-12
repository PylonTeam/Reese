namespace Reese.Common.Replayer;

public static class ReplayMode
{
    public static bool IsReplayPlayback => ReplayPlayback.IsReplayPlayback;

    public static bool IsInReplayMode(Player player)
    {
        return IsReplayPlayback &&
               player?.active == true &&
               player.whoAmI == Main.myPlayer;
    }

    public static bool IsInPlayerMode(Player player)
    {
        return player?.active == true &&
               !IsInReplayMode(player) &&
               player.whoAmI != ReplayPlayback.RecordClientIndex;
    }
}
