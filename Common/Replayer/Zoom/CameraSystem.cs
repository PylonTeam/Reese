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

    internal static float WorldZoom => MathHelper.Max(1f, Main.ForcedMinimumZoom) * ReplayClientSettings.ReplayZoom;

    public override void PostUpdateInput()
    {
        if (Main.gameMenu || !SpectatorMode.CanSpectate)
        {
            RestoreZoom();
            return;
        }

        BeginSpectating();
        Main.GameZoomTarget = ReplayClientSettings.ReplayZoom;
    }

    private void BeginSpectating()
    {
        if (!wasSpectating)
        {
            vanillaGameZoomTarget = MathHelper.Clamp(Main.GameZoomTarget, 1f, 2f);
            // Start each session at the player's current zoom, including on re-entry.
            ReplayClientSettings.ImportReplayZoomFromGame(vanillaGameZoomTarget);
            wasSpectating = true;
        }
    }

    public override void ModifyTransformMatrix(ref SpriteViewMatrix transform)
    {
        if (Main.gameMenu || !SpectatorMode.CanSpectate)
            return;

        BeginSpectating();
        // Terraria's resolution scale is part of normal zoom (2x at 3840x2160).
        // Replacing it with ReplayZoom alone abruptly doubles the view at 4K.
        float zoom = WorldZoom;
        transform.Zoom = new Vector2(zoom);
        Main.BackgroundViewMatrix.Zoom = zoom > 1f ? new Vector2(zoom) : Vector2.One;
    }

    public override void OnWorldUnload() => RestoreZoom();

    public override void Unload() => RestoreZoom();

    private void RestoreZoom()
    {
        if (!wasSpectating)
            return;

        Main.GameZoomTarget = vanillaGameZoomTarget;
        wasSpectating = false;
    }
}
