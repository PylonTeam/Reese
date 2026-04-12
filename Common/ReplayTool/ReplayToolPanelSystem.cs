using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.ReplayTool;

[Autoload(Side = ModSide.Client)]
public sealed class ReplayToolPanelSystem : ModSystem
{
    private UserInterface ui;
    private UIState replayToolPanelUIState;

    public bool IsActive() => ui?.CurrentState != null;

    public void ToggleActive()
    {
        if (IsActive())
        {
            ui.SetState(null);
            replayToolPanelUIState = null;
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
    }

    public override void OnWorldUnload()
    {
        ui?.SetState(null);
        replayToolPanelUIState = null;
    }

    private void RebuildPanel()
    {
        replayToolPanelUIState = new UIState();
        replayToolPanelUIState.Append(new ReplayToolPanel());
        ui.SetState(replayToolPanelUIState);
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
