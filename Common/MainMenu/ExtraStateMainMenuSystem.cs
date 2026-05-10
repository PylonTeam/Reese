using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using Reese.Common.MainMenu.UI;
using Reese.Core.Configs;
using Reese.Core.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.MainMenu;

/// <summary>
/// Adds a Reese Replays button to the Main Menu.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class ExtraStateMainMenuSystem : ModSystem
{
    public UserInterface ui;
    private UserInterface reeseMainMenuUI;
    private UIState reeseMainMenuState;
    private bool wasHovered;

#if DEBUG
    public static bool IsEnabled => true;
#else
    public static bool IsEnabled => true;
#endif

    public override void Load()
    {
        if (!IsEnabled) return;

        Main.QueueMainThreadAction(() => IL_Main.DrawMenu += InjectMatchmakingButton);

        ui = new UserInterface();

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
        if (!IsEnabled) return;

        Main.QueueMainThreadAction(() => IL_Main.DrawMenu -= InjectMatchmakingButton);
        On_Main.DrawVersionNumber -= DrawMenuUI;
        On_Main.UpdateUIStates -= PostUpdateUIStates;
        ui = null;
        reeseMainMenuUI = null;
        reeseMainMenuState = null;
    }

    private void InjectMatchmakingButton(ILContext il)
    {
        IL.Edit(il, c =>
        {
            int array9Index = -1;
            int array7Index = -1;
            int offYIndex = -1;
            int spacingIndex = -1;
            int num9Index = -1;
            int num11Index = -1;

            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Main>("selectedMenu"),
                    i => i.MatchLdloc(out array9Index),
                    i => i.MatchLdloc(out array7Index),
                    i => i.MatchLdloca(out offYIndex),
                    i => i.MatchLdloca(out spacingIndex),
                    i => i.MatchLdloca(out num9Index),
                    i => i.MatchLdloca(out num11Index),
                    i => i.MatchCall(out var m)
                         && m.DeclaringType.FullName == "Terraria.ModLoader.UI.Interface"
                         && m.Name == "AddMenuButtons"))
            {
                return;
            }

            c.Index = 0;

            int num11InitIndex = -1;
            int num2Index = -1;
            int num5Index = -1;
            int num4Index = -1;

            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(0),
                    i => i.MatchStloc(out num11InitIndex),
                    i => i.MatchLdcI4(220),
                    i => i.MatchStloc(out num2Index),
                    i => i.MatchLdcI4(7),
                    i => i.MatchStloc(out num5Index),
                    i => i.MatchLdcI4(52),
                    i => i.MatchStloc(out num4Index)))
            {
                return;
            }

            c.EmitLdloc(array9Index);
            c.EmitLdloca(num11InitIndex);
            c.EmitLdloca(num5Index);

            c.EmitDelegate((string[] labels, ref int num11, ref int num5) =>
            {
                if (labels == null)
                    return;

                if (!ModContent.GetInstance<ClientConfig>().AddExtraMenuState)
                    return;

                labels[num11] = "Reese";
                num11++;
                num5++;
            });
        });
    }

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

        if (Main.menuMode != 0)
            return;

        DrawReplayOverlay();
        DrawHeaderTextAndHandleClicks();
    }

    private static bool ShouldShowOverlay()
    {
        return Main.gameMenu && Main.menuMode == 0 && ModContent.GetInstance<ClientConfig>().ShowInMainMenu;
    }

    private void DrawReplayOverlay()
    {
        if (!ShouldShowOverlay() || reeseMainMenuUI?.CurrentState == null)
            return;

        DrawInterface(reeseMainMenuUI);
    }

    private void DrawHeaderTextAndHandleClicks()
    {
        if (!ModContent.GetInstance<ClientConfig>().AddExtraMenuState)
            return;

        const string label = "Reese";

        var font = FontAssets.DeathText.Value;
        Vector2 baseSize = font.MeasureString(label);
        Vector2 center = new(Main.screenWidth * 0.5f, 200);
        Vector2 topLeft = center - baseSize * 0.5f;

        var hitbox = new Rectangle(
            (int)(topLeft.X - 0),
            (int)(topLeft.Y + 56),
            (int)(baseSize.X + 0),
            (int)(baseSize.Y - 20)
        );

        bool hovered = hitbox.Contains(Main.MouseScreen.ToPoint());

        if (hovered && !wasHovered)
            SoundEngine.PlaySound(SoundID.MenuTick);

        Color color = hovered ? new Color(255, 240, 20) : Color.Gray;

#if DEBUG
        // debug draw: do not delete!
        //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hitbox, color*0.8f);
#endif

        if (hovered)
        {
            Main.blockMouse = true;

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.menuMode = 888;
                Main.QueueMainThreadAction(OpenReplayBrowser);
            }
        }

        wasHovered = hovered;
    }

    private void OpenReplayBrowser()
    {
        var state = new ExtraReeseMainMenuUIState(CloseReplayBrowser);
        ui?.SetState(state);
    }

    internal void OpenConfirmDelete(string targetName, Action onConfirm)
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
                    OpenReplayBrowser();
                else
                    CloseReplayBrowser();
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

    internal void OpenRename(string currentName, Action<string> onSubmit)
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
                    OpenReplayBrowser();
                else
                    CloseReplayBrowser();
            });
        }

        Main.clrInput();
        ui.SetState(new ConfirmRenameState(currentName, name => Close(() => onSubmit?.Invoke(name)), () => Close(null)));
    }

    internal void OpenClientConfig()
    {
        Main.QueueMainThreadAction(() =>
        {
            CloseAllMenuUI(resetMenuMode: true);
            ModContent.GetInstance<ClientConfig>().Open(() =>
            {
                CloseAllMenuUI(resetMenuMode: true);
                Main.menuMode = 0;
            });
        });
    }

    internal void CloseForReplayLaunch()
    {
        CloseAllMenuUI(resetMenuMode: false);
    }

    private void CloseReplayBrowser()
    {
        CloseAllMenuUI(resetMenuMode: true);
    }

    private void CloseAllMenuUI(bool resetMenuMode)
    {
        ui?.SetState(null);
        reeseMainMenuUI?.SetState(null);

        if (resetMenuMode)
            Main.menuMode = 0;

        Main.blockMouse = false;
    }

    private void PostUpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        if (Main.gameMenu && KeyboardHelper.Pressed(Keys.Escape) && (ui?.CurrentState != null || Main.menuMode == 888))
            CloseReplayBrowser();

        if (Main.gameMenu && ui?.CurrentState != null)
            UpdateInterface(ui, gameTime);
        else
            UpdateReplayOverlay(gameTime);

        orig(gameTime);

        if (!Main.gameMenu)
        {
            CloseAllMenuUI(resetMenuMode: false);
            return;
        }

        // If we left our custom empty-background menu, kill matchmaking UI
        if (ui?.CurrentState != null)
            Main.menuMode = 888;
    }

    private void UpdateReplayOverlay(GameTime gameTime)
    {
        if (ShouldShowOverlay())
        {
            if (reeseMainMenuUI == null)
                return;

            if (reeseMainMenuUI?.CurrentState == null)
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
}
