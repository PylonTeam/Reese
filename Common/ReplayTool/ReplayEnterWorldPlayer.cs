using Reese.Core.Debug;
using Reese.Common.Replayer;
using Reese.Common.ReplaySpectate.SpectatorMode;

namespace Reese.Common.ReplayTool;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        Log.Chat("Replay started");
        SpectatorModeSystem.RequestSetLocalMode(SpectateMode.Spectator);
        ModContent.GetInstance<ReplayToolPanelSystem>().Open();
    }
}
