using Reese.Common.Spectator;

namespace Reese.Common.Replayer.GhostHooks;

internal class GhostDisableItemUsePlayer : ModPlayer
{
    public override bool CanUseItem(Item item)
    {
        return !Player.ghost && !SpectatorMode.IsReplayClient(Player);
    }

    public override void PreUpdate()
    {
        if (Main.drawingPlayerChat || Main.ingameOptionsWindow || Main.gameMenu)
            return;

        if (!Player.ghost && !SpectatorMode.IsReplayClient(Player))
            return;

        Player.controlUseItem = false;
        Player.releaseUseItem = false;
        Player.channel = false;
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        Player.reuseDelay = 0;
    }
}
