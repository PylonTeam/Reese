using Reese.Common.Spectator;

namespace Reese.Common.Replay.GhostHooks;

internal class GhostDisableItemUsePlayer : ModPlayer
{
    public override bool CanUseItem(Item item)
    {
        return !Player.ghost && !SpectatorMode.IsPlaybackPlayer(Player);
    }

    public override void PreUpdate()
    {
        if (Main.drawingPlayerChat || Main.ingameOptionsWindow || Main.gameMenu)
            return;

        if (!Player.ghost && !SpectatorMode.IsPlaybackPlayer(Player))
            return;

        Player.controlUseItem = false;
        Player.releaseUseItem = false;
        Player.channel = false;
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        Player.reuseDelay = 0;
    }
}
