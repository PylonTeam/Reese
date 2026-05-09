using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.GhostSpectate.Hooks;

/// <summary>
/// Hides resource bars for ghosts
/// Vanilla: Resource Bars 	
/// https://github.com/tModLoader/tModLoader/wiki/Vanilla-Interface-layers-values
/// </summary>
internal class DisableGhostResourceBarsSystem : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.LocalPlayer?.ghost != true)
            return;

        GameInterfaceLayer layer = layers.Find(static layer => layer.Name == "Vanilla: Resource Bars");

        if (layer is not null)
            layer.Active = false;
    }
}
