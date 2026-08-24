using Reese.Common.Spectator;
using System;

namespace Reese.Common.Replay.ReplayHud;

internal static class ReplayClientSettings
{
    // Zoom settings
    public const float ReplayZoomMin = 0.33f;
    public const float ReplayZoomMax = 5f;

    public static float ReplayZoom { get; private set; } = 1f;
    public static int ReplayZoomPercent => (int)Math.Round(ReplayZoom * 100f);
    public static float ReplayZoomRatio => (ReplayZoom - ReplayZoomMin) / (ReplayZoomMax - ReplayZoomMin);

    public static void SetReplayZoomRatio(float ratio)
    {
        SetReplayZoom(ReplayZoomMin + (ReplayZoomMax - ReplayZoomMin) * Math.Clamp(ratio, 0f, 1f));
    }

    public static void SetReplayZoom(float value)
    {
        if (!SpectatorMode.CanSpectate)
            return;

        value = Math.Clamp(value, ReplayZoomMin, ReplayZoomMax);

        if (Math.Abs(ReplayZoom - value) <= 0.001f)
            return;

        ReplayZoom = value;
        Main.GameZoomTarget = value;
        HudRevision++;
    }

    public static void ImportReplayZoomFromGame(float value)
    {
        if (!SpectatorMode.CanSpectate)
            return;

        value = Math.Clamp(value, ReplayZoomMin, ReplayZoomMax);

        if (Math.Abs(ReplayZoom - value) <= 0.001f)
            return;

        ReplayZoom = value;
        HudRevision++;
    }

    // Playback HUD sizing
    public const float PlaybackHudWidthMin = 866f;
    public const float PlaybackHudWidthMax = 1600f;

    public static float PlaybackHudWidthRatio { get; private set; } = 0f;
    public static int PlaybackHudWidthPercent => (int)Math.Round(PlaybackHudWidthRatio * 100f);

    public static float GetPlaybackHudWidth()
    {
        float maxWidth = Math.Max(PlaybackHudWidthMin, Math.Min(PlaybackHudWidthMax, Main.screenWidth - 72f));
        return PlaybackHudWidthMin + (maxWidth - PlaybackHudWidthMin) * PlaybackHudWidthRatio;
    }

    public static void SetPlaybackHudWidthRatio(float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);

        if (Math.Abs(PlaybackHudWidthRatio - ratio) <= 0.001f)
            return;

        PlaybackHudWidthRatio = ratio;
        TouchHud();
    }

    // Ghost settings
    public static bool RightClickTeleport { get; set; } = true;
    public static void ToggleRightClickTeleport() => RightClickTeleport = !RightClickTeleport;

    // Display settings (unused)
    public static bool IsCompactModeOn { get; set; } = false;
    public static void ToggleCompactMode() => IsCompactModeOn = !IsCompactModeOn;

    // Draw settings
    public static bool IsDrawPlayersOn { get; set; } = true;
    public static bool IsDrawGhostsOn { get; set; } = true;
    public static bool IsDrawProjectilesOn { get; set; } = true;
    public static bool IsDrawNPCsOn { get; set; } = true;
    public static bool IsDrawItemsOn { get; set; } = true;
    public static bool IsDrawChatOn { get; set; } = true;
    public static bool IsNameplatesOn { get; set; } = true;
    public static void ToggleNameplates() => IsNameplatesOn = !IsNameplatesOn;
    public static void TogglePlayers() => IsDrawPlayersOn = !IsDrawPlayersOn;
    public static void ToggleGhosts() => IsDrawGhostsOn = !IsDrawGhostsOn;
    public static void ToggleProjectiles() => IsDrawProjectilesOn = !IsDrawProjectilesOn;
    public static void ToggleNPCs() => IsDrawNPCsOn = !IsDrawNPCsOn;
    public static void ToggleItems() => IsDrawItemsOn = !IsDrawItemsOn;
    public static void ToggleChat() => IsDrawChatOn = !IsDrawChatOn;

    // HUD settings
    public static bool ShowSpectateHud { get; private set; } = true;
    public static bool ShowPlaybackHud { get; private set; } = true;
    public static bool ShowReplayHudSpeed { get; private set; } = true;
    public static bool ShowReplayHudPlaybackControls { get; private set; } = true;
    public static bool ShowReplayHudSeekbar { get; private set; } = true;
    public static bool ShowBossesSummoned { get; private set; } = true;
    public static bool ShowBossesDefeated { get; private set; } = true;
    public static bool ShowPlayerJoinLeave { get; private set; } = true;
    public static bool ShowPvpDeaths { get; private set; } = true;
    public static bool ShowPveDeaths { get; private set; } = true;
    public static bool ShowInvasions { get; private set; } = true;
    public static int HudRevision { get; private set; }
    public static void ToggleShowSpectateHud() { ShowSpectateHud = !ShowSpectateHud; TouchHud(); }
    public static void ToggleShowPlaybackHud() { ShowPlaybackHud = !ShowPlaybackHud; TouchHud(); }
    public static void ToggleShowReplayHudSpeed() { ShowReplayHudSpeed = !ShowReplayHudSpeed; TouchHud(); }
    public static void ToggleShowReplayHudPlaybackControls() { ShowReplayHudPlaybackControls = !ShowReplayHudPlaybackControls; TouchHud(); }
    public static void ToggleShowReplayHudSeekbar() { ShowReplayHudSeekbar = !ShowReplayHudSeekbar; TouchHud(); }
    public static void ToggleShowBossesSummoned() { ShowBossesSummoned = !ShowBossesSummoned; TouchHud(); }
    public static void ToggleShowBossesDefeated() { ShowBossesDefeated = !ShowBossesDefeated; TouchHud(); }
    public static void ToggleShowPlayerJoinLeave() { ShowPlayerJoinLeave = !ShowPlayerJoinLeave; TouchHud(); }
    public static void ToggleShowPvpDeaths() { ShowPvpDeaths = !ShowPvpDeaths; TouchHud(); }
    public static void ToggleShowPveDeaths() { ShowPveDeaths = !ShowPveDeaths; TouchHud(); }
    public static void ToggleShowInvasions() { ShowInvasions = !ShowInvasions; TouchHud(); }
    private static void TouchHud()
    {
        HudRevision++;
    }

}
