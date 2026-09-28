using Reese.Common.MainMenu.UI;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
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
/// Adds a custom Reese Replays button after Pylon (or Multiplayer when Pylon is absent).
/// Adds a <see cref="MainMenuUIState"/> with the <see cref="ReplayBrowserPanel"/>
/// </summary>
[Autoload(Side = ModSide.Client)]
public class MainMenuSystem : ModSystem
{
    private const int SharedMenuMode = 888;
    private const string ButtonLabel = "Reese";
    private static readonly TimeSpan ForceLoadReplayLaunchTimeout = TimeSpan.FromSeconds(10);


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

        MainMenuTextThemeDrawer.Unload();
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

            // Pylon inserts at the preceding index increment, keeping Reese directly below it.
            if (!c.TryGotoPrev(MoveType.Before,
                    i => i.MatchLdloc(buttonIndexIndex),
                    i => i.MatchLdsfld<Lang>("menu"),
                    i => i.MatchLdcI4(131)))
            {
                Log.Warn("Failed to find main menu insertion point before Achievements.");
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
            PatchButtonText(c, buttonNamesIndex);

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

        OpenReplayBrowserFromExternal();
        return -1;
    }

    public bool OpenReplayBrowserFromExternal()
    {
        if (!IsEnabled || Main.dedServ || !Main.gameMenu || ui == null || reeseMainMenuUI == null)
            return false;

        Main.mouseLeftRelease = false;
        Main.blockMouse = true;
        Main.menuMode = SharedMenuMode;

        SoundEngine.PlaySound(SoundID.MenuOpen);
        MainMenuActions.OpenReplayBrowser(ui, reeseMainMenuUI);
        return true;
    }

    private delegate bool DrawButtonTextDelegate(
        SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position,
        Color color, float rotation, Vector2 origin, float scale,
        SpriteEffects effects, float layerDepth, int drawPass, bool hovered);

