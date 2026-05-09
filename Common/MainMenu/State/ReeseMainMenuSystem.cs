using Reese.Core.Configs;
using Terraria.UI;

namespace Reese.Common.MainMenu.State;

[Autoload(Side = ModSide.Client)]
internal sealed class ReeseMainMenuSystem : ModSystem
{
    private UserInterface reeseMainMenuUI;
    private UIState reeseMainMenuState;

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;

        reeseMainMenuUI = new();
        reeseMainMenuState = new MainMenuReplayBrowserUIState();
        reeseMainMenuUI.SetState(reeseMainMenuState);

        On_Main.DrawVersionNumber += DrawMenuUI;
        On_Main.UpdateUIStates += UpdateUIStates;
    }

    private static bool ShouldShowOverlay()
    {
        return Main.gameMenu && Main.menuMode == 0 && ModContent.GetInstance<ClientConfig>().ShowInMainMenu;
    }

    private void DrawMenuUI(On_Main.orig_DrawVersionNumber orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        if (!ShouldShowOverlay() || reeseMainMenuUI?.CurrentState == null)
            return;

        var old = UserInterface.ActiveInstance;
        try
        {
            UserInterface.ActiveInstance = reeseMainMenuUI;
            reeseMainMenuUI.Draw(Main.spriteBatch, new GameTime());
        }
        finally
        {
            UserInterface.ActiveInstance = old;
        }
    }

    private void UpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        bool shouldShow = Main.gameMenu && Main.menuMode == 0 && ModContent.GetInstance<ClientConfig>().ShowInMainMenu;

        if (shouldShow)
        {
            if (reeseMainMenuUI.CurrentState == null)
                reeseMainMenuUI.SetState(reeseMainMenuState);

            var old = UserInterface.ActiveInstance;
            try
            {
                UserInterface.ActiveInstance = reeseMainMenuUI;
                reeseMainMenuUI.Update(gameTime);
            }
            finally
            {
                UserInterface.ActiveInstance = old;
            }
        }
        else if (reeseMainMenuUI?.CurrentState != null)
        {
            reeseMainMenuUI.SetState(null);
        }

        orig(gameTime);
    }

    public override void Unload()
    {
        On_Main.DrawVersionNumber -= DrawMenuUI;
        On_Main.UpdateUIStates -= UpdateUIStates;

        reeseMainMenuUI = null;
        reeseMainMenuState = null;
    }
}
