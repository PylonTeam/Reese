using Reese.Core.Debug;
using Reese.Common.ReplaySpectate.SpectatorMode;
using Reese.Common.ReplayControls;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        Log.Chat("Replay started");
        SpectatorModeSystem.RequestSetLocalMode(SpectateMode.Spectator);
        ModContent.GetInstance<ReplayControlsPanelUISystem>().Open();
    }
}
