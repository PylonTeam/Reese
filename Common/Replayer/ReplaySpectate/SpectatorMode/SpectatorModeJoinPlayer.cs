using Reese.Common.Replayer;

namespace Reese.Common.Replayer.ReplaySpectate.SpectatorMode;

/// <summary>
/// Applies forced spectator mode after SSC has finished loading.
/// </summary>
public class SpectatorModeJoinPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        base.OnEnterWorld();

        if (ReplaySession.IsReplayPlayback)
            SpectatorModeSystem.RequestSetLocalMode(SpectateMode.Spectator);
    }
}
