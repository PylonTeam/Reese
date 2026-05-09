using Reese.Core.Debug;
using Reese.Common.GhostSpectate.SpectatorMode;
using Reese.Common.Replayer;

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
