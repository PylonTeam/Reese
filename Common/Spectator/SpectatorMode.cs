using Reese.Common.Replayer;
using Reese.Core.Configs;

namespace Reese.Common.Spectator;

internal static class SpectatorMode
{
    public static bool IsReplay => ReplayPlayback.IsReplayPlayback;
    public static bool IsLocalReplayClient => ReplayPlayback.IsPlayerReplayClient(Main.LocalPlayer);
    public static bool IsLocalGhost => IsGhost(Main.LocalPlayer);
    public static bool IsLocalLiveGhost => IsLocalGhost && !IsReplay;
    public static bool CanUseReplayHud => IsReplay;
    public static bool CanUseGhostHud => IsGhostSpectatingEnabled && IsLocalLiveGhost;
    public static bool CanSpectate => IsLocalReplayClient || IsGhostSpectatingEnabled && IsLocalGhost;
    private static ServerConfig.GhostSpectatingConfig GhostSpectatingConfig => ModContent.GetInstance<ServerConfig>()?.ghostSpectatingConfig;
    public static bool IsGhostSpectatingEnabled => GhostSpectatingConfig?.IsGhostSpectatingEnabled == true;
    public static bool CanDrawOtherGhosts => IsGhostSpectatingEnabled && GhostSpectatingConfig?.DrawGhosts == true;
    public static bool CanDrawOtherGhostNameplates => IsGhostSpectatingEnabled && GhostSpectatingConfig?.DrawGhostsNameplates == true;

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

    public static bool ShouldDrawGhost(Player player)
    {
        if (player?.active != true || !player.ghost)
            return true;

        if (IsReplay)
            return true;

        return player.whoAmI == Main.myPlayer || CanDrawOtherGhosts;
    }

    public static bool ShouldDrawGhostNameplate(Player player)
    {
        if (player?.active != true || !player.ghost)
            return true;

        if (IsReplay)
            return true;

        return player.whoAmI == Main.myPlayer || CanDrawOtherGhostNameplates;
    }
}