    private static void PatchButtonText(ILCursor c, int buttonNamesIndex)
    {
        int buttonIndex = -1;
        int drawPassIndex = -1;
        int hoveredIndex = -1;

        // Find the final menu-label loop by its own text array, rather than counting color constructors.
        if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(buttonNamesIndex),
                i => i.MatchLdloc(out buttonIndex),
                i => i.MatchLdelemRef(),
                i => i.MatchCallvirt<DynamicSpriteFont>(nameof(DynamicSpriteFont.MeasureString))) ||
            !c.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out hoveredIndex),
                i => i.MatchLdloc(buttonIndex),
                i => i.MatchBneUn(out _),
                i => i.MatchLdloc(out drawPassIndex),
                i => i.MatchLdcI4(4),
                i => i.MatchBneUn(out _)))
        {
            Log.Warn("Failed to find the main menu text draw loop for flame effects.");
            return;
        }

        int patchedDrawCalls = 0;

        // Terraria has centered and left-aligned paths. Preserve each native call so another mod
        // can wrap it too, regardless of load order, while skipping it when our label is rendered.
        while (patchedDrawCalls < 2 && c.TryGotoNext(MoveType.Before,
                   i => i.MatchCall(typeof(DynamicSpriteFontExtensionMethods), "DrawString")))
        {
            MethodReference drawMethod = (MethodReference)c.Next.Operand;
            if (drawMethod.Parameters.Count != 10 || drawMethod.Parameters[7].ParameterType.FullName != "System.Single")
            {
                Log.Warn("Unexpected main menu DrawString signature; flame effects were not applied.");
                return;
            }

            VariableDefinition[] arguments = new VariableDefinition[drawMethod.Parameters.Count];
            for (int i = 0; i < arguments.Length; i++)
            {
                arguments[i] = new VariableDefinition(drawMethod.Parameters[i].ParameterType);
                c.Context.Body.Variables.Add(arguments[i]);
            }

            ILLabel afterDraw = c.DefineLabel();
            c.MoveAfterLabels();
            for (int i = arguments.Length - 1; i >= 0; i--)
                c.Emit(OpCodes.Stloc, arguments[i]);
            foreach (VariableDefinition argument in arguments)
                c.Emit(OpCodes.Ldloc, argument);

            c.EmitLdloc(drawPassIndex);
            c.EmitLdloc(buttonIndex);
            c.EmitLdloc(hoveredIndex);
            c.Emit(OpCodes.Ceq);
            c.EmitDelegate<DrawButtonTextDelegate>(TryDrawButtonText);
            c.EmitBrtrue(afterDraw);

            foreach (VariableDefinition argument in arguments)
                c.Emit(OpCodes.Ldloc, argument);
            c.Index++; // Leave the original DrawString call in place.
            c.MarkLabel(afterDraw);
            patchedDrawCalls++;
        }

        if (patchedDrawCalls != 2)
            Log.Warn("Could not patch both main menu text alignment paths for flame effects.");
    }

    private static bool TryDrawButtonText(
        SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position,
        Color color, float rotation, Vector2 origin, float scale,
        SpriteEffects effects, float layerDepth, int drawPass, bool hovered)
    {
        if (!Main.gameMenu || Main.menuMode != 0 || drawPass != 4 || text != ButtonLabel)
            return false;

        MainMenuTextThemeDrawer.Draw(batch, font, text, position, color, rotation, origin,
            Vector2.One * scale, effects, layerDepth, hovered);
        return true;
    }

    #endregion

    private void DrawMenuUI(On_Main.orig_DrawVersionNumber orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        if (!Main.gameMenu)
            return;

        if (Main.menuMode == SharedMenuMode && ui?.CurrentState != null)
        {
            DrawInterface(ui);
        }
    }

    private void PostUpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        // Hotfix menuMode 14 not cancelling properly
        if (IsLaunchingReplay && Netplay.Disconnect)
        {
            Netplay.Disconnect = false;
            if (IsForceLoadingModMismatch)
                FailReplayLaunch(timedOut: false);
            else
                CancelReplayLaunch();
        }
        else if (IsLaunchingReplay &&
                 IsForceLoadingModMismatch &&
                 DateTime.UtcNow >= forceLoadReplayDeadlineUtc)
        {
            FailReplayLaunch(timedOut: true);
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
        if (Main.gameMenu && Main.menuMode == SharedMenuMode && ui?.CurrentState != null)
            UpdateInterface(ui, gameTime);

        orig(gameTime);

        if (!Main.gameMenu)
        {
            MainMenuActions.CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: false);
            return;
        }

        if (ui?.CurrentState != null)
            Main.menuMode = SharedMenuMode;
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
    private string forcedModMismatchReason;
    private DateTime forceLoadReplayDeadlineUtc;

    public ReplayLaunchSession BeginReplayLaunch(string forcedModMismatchReason = null)
    {
        activeSession?.Cancel();
        activeSession?.Dispose();
        Netplay.Disconnect = false;
        this.forcedModMismatchReason = forcedModMismatchReason;
        forceLoadReplayDeadlineUtc = DateTime.UtcNow + ForceLoadReplayLaunchTimeout;
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
        forcedModMismatchReason = null;
        MainMenuActions.CloseAllMenuUI(ui, reeseMainMenuUI, resetMenuMode: true);
    }

    public void CompleteReplayLaunch()
    {
        ReplayPlayback.CompleteLaunchAttempt(activeSession?.Generation ?? 0);
        ReplayPlayback.IsLaunchCancelled = null;
        activeSession?.Dispose();
        activeSession = null;
        forcedModMismatchReason = null;
    }

    public bool IsLaunchingReplay => activeSession != null && !activeSession.IsCancelled && ReplayPlayback.IsReplayLaunchActive;
    private bool IsForceLoadingModMismatch => !string.IsNullOrWhiteSpace(forcedModMismatchReason);

    private void FailReplayLaunch(bool timedOut)
    {
        string message = timedOut
            ? Loc.Get("MainMenu.ReplayStartup.ForceLoadTimedOut", (int)ForceLoadReplayLaunchTimeout.TotalSeconds)
            : Loc.Get("MainMenu.ReplayStartup.ForceLoadFailed");

        if (!string.IsNullOrWhiteSpace(forcedModMismatchReason))
            message += "\n" + Loc.Get("MainMenu.ReplayStartup.ModMismatch", forcedModMismatchReason);

        message += "\n" + Loc.Get("MainMenu.ReplayStartup.ForceLoadAdvice");

        Log.Error(message);

        Netplay.Disconnect = true;
        if (Netplay.Connection != null)
        {
            Netplay.Connection.IsActive = false;
            Netplay.Connection.StatusText = string.Empty;
        }

        ReplayPlayback.End(message);
        CancelReplayLaunch();

        Main.statusText = message;
        Main.MenuUI.SetState(null);
        Main.menuMode = MenuID.MultiplayerJoining;
    }

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
