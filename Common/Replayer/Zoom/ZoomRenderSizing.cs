using System;

namespace Reese.Common.Replayer.Zoom;

internal static class ZoomRenderSizing
{
    private const int RangeStep = 256;

    internal static int GetExtraOffscreenRange(int width, int height, float worldZoom)
    {
        if (!float.IsFinite(worldZoom) || worldZoom <= 0f || worldZoom >= 1f)
            return 0;

        // Offscreen range is shared by both axes. Round up in steps so dragging
        // the zoom slider does not rebuild all world targets for every pixel.
        double extra = Math.Max(width, height) * (1d / worldZoom - 1d) * 0.5d;
        return (int)Math.Ceiling(extra / RangeStep) * RangeStep;
    }
}
