using Reese.Core.Debug;

namespace Reese.Common.ReplayTool;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        Log.Chat("Replay started");
        ModContent.GetInstance<ReplayToolPanelSystem>().Open();
    }
}
