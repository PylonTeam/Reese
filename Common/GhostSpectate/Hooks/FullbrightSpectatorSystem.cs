using Reese.Common.GhostSpectate.SpectatorMode;
using Terraria.Graphics.Light;

namespace Reese.Common.GhostSpectate.Hooks;

/// <summary>
/// Fullbright tool for spectators
/// </summary>
internal class FullbrightSpectatorSystem : ModSystem
{
    public static int strength = 1;
    public static bool Enabled
    {
        get => strength > 0;
        set => strength = value ? 1 : 0;
    }

    public override void Load()
    {
        On_TileLightScanner.GetTileLight += HackLight;
    }

    private void HackLight(On_TileLightScanner.orig_GetTileLight orig, TileLightScanner self, int x, int y, out Vector3 outputColor)
    {
        orig(self, x, y, out outputColor);

        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
            return;

        if (strength > 0)
            outputColor += Vector3.One * strength;
    }
}
