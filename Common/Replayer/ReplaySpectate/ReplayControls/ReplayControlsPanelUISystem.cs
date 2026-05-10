using Microsoft.Xna.Framework;
using Reese.Common.Replayer;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplaySpectate.ReplayControls;

[Autoload(Side = ModSide.Client)]
public sealed class ReplayControlsPanelUISystem : ModSystem
{
    private UserInterface ui;
    private UIState replayToolPanelUIState;
    private ReplayControlsPanel replayToolPanel;

    public bool IsActive() => ui?.CurrentState != null;

    public void ToggleActive()
    {
        if (IsActive())
        {
            ui.SetState(null);
            replayToolPanelUIState = null;
            replayToolPanel = null;
            return;
        }

        if (!ReplaySession.IsReplayPlayback)
            return;

        RebuildPanel();
    }

    public void Open()
    {
        if (IsActive() || !ReplaySession.IsReplayPlayback)
            return;

        RebuildPanel();
    }

    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        replayToolPanelUIState = null;
        replayToolPanel = null;
    }

    public override void OnWorldUnload()
    {
        ui?.SetState(null);
        replayToolPanelUIState = null;
        replayToolPanel = null;
    }

    private void RebuildPanel()
    {
        replayToolPanelUIState = new UIState();
        replayToolPanel = new ReplayControlsPanel();
        replayToolPanelUIState.Append(replayToolPanel);
        ui.SetState(replayToolPanelUIState);
    }

    public bool IsMouseOverPanel()
    {
        return IsActive() && replayToolPanel?.ContainsPoint(Main.MouseScreen) == true;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        base.UpdateUI(gameTime);
        ui?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            name: "Reese: ReplayToolPanelSystem",
            drawMethod: () =>
            {
                if (IsActive())
                    ui?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);

                return true;
            },
            scaleType: InterfaceScaleType.UI
        ));
    }
}
