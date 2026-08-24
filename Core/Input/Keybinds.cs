using Microsoft.Xna.Framework.Input;
using Reese.Common.Replay.ReplayHud;
using Reese.Common.Replay.ReplayHud.Ghost;
using Reese.Common.Spectator;
using Reese.Content;
using Terraria.GameInput;

namespace Reese.Core.Input;

[Autoload(Side = ModSide.Client)]
public sealed class Keybinds : ModSystem
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

public sealed class KeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Main.drawingPlayerChat)
            return;

        Keybinds keybinds = ModContent.GetInstance<Keybinds>();

        if (keybinds.ReplayUI?.JustPressed != true)
            return;

        if (SpectatorMode.CanUseReplayHud)
        {
            ModContent.GetInstance<ReplayHudSystem>().ToggleReplayHud();
            return;
        }

        if (SpectatorMode.CanUseGhostHud)
        {
            ModContent.GetInstance<GhostHudSystem>().OpenFullHud();
            return;
        }

        Main.NewText($"[i:{ModContent.ItemType<CameraItem>()}] You are not in a replay or ghost mode, spectate UI cannot be shown!", Color.Yellow);
    }
}
