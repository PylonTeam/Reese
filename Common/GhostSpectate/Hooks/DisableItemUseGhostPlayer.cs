using Reese.Common.GhostSpectate.SpectatorMode;

namespace Reese.Common.GhostSpectate.Hooks;

internal class DisableItemUseGhostPlayer : ModPlayer
{
    public override bool CanUseItem(Item item)
    {
        return !Player.ghost && !SpectatorModeSystem.IsInSpectateMode(Player);
    }

    public override void PreUpdate()
    {
        if (!Player.ghost && !SpectatorModeSystem.IsInSpectateMode(Player))
            return;

        Player.controlUseItem = false;
        Player.releaseUseItem = false;
        Player.channel = false;
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        Player.reuseDelay = 0;
    }
}
