using Reese.Common.Replayer.ReplayHud;
using Reese.Common.Spectator;
using Terraria.Graphics;

namespace Reese.Common.Replayer.Zoom;

/// <summary>
/// Adjusts camera zoom based on the ReplayZoom setting in ReplayClientSettings.
/// This allows users to zoom in and out while spectating.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class CameraSystem : ModSystem
{
    private float vanillaGameZoomTarget = 1f;
    private bool wasSpectating;

    public override void PostUpdateInput()
    {
        if (Main.gameMenu)
            return;

        if (!SpectatorMode.CanSpectate)
        {
            if (wasSpectating)
            {
                Main.GameZoomTarget = vanillaGameZoomTarget;
                wasSpectating = false;
            }

            return;
        }

        if (!wasSpectating)
        {
            vanillaGameZoomTarget = MathHelper.Clamp(Main.GameZoomTarget, 1f, 2f);
            wasSpectating = true;
        }

        Main.GameZoomTarget = ReplayClientSettings.ReplayZoom;
    }

    public override void ModifyTransformMatrix(ref SpriteViewMatrix transform)
    {
        if (Main.gameMenu || !SpectatorMode.CanSpectate)
            return;

        float zoom = ReplayClientSettings.ReplayZoom;
        transform.Zoom = new Vector2(zoom);
        Main.BackgroundViewMatrix.Zoom = zoom > 1f ? new Vector2(zoom) : Vector2.One;
    }
}
