using Microsoft.Xna.Framework.Input;
using Reese.Core.Configs;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Core.Debug;

#if DEBUG
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
        // 1. Check if the config allows the drawer to run
        if (!ModContent.GetInstance<ClientConfig>().ShowDebugDrawer)
            return;

        int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

        if (index < 0)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "Reese: Debug UI",
            () =>
            {
                debugInterface?.Draw(Main.spriteBatch, new GameTime());

                DebugDrawer.DrawButtons();
                DebugDrawer.DrawDebugInfo();
                DebugDrawer.Flush(Main.spriteBatch);

                return true;
            },
            InterfaceScaleType.UI));
    }
}
#endif