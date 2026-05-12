using Reese.Common.MainMenu.UI;
using Reese.Core.Configs;
using System;
using System.Threading.Tasks;
using Terraria.UI;

namespace Reese.Common.MainMenu;

public static class MainMenuActions
{
    public static void OpenReplayBrowser(UserInterface ui, UserInterface reeseMainMenuUI)
    {
        var state = new MainMenuUIState(onBack: () => CloseReplayBrowser(ui, reeseMainMenuUI));
        ui?.SetState(state);
    }

    public static void OpenConfirmDelete(UserInterface ui, UserInterface reeseMainMenuUI, string targetName, Action onConfirm)
    {
        bool returnToBrowserState = ui?.CurrentState != null;
        bool handled = false;

        void Close(Action action)
        {
            if (handled)
                return;

            handled = true;
            Main.QueueMainThreadAction(() =>
            {
                action?.Invoke();
                if (returnToBrowserState)
                    OpenReplayBrowser(ui, reeseMainMenuUI);
                else
                    CloseReplayBrowser(ui, reeseMainMenuUI);
            });
        }

        ConfirmDeleteState state = new()
        {
            TargetName = targetName,
            OnYes = () => Close(onConfirm),
            OnNo = () => Close(null)
        };

        ui.SetState(state);
    }

    public static void OpenRename(UserInterface ui, UserInterface reeseMainMenuUI, string currentName, Action<string> onSubmit)
    {
        bool returnToBrowserState = ui?.CurrentState != null;
        bool handled = false;

        void Close(Action action)
        {
            if (handled)
                return;

            handled = true;
            Main.QueueMainThreadAction(() =>
            {
            action?.Invoke();

            if (returnToBrowserState)
                OpenReplayBrowser(ui, reeseMainMenuUI);
            else
                CloseReplayBrowser(ui, reeseMainMenuUI);
            });
        }

        Main.clrInput();
        ui.SetState(new ConfirmRenameState(currentName, name => Close(() => onSubmit?.Invoke(name)), () => Close(null)));
    }

    public static void OpenClientConfig(UserInterface ui, UserInterface reeseMainMenuUI)
    {
        Main.QueueMainThreadAction(() =>
        {
            CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: true);
            ModContent.GetInstance<ClientConfig>().Open(() =>
            {
                CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: true);
                Main.menuMode = 0;
            });
        });
    }

    public static void CloseForReplayLaunch(UserInterface ui, UserInterface reeseMainMenuUI)
    {
        CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: false);
    }

    public static void CloseReplayBrowser(UserInterface ui, UserInterface reeseMainMenuUI)
    {
        CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: true);
    }

    public static void CloseAllMenuUI(UserInterface ui, UserInterface reeseMainMenuUI, bool resetMenuMode)
    {
        ui?.SetState(null);
        reeseMainMenuUI?.SetState(null);

        if (resetMenuMode)
            Main.menuMode = 0;

        Main.blockMouse = false;
    }
}
