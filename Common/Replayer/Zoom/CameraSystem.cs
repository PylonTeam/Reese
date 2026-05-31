using Reese.Common.Replayer.ReplayHud;
using Terraria.Graphics;

namespace Reese.Common.Replayer.Zoom;

/// <summary>
/// Adjusts camera zoom based on the ReplayZoom setting in ReplayClientSettings.
/// This allows users to zoom in and out while viewing replays.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class CameraSystem : ModSystem
{
    private float vanillaGameZoomTarget = 1f;
    private bool wasReplayPlayback;

    public override void PostUpdateInput()
    {
        if (Main.gameMenu)
            return;

        if (!ReplayPlayback.IsReplayPlayback)
        {
            if (wasReplayPlayback)
            {
                Main.GameZoomTarget = vanillaGameZoomTarget;
                wasReplayPlayback = false;
            }

            return;
        }

        if (!wasReplayPlayback)
        {
            vanillaGameZoomTarget = MathHelper.Clamp(Main.GameZoomTarget, 1f, 2f);
            wasReplayPlayback = true;
        }

        Main.GameZoomTarget = ReplayClientSettings.ReplayZoom;
    }

    public override void ModifyTransformMatrix(ref SpriteViewMatrix transform)
    {
        if (Main.gameMenu || !ReplayPlayback.IsReplayPlayback)
            return;

        float zoom = ReplayClientSettings.ReplayZoom;
        transform.Zoom = new Vector2(zoom);
        Main.BackgroundViewMatrix.Zoom = zoom > 1f ? new Vector2(zoom) : Vector2.One;
    }
}
