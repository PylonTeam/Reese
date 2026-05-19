using Microsoft.Xna.Framework.Input;
using Reese.Core.Configs;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Core.Debug;

//#if DEBUG
internal sealed class DebugDrawerSystem : ModSystem
{
    private UserInterface debugInterface;
    private UIState debugState;

    public override void OnWorldLoad()
    {
        debugInterface = new UserInterface();
        debugState = new UIState();

        debugState.Activate();

        debugInterface.SetState(debugState);
    }

    public override void OnWorldUnload()
    {
        debugState = null;
        debugInterface = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        bool showDebugDrawer = ModContent.GetInstance<ClientConfig>().ShowDebugDrawer;

        if (KeyboardHelper.Pressed(Keys.F6))
            DebugDrawer.ToggleDebugButtons(showDebugDrawer);

#if DEBUG
        if (KeyboardHelper.Pressed(Keys.NumPad0))
        {
            if (Main.LocalPlayer.ghost)
                Main.LocalPlayer.ghost = false;
            else
                Main.LocalPlayer.ghost = true;
        }
#endif

        debugInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        bool showDebugDrawer = ModContent.GetInstance<ClientConfig>().ShowDebugDrawer;
        bool showDebugButtons = DebugDrawer.AreDebugButtonsVisible(showDebugDrawer);

        if (!showDebugDrawer && !showDebugButtons)
            return;

        int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

        if (index < 0)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "Reese: Debug UI",
            () =>
            {
                debugInterface?.Draw(Main.spriteBatch, new GameTime());

                if (showDebugButtons)
                    DebugDrawer.DrawButtons();

                if (showDebugDrawer)
                    DebugDrawer.DrawDebugInfo();

                DebugDrawer.Flush(Main.spriteBatch);

                return true;
            },
            InterfaceScaleType.UI));
    }
}
//#endif
