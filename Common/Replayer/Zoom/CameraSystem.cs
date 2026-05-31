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
    public override void PostUpdateInput()
    {
        if (!Main.gameMenu)
            Main.GameZoomTarget = ReplayClientSettings.ReplayZoom;
    }

    public override void ModifyTransformMatrix(ref SpriteViewMatrix transform)
    {
        if (Main.gameMenu)
            return;

        float zoom = ReplayClientSettings.ReplayZoom;
        transform.Zoom = new Vector2(zoom);
        Main.BackgroundViewMatrix.Zoom = zoom > 1f ? new Vector2(zoom) : Vector2.One;
    }
}