using Microsoft.Xna.Framework.Input;
using Reese.Common.TimeScaleTool;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace Reese.Core;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind TimescaleUI { get; private set; }

    public override void Load()
    {
        TimescaleUI = KeybindLoader.RegisterKeybind(Mod, "TimescaleUI", Keys.NumPad6);
    }

    public override void Unload()
    {
        TimescaleUI = null;
    }
}

internal class KeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        Keybinds keybinds = ModContent.GetInstance<Keybinds>();

        if (keybinds.TimescaleUI?.JustPressed == true)
            ModContent.GetInstance<TimeScalePanelSystem>().ToggleActive();
    }
}