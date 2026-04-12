using Microsoft.Xna.Framework.Input;
using Reese.Common.ReplayTool;
using Reese.Common.TimeScaleTool;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace Reese.Core;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind TimescaleUI { get; private set; }
    public ModKeybind ReplayUI { get; private set; }

    public override void Load()
    {
        TimescaleUI = KeybindLoader.RegisterKeybind(Mod, "TimescaleUI", Keys.NumPad6);
        ReplayUI = KeybindLoader.RegisterKeybind(Mod, "ReplayUI", Keys.NumPad7);
    }

    public override void Unload()
    {
        TimescaleUI = null;
        ReplayUI = null;
    }
}

internal class KeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        Keybinds keybinds = ModContent.GetInstance<Keybinds>();

        if (keybinds.TimescaleUI?.JustPressed == true)
            ModContent.GetInstance<TimeScalePanelSystem>().ToggleActive();

        if (keybinds.ReplayUI?.JustPressed == true)
            ModContent.GetInstance<ReplayToolPanelSystem>().ToggleActive();
    }
}
