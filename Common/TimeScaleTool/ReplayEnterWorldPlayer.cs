using Reese.Common.TimeScaleTool;
using Terraria;
using Terraria.ModLoader;

namespace Reese.Common;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        // TODO check if this is actually a replay and not just a normal world load
        Log.Chat("Replay started, maybe");

        // Open time scale panel
        ModContent.GetInstance<TimeScalePanelSystem>().ToggleActive();
    }
}