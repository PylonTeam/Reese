using Reese.Common.Replay;
using Reese.Common.Replayer;
using Reese.Core.Configs;

namespace Reese.Common.Spectator;

internal static class SpectatorMode
{
    public static bool IsReplay => Playback.IsPlaying;
    public static bool IsLocalPlayerPlayback => IsPlaybackPlayer(Main.LocalPlayer);
    public static bool IsLocalGhost => IsGhost(Main.LocalPlayer);
    public static bool IsLocalLiveGhost => IsLocalGhost && !IsReplay;
    public static bool CanUseReplayHud => IsReplay;
    public static bool CanUseGhostHud => IsGhostSpectatingEnabled && IsLocalLiveGhost;
    public static bool CanSpectate => IsLocalPlayerPlayback || IsGhostSpectatingEnabled && IsLocalGhost;
    private static ServerConfig.GhostSpectatingConfig GhostSpectatingConfig => ModContent.GetInstance<ServerConfig>()?.ghostSpectatingConfig;
    public static bool IsGhostSpectatingEnabled => GhostSpectatingConfig?.IsGhostSpectatingEnabled == true;
    public static bool CanDrawOtherGhosts => IsGhostSpectatingEnabled && GhostSpectatingConfig?.DrawGhosts == true;
    public static bool CanDrawOtherGhostNameplates => IsGhostSpectatingEnabled && GhostSpectatingConfig?.DrawGhostsNameplates == true;

    public static bool IsSpectator(Player player)
    {
        return IsGhost(player) || IsPlaybackPlayer(player);
    }

    public static bool IsPlaybackPlayer(Player player)
    {
        return Playback.IsPlayingReplay(out var replay) && replay.MetaInfo.WhoAmI == player.whoAmI;
    }

    public static bool IsGhost(Player player)
    {
        return player?.active == true && player.ghost;
    }

    /// <summary>
    /// Ghost check for world simulation (spawning, despawning), safe to call on a dedicated server.
    /// <para/>
    /// <see cref="Player.ghost"/> is set client-side and only reaches the server inside
    /// MessageID.PlayerControls, which is sent on input change, so it lags behind a mode switch and stays
    /// stale while a ghost holds still. <see cref="SpectatorModeSystem.Modes"/> is server-authoritative,
    /// so it is the one that is trusted here; the flag is kept as an OR so vanilla hardcore ghosts count too.
    /// </summary>
    public static bool IsSpectatingForWorldLogic(Player player)
    {
        return player?.active == true && (player.ghost || SpectatorModeSystem.IsInSpectateMode(player));
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
