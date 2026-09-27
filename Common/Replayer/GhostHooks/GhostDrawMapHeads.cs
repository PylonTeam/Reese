using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Spectator;
using Reese.Core.Utilities;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Map;
using Terraria.UI;

namespace Reese.Common.Replayer.GhostHooks;

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
        // This layer owns ghost icons when available, including hiding them with Draw Heads.
        // Otherwise keep vanilla heads for players who cannot see the custom ghost icons.
        if (drawPlayer?.active == true && drawPlayer.ghost && GhostMapHeadLayer.WillDrawGhostIcons)
            return;

        orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
    }
}

internal sealed class GhostMapHeadLayer : ModMapLayer
{
    /// <summary>
    /// Whether this layer owns ghost icons. Draw Heads can still hide those icons;
    /// <see cref="GhostDrawMapHeads"/> suppresses vanilla heads either way.
    /// </summary>
    internal static bool WillDrawGhostIcons =>
        SpectatorMode.CanSpectate || SpectatorMode.CanDrawOtherGhosts || Main.LocalPlayer?.ghost == true;

    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!WillDrawGhostIcons || !DrawAllPlayerHeadsOnMapSystem.ShouldDrawHeads)
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
