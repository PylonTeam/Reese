using Reese.Common.Spectator;
using Reese.Common.Replayer.ReplayHud;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Applies the spectator's Draw Heads setting to player heads on the map.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class DrawAllPlayerHeadsOnMapSystem : ModSystem
{
    private static bool drawingWorldMap;

    internal static bool ShouldDrawHeads => !SpectatorMode.CanSpectate || ReplayClientSettings.IsDrawHeadsOn;

    public override void Load()
    {
        On_Main.DrawMap += DrawMapOverride;
        On_MapHeadRenderer.DrawPlayerHead += DrawPlayerHead;
    }

    public override void Unload()
    {
        On_Main.DrawMap -= DrawMapOverride;
        On_MapHeadRenderer.DrawPlayerHead -= DrawPlayerHead;
        drawingWorldMap = false;
    }

    private static void DrawPlayerHead(On_MapHeadRenderer.orig_DrawPlayerHead orig, MapHeadRenderer self, Camera camera, Player drawPlayer, Vector2 position, float alpha, float scale, Color borderColor)
    {
        // The same renderer draws HUD portraits and nameplate icons outside Main.DrawMap.
        if (drawingWorldMap && !ShouldDrawHeads)
            return;

        orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
    }

    /// <summary>
    /// Thanks PvPFrameworkMini and EJ
    /// </summary>
    private static void DrawMapOverride(On_Main.orig_DrawMap orig, Main self, GameTime gameTime)
    {
        if (!SpectatorMode.CanSpectate)
        {
            orig(self, gameTime);
            return;
        }

        bool[] hostile = new bool[Main.maxPlayers];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player?.active != true)
                continue;

            hostile[i] = player.hostile;
            player.hostile = false;
        }

        bool wasDrawingWorldMap = drawingWorldMap;
        drawingWorldMap = true;
        try
        {
            orig(self, gameTime);
        }
        finally
        {
            drawingWorldMap = wasDrawingWorldMap;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (player?.active == true)
                    player.hostile = hostile[i];
            }
        }
    }
}
