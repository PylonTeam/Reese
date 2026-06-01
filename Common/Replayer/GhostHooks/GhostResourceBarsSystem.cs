using Reese.Common.Spectator;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Hides resource bars for ghosts
/// Vanilla: Resource Bars 	
/// https://github.com/tModLoader/tModLoader/wiki/Vanilla-Interface-layers-values
/// </summary>
internal class GhostResourceBarsSystem : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!SpectatorMode.IsLocalGhost)
            return;

        GameInterfaceLayer layer = layers.Find(static layer => layer.Name == "Vanilla: Resource Bars");

        if (layer is not null)
            layer.Active = false;
    }
}
