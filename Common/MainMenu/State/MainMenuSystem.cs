using Terraria.UI;

namespace Reese.Common.MainMenu.State;

[Autoload(Side = ModSide.Client)]
internal sealed class MainMenuSystem : ModSystem
{
    private UserInterface reeseMainMenuUI;
    private ReplaysBrowserUIState reeseMainMenuState;

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;

        reeseMainMenuUI = new UserInterface();
        reeseMainMenuState = new ReplaysBrowserUIState();
        reeseMainMenuUI.SetState(reeseMainMenuState);

        On_Main.DrawMenu += DrawMenu;
        On_Main.UpdateUIStates += UpdateUIStates;
    }

    private void DrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        if (Main.gameMenu && Main.menuMode == 0 && reeseMainMenuUI?.CurrentState != null)
            reeseMainMenuUI.Draw(Main.spriteBatch, gameTime);

        orig(self, gameTime);
    }

    private void UpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        if (Main.gameMenu && Main.menuMode == 0)
        {
            if (reeseMainMenuUI.CurrentState == null)
                reeseMainMenuUI.SetState(reeseMainMenuState);

            reeseMainMenuUI.Update(gameTime);
        }
        else if (reeseMainMenuUI.CurrentState != null)
        {
            reeseMainMenuUI.SetState(null);
        }

        orig(gameTime);
    }

    public override void Unload()
    {
        On_Main.DrawMenu -= DrawMenu;
        On_Main.UpdateUIStates -= UpdateUIStates;

        reeseMainMenuUI = null;
        reeseMainMenuState = null;
    }
}