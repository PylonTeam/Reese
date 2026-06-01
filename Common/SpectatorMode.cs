using Reese.Common.Replayer;

namespace Reese.Common;

internal static class SpectatorMode
{
    public static bool IsReplay => ReplayPlayback.IsReplayPlayback;
    public static bool IsLocalReplayClient => ReplayPlayback.IsPlayerReplayClient(Main.LocalPlayer);
    public static bool IsLocalGhost => IsGhost(Main.LocalPlayer);
    public static bool IsLocalLiveGhost => IsLocalGhost && !IsReplay;
    public static bool CanUseReplayHud => IsReplay;
    public static bool CanUseGhostHud => IsLocalLiveGhost;
    public static bool CanSpectate => IsLocalReplayClient || IsLocalGhost;

    public static bool IsSpectator(Player player)
    {
        return IsGhost(player) || ReplayPlayback.IsPlayerReplayClient(player);
    }

    public static bool IsReplayClient(Player player)
    {
        return ReplayPlayback.IsPlayerReplayClient(player);
    }

    public static bool IsGhost(Player player)
    {
        return player?.active == true && player.ghost;
    }
}
