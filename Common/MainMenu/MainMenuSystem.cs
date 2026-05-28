using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using Reese.Common.Replayer;
using Reese.Core.Configs;
using System;
using System.Threading;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using static Reese.Common.MainMenu.MainMenuActions;

namespace Reese.Common.MainMenu;

/// <summary>
/// Adds a custom Reese Replays button to the main menu buttons (the button is added inbetween workshop and settings)
/// Adds a <see cref="MainMenuUIState"/> with the <see cref="ReplayBrowserPanel"/>
/// </summary>
[Autoload(Side = ModSide.Client)]
public class MainMenuSystem : ModSystem
{
    private const int SharedMenuMode = 888;
    private const string ButtonLabel = "Reese";


    public UserInterface ui;
    private UserInterface reeseMainMenuUI;
    private UIState reeseMainMenuState;
    private int reeseButtonIndex = -1;

    public static bool IsEnabled => true;

    public override void Load()
    {
        if (!IsEnabled)
            return;

        ui = new UserInterface();

        IL_Main.DrawMenu += InjectMatchmakingButton;

        On_Main.DrawVersionNumber += DrawMenuUI;
        On_Main.UpdateUIStates += PostUpdateUIStates;
    }

    public override void PostSetupContent()
    {
        if (!IsEnabled || Main.dedServ)
            return;

        reeseMainMenuUI = new();
        reeseMainMenuState = new MainMenuReplayBrowserUIState();
    }

    public override void Unload()
    {
        if (!IsEnabled)
            return;

        IL_Main.DrawMenu -= InjectMatchmakingButton;
        On_Main.DrawVersionNumber -= DrawMenuUI;
        On_Main.UpdateUIStates -= PostUpdateUIStates;

        ui = null;
        reeseMainMenuUI = null;
        reeseMainMenuState = null;
        reeseButtonIndex = -1;
    }

