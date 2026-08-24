using Reese.Common.Replayer;
using Reese.Common.Replay.ReplayHud.Ghost;

namespace Reese.Common.Spectator;

/// <summary>
/// Applies forced spectator mode after SSC has finished loading.
/// </summary>
public class SpectatorModeJoinPlayer : ModPlayer
{
    public override void PostUpdate()
    {
        //if (delayTicks <= 0)
        //    return;

        //delayTicks--;

        //if (delayTicks > 0)
        //    return;

        //Log.Chat($"Sending request to become a spectator for player id: {Main.myPlayer}");
        //SpectatorModeNetHandler.SendRequestSetMode(Main.myPlayer, SpectateMode.Spectator);
    }

    public override void OnEnterWorld()
    {
        base.OnEnterWorld();

        if (ReplayPlayback.IsReplayPlayback)
            return;

        if (!SpectatorModeSystem.IsGhostSpectatingEnabled)
        {
            if (!Main.LocalPlayer.dead)
                Main.LocalPlayer.ghost = false;

            ModContent.GetInstance<GhostHudSystem>()?.CloseFullHud();
            ModContent.GetInstance<SpectatorJoinUISystem>()?.Close();
            return;
        }

        if (SpectatorModeSystem.ForceSpectateWhenJoining)
        {
            Main.LocalPlayer.ghost = true;
            SpectatorModeSystem.RequestSetLocalMode(SpectateMode.Spectator);
        }
        else if (SpectatorModeSystem.ShowSpectatorJoinPanel)
        {
            Main.LocalPlayer.ghost = true;
            SpectatorModeSystem.SetModeLocal(Main.myPlayer, SpectateMode.Spectator);
        }

        if (SpectatorModeSystem.ShowSpectatorJoinPanel)
        {
            ModContent.GetInstance<GhostHudSystem>()?.OpenFullHud();
            ModContent.GetInstance<SpectatorJoinUISystem>()?.Open();
        }

        //delayTicks = 30;
        //Main.LocalPlayer.ghost = true;
        //Log.Chat($"Force spectating is enabled, will send request to become a spectator in 30 ticks for player id: {Player.whoAmI}");
    }
}
