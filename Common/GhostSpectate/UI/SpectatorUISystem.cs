using Reese.Common.GhostSpectate.SpectatorMode;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.GhostSpectate.UI;

[Autoload(Side = ModSide.Client)]
public class SpectatorUISystem : ModSystem
{
    private UserInterface spectatorInterface;
    private SpectatorUIState spectatorState;

    public override void OnWorldLoad()
    {
        spectatorInterface = new UserInterface();
        spectatorState = new SpectatorUIState();
        spectatorInterface.SetState(spectatorState);
    }

    public void RebuildUI()
    {
        spectatorState?.Rebuild();
    }

    public void OpenSpectatorJoinPanel()
    {
        spectatorState?.OpenJoinPanel();
    }

    public void CloseSpectatorJoinPanel()
    {
        spectatorState?.CloseJoinPanel();
    }

    public void ToggleSpectatorInfoPanel()
    {
        spectatorState?.ToggleInfoPanel();
    }

    public void OnLocalModeAccepted(SpectateMode mode)
    {
        spectatorState?.OnLocalModeAccepted(mode);
    }

    public void EnsureSpectatorHUDStaysOpen()
    {
        spectatorState?.EnsureSpectatorHUDStaysOpen();
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
                "GhostSpectating: Spectator UI",
                () =>
                {
                    spectatorInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
