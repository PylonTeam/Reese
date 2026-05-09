namespace Reese.Common.GhostSpectate.SpectatorMode;

/// <summary>
/// Applies forced spectator mode after SSC has finished loading.
/// </summary>
public class SpectatorModeJoinPlayer : ModPlayer
{
    int delayTicks = 0;

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

        //var serverConfig = ModContent.GetInstance<ServerConfig>();

        if (ReplaySession.IsReplayPlayback)
        {
            SpectatorModeNetHandler.SendRequestSetMode(Main.myPlayer, SpectateMode.Spectator);
        }

        //delayTicks = 30;
        //Main.LocalPlayer.ghost = true;
        //Log.Chat($"Force spectating is enabled, will send request to become a spectator in 30 ticks for player id: {Player.whoAmI}");
    }
}