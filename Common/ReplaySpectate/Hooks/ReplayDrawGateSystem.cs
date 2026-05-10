using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer;
using Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;
using Terraria.DataStructures;

namespace Reese.Common.ReplaySpectate.Hooks;

internal static class ReplayDrawGate
{
    public static bool ShouldDrawPlayer(Player player)
    {
        if (!ReplaySession.IsReplayPlayback || player?.active != true)
            return true;

        return player.ghost ? SpectatorDrawSettings.IsDrawGhostsOn : SpectatorDrawSettings.IsDrawPlayersOn;
    }

    public static bool ShouldDrawGhost(Player player)
    {
        return !ReplaySession.IsReplayPlayback || player?.ghost != true || SpectatorDrawSettings.IsDrawGhostsOn;
    }

    public static bool ShouldDrawNameplate(Player player, bool isSpectator)
    {
        if (!ReplaySession.IsReplayPlayback || player?.active != true)
            return true;

        return isSpectator ? SpectatorDrawSettings.IsDrawGhostsOn : SpectatorDrawSettings.IsDrawPlayersOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayPlayerDrawGate : ModPlayer
{
    public override void TransformDrawData(ref PlayerDrawSet drawInfo)
    {
        if (ReplayDrawGate.ShouldDrawPlayer(drawInfo.drawPlayer))
            return;

        drawInfo.DrawDataCache.Clear();
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayNPCDrawGate : GlobalNPC
{
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        return !ReplaySession.IsReplayPlayback || SpectatorDrawSettings.IsDrawNPCsOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayProjectileDrawGate : GlobalProjectile
{
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        return !ReplaySession.IsReplayPlayback || SpectatorDrawSettings.IsDrawProjectilesOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayItemDrawGate : GlobalItem
{
    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        return !ReplaySession.IsReplayPlayback || SpectatorDrawSettings.IsDrawItemsOn;
    }
}
