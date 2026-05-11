using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Common.Replayer.ReplayHud.Shared.Settings;

internal static class SpectatorClientSettings
{
    public static bool RightClickTeleport { get; set; } = true;
    public static void ToggleRightClickTeleport() => RightClickTeleport = !RightClickTeleport;
}
