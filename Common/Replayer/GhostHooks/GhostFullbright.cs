using Reese.Common.Spectator;
using System;
using Terraria.Graphics.Light;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Fullbright tool for spectators
/// </summary>
[Autoload(Side =ModSide.Client)]
internal class GhostFullbright : ModSystem
{
    public static int strength = 1;
    public static bool Enabled
    {
        get => strength > 0;
        set => strength = value ? 1 : 0;
    }

    /// <summary>
    /// <see cref="HackLight"/> runs once per tile of the lighting pass, so the spectator check is
    /// resolved once per tick here instead of walking the config chain tens of thousands of times.
    /// </summary>
    private static bool active;

    public override void PostUpdateEverything() => active = Enabled && SpectatorMode.CanSpectate;

    public override void OnWorldUnload() => active = false;

    public override void Load()
    {
        On_TileLightScanner.GetTileLight += HackLight;
    }

    public override void Unload()
    {
        On_TileLightScanner.GetTileLight -= HackLight;
    }

    private void HackLight(
    On_TileLightScanner.orig_GetTileLight orig,
    TileLightScanner self, int x, int y,
    out Vector3 outputColor)
    {
        orig(self, x, y, out outputColor);

        if (!active)
            return;

        // Clamp to 1 from below so dark tiles become fully lit,
        // but don't let naturally brighter areas get blown out.
        outputColor.X = Math.Max(outputColor.X, strength);
        outputColor.Y = Math.Max(outputColor.Y, strength);
        outputColor.Z = Math.Max(outputColor.Z, strength);
    }
}
