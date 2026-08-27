using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replay.Hud.ReplaySpectate;
using Reese.Common.Spectator;
using Reese.Core.Utilities;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Map;
using Terraria.UI;

namespace Reese.Common.Replay.GhostHooks;

/// <summary>
/// Draws all ghosts on the map with a custom icon instead of the default player head.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class GhostDrawMapHeads : ModSystem
{
    public override void Load()
    {
        On_MapHeadRenderer.DrawPlayerHead += HideGhostPlayersVanillaHeads;
    }

    public override void Unload()
    {
        On_MapHeadRenderer.DrawPlayerHead -= HideGhostPlayersVanillaHeads;
    }

    private static void HideGhostPlayersVanillaHeads(On_MapHeadRenderer.orig_DrawPlayerHead orig, MapHeadRenderer self, Camera camera, Player drawPlayer, Vector2 position, float alpha, float scale, Color borderColor)
    {
        // Only take the vanilla head away when GhostMapHeadLayer is actually going to replace it,
        // otherwise ghosts vanish from the map entirely for anyone not spectating.
        if (drawPlayer?.active == true && drawPlayer.ghost && GhostMapHeadLayer.WillDrawGhostIcons)
            return;

        orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
    }
}

internal sealed class GhostMapHeadLayer : ModMapLayer
{
    /// <summary>
    /// Whether this layer draws ghost icons. <see cref="GhostDrawMapHeads"/> reads the same
    /// property so the two cannot drift apart and leave ghosts undrawn by both.
    /// </summary>
    internal static bool WillDrawGhostIcons =>
        SpectatorMode.CanSpectate || SpectatorMode.CanDrawOtherGhosts || Main.LocalPlayer?.ghost == true;

    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!WillDrawGhostIcons)
            return;

        Texture2D ghostRight = Ass.GhostRight.Value;
        Texture2D ghostLeft = Ass.GhostLeft.Value;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player?.active != true || !player.ghost || !ReplayDrawGate.ShouldDrawGhost(player))
                continue;

            Texture2D texture = player.direction == -1 ? ghostLeft : ghostRight;

            MapOverlayDrawContext.DrawResult result = context.Draw(
                texture,
                player.Center / 16f,
                Color.White,
                new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.6f,
                scaleIfSelected: 2.2f,
                Alignment.Center);

            if (result.IsMouseOver)
                text = $"{player.name} (spectator)";
        }
    }
}