    #region Add menu button
    private void InjectMatchmakingButton(ILContext il)
    {
        IL.Edit(il, c =>
        {
            int buttonNamesIndex = -1;
            int buttonScalesIndex = -1;
            int offYIndex = -1;
            int spacingIndex = -1;
            int buttonIndexIndex = -1;
            int numButtonsIndex = -1;

            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Main>("selectedMenu"),
                    i => i.MatchLdloc(out buttonNamesIndex),
                    i => i.MatchLdloc(out buttonScalesIndex),
                    i => i.MatchLdloca(out offYIndex),
                    i => i.MatchLdloca(out spacingIndex),
                    i => i.MatchLdloca(out buttonIndexIndex),
                    i => i.MatchLdloca(out numButtonsIndex),
                    i => i.MatchCall(out var m) && m.DeclaringType.FullName == "Terraria.ModLoader.UI.Interface" && m.Name == "AddMenuButtons"))
            {
                Log.Warn("Failed to find Main.DrawMenu AddMenuButtons call.");
                return;
            }

            c.EmitLdloc(buttonNamesIndex);
            c.EmitLdloc(buttonScalesIndex);
            c.EmitLdloca(buttonIndexIndex);
            c.EmitLdloca(numButtonsIndex);

            c.EmitDelegate((string[] buttonNames, float[] buttonScales, ref int buttonIndex, ref int numButtons) =>
            {
                AddReeseMenuButton(buttonNames, buttonScales, ref buttonIndex, ref numButtons);
            });

            c.Index = 0;
            PatchButtonColor(c, buttonNamesIndex);

            c.Index = 0;

            int patchedSelectedMenuWrites = 0;

            while (c.TryGotoNext(MoveType.Before, i => i.MatchStfld<Main>("selectedMenu")))
            {
                c.EmitDelegate((int selectedMenu) => HandleSelectedMenuWrite(selectedMenu));
                patchedSelectedMenuWrites++;
                c.Index++;
            }

            if (patchedSelectedMenuWrites == 0)
                Log.Warn("Failed to patch Main.DrawMenu selectedMenu write.");
        });
    }

    private void AddReeseMenuButton(string[] buttonNames, float[] buttonScales, ref int buttonIndex, ref int numButtons)
    {
        reeseButtonIndex = -1;

        if (!ModContent.GetInstance<ClientConfig>().AddExtraMenuState)
            return;

        if (buttonNames == null || buttonScales == null)
            return;

        if ((uint)buttonIndex >= (uint)buttonNames.Length || (uint)buttonIndex >= (uint)buttonScales.Length)
        {
            Log.Warn("Could not add main menu button because the menu arrays are full.");
            return;
        }

        reeseButtonIndex = buttonIndex;
        buttonNames[buttonIndex] = ButtonLabel;

        if (buttonScales[buttonIndex] <= 0f)
            buttonScales[buttonIndex] = 1f;

        buttonIndex++;
        numButtons++;
    }

    private int HandleSelectedMenuWrite(int selectedMenu)
    {
        if (!Main.gameMenu || Main.menuMode != 0)
            return selectedMenu;

        if (!ModContent.GetInstance<ClientConfig>().AddExtraMenuState)
            return selectedMenu;

        if (reeseButtonIndex < 0 || selectedMenu != reeseButtonIndex)
            return selectedMenu;

        Main.mouseLeftRelease = false;
        Main.blockMouse = true;
        Main.menuMode = SharedMenuMode;

        SoundEngine.PlaySound(SoundID.MenuOpen);
        MainMenuActions.OpenReplayBrowser(ui, reeseMainMenuUI);

        return -1;
    }

    private static void PatchButtonColor(ILCursor c, int buttonNamesIndex)
    {
        int colorIndex = -1;
        int rIndex = -1;
        int gIndex = -1;
        int bIndex = -1;
        int aIndex = -1;
        int hoveredIndex = -1;
        int outerIteratorIndex = -1;
        int innerIteratorIndex = -1;
        int interpolatorIndex = -1;

        ILLabel jumpColorCtorTarget = c.DefineLabel();

        for (int i = 0; i < 5; i++)
        {
            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdloca(out colorIndex),
                    i => i.MatchLdloc(out rIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchLdloc(out gIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchLdloc(out bIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchLdloc(out aIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchCall<Color>(".ctor")))
            {
                Log.Warn("Failed to patch main menu button color constructor.");
                return;
            }

            if (i == 4)
                break;
        }

        c.MarkLabel(jumpColorCtorTarget);

        if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out innerIteratorIndex),
                i => i.MatchLdcI4(4),
                i => i.MatchBneUn(out _)))
        {
            Log.Warn("Failed to find main menu draw-pass check for button color patch.");
            return;
        }

        if (!c.TryGotoPrev(MoveType.Before,
                i => i.MatchLdloc(out hoveredIndex),
                i => i.MatchLdloc(out outerIteratorIndex),
                i => i.MatchBneUn(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdcI4(4),
                i => i.MatchBneUn(out _),
                i => i.MatchLdloc(out interpolatorIndex)))
        {
            Log.Warn("Failed to find main menu hover-color block for button color patch.");
            return;
        }

        c.MoveAfterLabels();

        c.EmitLdloca(colorIndex);
        c.EmitLdloc(buttonNamesIndex);
        c.EmitLdloc(innerIteratorIndex);
        c.EmitLdloc(outerIteratorIndex);
        c.EmitLdloc(hoveredIndex);
        c.EmitLdloc(interpolatorIndex);

        c.EmitDelegate((ref Color color, string[] buttonNames, int drawPass, int buttonIndex, int hoveredIndex, int interpolator) =>
        {
            if (drawPass != 4)
                return false;

            if (buttonNames == null || (uint)buttonIndex >= (uint)buttonNames.Length)
                return false;

            if (buttonNames[buttonIndex] != ButtonLabel)
                return false;

            //Color TextColor = new(255, 64, 96);
            //Color HoverColor = new(255, 92, 92); // brighter red
            //Color HoverColor = new(255, 200, 255);
            Color TextColor = new(90, 210, 255);
            Color HoverColor = new(255, 240, 80);
            color = Color.Lerp(TextColor, HoverColor, hoveredIndex == buttonIndex ? interpolator / 255f : 0f);
            return true;
        });

        c.EmitBrtrue(jumpColorCtorTarget);
    }
    #endregion

    private void DrawMenuUI(On_Main.orig_DrawVersionNumber orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        if (!Main.gameMenu)
            return;

        if (ui?.CurrentState != null)
        {
            DrawInterface(ui);
            return;
        }

        DrawReplayOverlay();
    }

    private static bool ShouldShowOverlay()
    {
        return Main.gameMenu && Main.menuMode == 0 && ModContent.GetInstance<ClientConfig>().ShowInMainMenu;
    }

    private void DrawReplayOverlay()
    {
        if (ShouldShowOverlay() && reeseMainMenuUI?.CurrentState != null)
            DrawInterface(reeseMainMenuUI);
    }

    private void PostUpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        // Hotfix menuMode 14 not cancelling properly
        if (IsLaunchingReplay && Netplay.Disconnect)
        {
            Netplay.Disconnect = false;
            CancelReplayLaunch();
        }

        // Escape
        if (Main.gameMenu && KeyboardHelper.Pressed(Keys.Escape))
        {
            if (IsLaunchingReplay)
                CancelReplayLaunch();
            else if (ui?.CurrentState != null)
                MainMenuActions.CloseReplayBrowser(ui, reeseMainMenuUI);
        }

        // Update interface
        if (Main.gameMenu && ui?.CurrentState != null)
            UpdateInterface(ui, gameTime);
        else
            UpdateReplayOverlay(gameTime);

        orig(gameTime);

        if (!Main.gameMenu)
        {
            MainMenuActions.CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: false);
            return;
        }

        if (ui?.CurrentState != null)
            Main.menuMode = SharedMenuMode;
    }

    private void UpdateReplayOverlay(GameTime gameTime)
    {
        if (ShouldShowOverlay())
        {
            if (reeseMainMenuUI == null)
                return;

            if (reeseMainMenuUI.CurrentState == null)
                reeseMainMenuUI.SetState(reeseMainMenuState);

            UpdateInterface(reeseMainMenuUI, gameTime);
        }
        else if (reeseMainMenuUI?.CurrentState != null)
        {
            reeseMainMenuUI.SetState(null);
        }
    }

    private static void DrawInterface(UserInterface userInterface)
    {
        var old = UserInterface.ActiveInstance;

        try
        {
            UserInterface.ActiveInstance = userInterface;
            userInterface.Draw(Main.spriteBatch, new GameTime());
        }
        finally
        {
            UserInterface.ActiveInstance = old;
        }
    }

    private static void UpdateInterface(UserInterface userInterface, GameTime gameTime)
    {
        var old = UserInterface.ActiveInstance;

        try
        {
            UserInterface.ActiveInstance = userInterface;
            userInterface.Update(gameTime);
        }
        finally
        {
            UserInterface.ActiveInstance = old;
        }
    }
    private ReplayLaunchSession activeSession;

    public ReplayLaunchSession BeginReplayLaunch()
    {
        activeSession?.Cancel();
        activeSession?.Dispose();
        activeSession = new ReplayLaunchSession(ReplayPlayback.BeginLaunchAttempt());
        MainMenuActions.BeginReplayLaunch(ui, reeseMainMenuUI);
        return activeSession;
    }

    public void CancelReplayLaunch()
    {
        ReplayPlayback.CancelLaunchAttempt(activeSession?.Generation ?? 0);
        ReplayPlayback.IsLaunchCancelled = null;
        activeSession?.Cancel();
        activeSession?.Dispose();
        activeSession = null;
        MainMenuActions.CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: true);
    }

    public void CompleteReplayLaunch()
    {
        ReplayPlayback.CompleteLaunchAttempt(activeSession?.Generation ?? 0);
        ReplayPlayback.IsLaunchCancelled = null;
        activeSession?.Dispose();
        activeSession = null;
    }

    public bool IsLaunchingReplay => activeSession != null && !activeSession.IsCancelled && ReplayPlayback.IsReplayLaunchActive;

    // Actions
    internal void OpenConfirmDelete(string targetName, Action onConfirm)
    {
        MainMenuActions.OpenConfirmDelete(ui, reeseMainMenuUI, targetName, onConfirm);
    }

    internal void OpenRename(string currentName, Action<string> onSubmit)
    {
        MainMenuActions.OpenRename(ui, reeseMainMenuUI, currentName, onSubmit);
    }

    internal void OpenClientConfig()
    {
        MainMenuActions.OpenClientConfig(ui, reeseMainMenuUI);
    }

    internal void CloseForReplayLaunch()
    {
        MainMenuActions.CloseForReplayLaunch(ui, reeseMainMenuUI);
    }
}
