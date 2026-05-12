using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal static class ReplayDrawGate
{
    public static bool ShouldDrawPlayer(Player player)
    {
        if (!ReplayPlayback.IsReplayPlayback || player?.active != true)
            return true;

        return player.ghost ? ReplayClientSettings.IsDrawGhostsOn : ReplayClientSettings.IsDrawPlayersOn;
    }

    public static bool ShouldDrawGhost(Player player)
    {
        return !ReplayPlayback.IsReplayPlayback || player?.ghost != true || ReplayClientSettings.IsDrawGhostsOn;
    }

    public static bool ShouldDrawNameplate(Player player, bool isSpectator)
    {
        if (!ReplayPlayback.IsReplayPlayback || player?.active != true)
            return true;

        return ReplayClientSettings.IsNameplatesOn;
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
        return !ReplayPlayback.IsReplayPlayback || ReplayClientSettings.IsDrawNPCsOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayProjectileDrawGate : GlobalProjectile
{
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        return !ReplayPlayback.IsReplayPlayback || ReplayClientSettings.IsDrawProjectilesOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayItemDrawGate : GlobalItem
{
    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        return !ReplayPlayback.IsReplayPlayback || ReplayClientSettings.IsDrawItemsOn;
    }
}
