using Reese.Common.Replayer.ReplaySpectate.SpectatorMode;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplaySpectate.UI;

[Autoload(Side = ModSide.Client)]
public class ReplayUISystem : ModSystem
{
    private UserInterface spectatorInterface;
    private ReplayUIState spectatorState;

    public override void OnWorldLoad()
    {
        spectatorInterface = new UserInterface();
        spectatorState = new ReplayUIState();
        spectatorInterface.SetState(spectatorState);
    }

    public void RebuildUI()
    {
        spectatorState?.Rebuild();
    }

    public void ToggleSpectatorInfoPanel()
    {
        spectatorState?.ToggleInfoPanel();
    }

    public void OnLocalModeAccepted(SpectateMode mode)
    {
        spectatorState?.OnLocalModeAccepted(mode);
    }

    public bool IsMouseOverSpectatorUI()
    {
        return spectatorState?.IsMouseOverVisiblePanel() == true;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        spectatorInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Death Text");

        if (ConfigHelper.IsAnyConfigUIOpen())
            index = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (index == -1)
            return;

        if (spectatorInterface?.CurrentState != null)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer(
                "Reese: Spectator UI",
                () =>
                {
                    spectatorInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
