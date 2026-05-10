using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplaySpectate.UI;
using Terraria.GameInput;

namespace Reese.Core.Input;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind ReplayUI { get; private set; }

    public override void Load()
    {
        ReplayUI = KeybindLoader.RegisterKeybind(Mod, "ReplayUI", Keys.NumPad7);
    }

    public override void Unload()
    {
        ReplayUI = null;
    }
}

internal class KeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        Keybinds keybinds = ModContent.GetInstance<Keybinds>();

        if (keybinds.ReplayUI?.JustPressed == true)
            ModContent.GetInstance<ReplayUISystem>().ToggleAllReplayUI();
    }
}
