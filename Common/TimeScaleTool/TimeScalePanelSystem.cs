using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.TimeScaleTool;

/// <summary>
/// The system responsible for managing the <see cref="TimeScalePanel"/> UI.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class TimeScalePanelSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState timeScalePanelUIState;

    // State
    public bool IsActive() => ui?.CurrentState != null;

    public void ToggleActive()
    {
        if (IsActive())
        {
            ui.SetState(null);
            timeScalePanelUIState = null;
            return;
        }

        RebuildPanel();
    }

    public override void OnWorldLoad()
    {
        ui = new();
        timeScalePanelUIState = null;
    }
    public override void OnWorldUnload()
    {
        ui?.SetState(null);
        timeScalePanelUIState = null;
    }

    private void RebuildPanel()
    {
        timeScalePanelUIState = new UIState();
        timeScalePanelUIState.Append(new TimeScalePanel());
        ui.SetState(timeScalePanelUIState);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        base.UpdateUI(gameTime);
        ui?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index != -1)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer(
                name: "ErkySSC: TimeScalePanelSystem",
                drawMethod: () =>
                {
                    if (IsActive())
                    {
                        ui?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);

                        return true;
                    }
                    return true;
                },
                scaleType: InterfaceScaleType.UI
            ));
        }
    }
}
