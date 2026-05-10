using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace Reese.Common.ReplaySpectate.Hooks;

/// <summary>
/// Disable ghost draw if client config option says so.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class DisableGhostDrawSystem : ModSystem
{
    public override void Load()
    {
        On_LegacyPlayerRenderer.DrawGhost += OnDrawGhost;
    }

    public override void Unload()
    {
        On_LegacyPlayerRenderer.DrawGhost -= OnDrawGhost;
    }

    private void OnDrawGhost(On_LegacyPlayerRenderer.orig_DrawGhost orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position, float shadow)
    {
        if (!ShouldDrawGhost(drawPlayer))
            return;

        //orig(self, camera, drawPlayer, position, shadow);
        DrawGhost(camera, drawPlayer, position, shadow);
    }

    // Copied from LegacyPlayerRenderer.DrawGhost.
    private void DrawGhost(Camera camera, Player drawPlayer, Vector2 position, float shadow = 0f)
    {
        byte mouseTextColor = Main.mouseTextColor;
        SpriteEffects effects = ((drawPlayer.direction != 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        Color immuneAlpha = drawPlayer.GetImmuneAlpha(Lighting.GetColor((int)((double)drawPlayer.position.X + (double)drawPlayer.width * 0.5) / 16, (int)((double)drawPlayer.position.Y + (double)drawPlayer.height * 0.5) / 16, new Color(mouseTextColor / 2 + 100, mouseTextColor / 2 + 100, mouseTextColor / 2 + 100, mouseTextColor / 2 + 100)), shadow);
        immuneAlpha.A = (byte)((float)(int)immuneAlpha.A * (1f - Math.Max(0.5f, shadow - 0.5f)));
        Rectangle value = new Rectangle(0, TextureAssets.Ghost.Height() / 4 * drawPlayer.ghostFrame, TextureAssets.Ghost.Width(), TextureAssets.Ghost.Height() / 4);
        Vector2 origin = new Vector2((float)value.Width * 0.5f, (float)value.Height * 0.5f);
        camera.SpriteBatch.Draw(TextureAssets.Ghost.Value, new Vector2((int)(position.X - camera.UnscaledPosition.X + (float)(value.Width / 2)), (int)(position.Y - camera.UnscaledPosition.Y + (float)(value.Height / 2))), value, immuneAlpha, 0f, origin, 1f, effects, 0f);
    }

    public static bool ShouldDrawGhost(Player drawPlayer)
    {
        if (drawPlayer == null || !drawPlayer.active || !drawPlayer.ghost)
            return true;

        if (drawPlayer.whoAmI == Main.myPlayer)
            return true;

        Player local = Main.LocalPlayer;

        if (local?.active == true && local.ghost)
            return true;

        return true;
        //return ModContent.GetInstance<ClientConfig>().DrawSpectators;
    }
}

//internal sealed class SpectatorGhostDrawPlayer : ModPlayer
//{
//    internal static bool ShouldDrawGhost(Player drawPlayer)
//    {
//        if (drawPlayer == null || !drawPlayer.active || !drawPlayer.ghost)
//            return true;

//        if (drawPlayer.whoAmI == Main.myPlayer)
//            return true;

//        ClientConfig config = ModContent.GetInstance<ClientConfig>();
//        return config.DrawGhostsForOthers;
//    }

//    public override void HideDrawLayers(PlayerDrawSet drawInfo)
//    {
//        Player drawPlayer = drawInfo.drawPlayer;
//        if (ShouldDrawGhost(drawPlayer))
//            return;

//        foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.GetDrawLayers(drawInfo))
//            layer.Hide();
//    }
//}