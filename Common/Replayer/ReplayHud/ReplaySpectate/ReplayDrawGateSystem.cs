using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer;
using Reese.Common.Spectator;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal static class ReplayDrawGate
{
    public static bool ShouldDrawPlayer(Player player)
    {
        if (player?.active != true)
            return true;

        if (player.ghost)
            return ShouldDrawGhost(player);

        return !SpectatorMode.CanSpectate || ReplayClientSettings.IsDrawPlayersOn;
    }

    public static bool ShouldDrawGhost(Player player)
    {
        if (player?.active != true || !player.ghost)
            return true;

        if (ReplayPlayback.IsReplayPlayback)
            return ReplayClientSettings.IsDrawGhostsOn;

        return SpectatorMode.ShouldDrawGhost(player);
    }

    public static bool ShouldDrawNameplate(Player player, bool isSpectator)
    {
        if (player?.active != true)
            return true;

        if (player.ghost)
        {
            if (ReplayPlayback.IsReplayPlayback)
                return ReplayClientSettings.IsNameplatesOn;

            return SpectatorMode.ShouldDrawGhostNameplate(player);
        }

        return !SpectatorMode.CanSpectate || ReplayClientSettings.IsNameplatesOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayChatDrawGateSystem : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!SpectatorMode.CanSpectate || ReplayClientSettings.IsDrawChatOn)
            return;

        GameInterfaceLayer layer = layers.Find(static layer => layer.Name == "Vanilla: Player Chat");

        if (layer is not null)
            layer.Active = false;
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
        return !SpectatorMode.CanSpectate || ReplayClientSettings.IsDrawNPCsOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayProjectileDrawGate : GlobalProjectile
{
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        return !SpectatorMode.CanSpectate || ReplayClientSettings.IsDrawProjectilesOn;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayItemDrawGate : GlobalItem
{
    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        return !SpectatorMode.CanSpectate || ReplayClientSettings.IsDrawItemsOn;
    }
}
