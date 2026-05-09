using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.SpectatorMode;
using Reese.Common.ReplaySpectate.TeammateOverlay;
using Reese.Core.Debug;
using Reese.UI;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.ReplaySpectate.UI;
public class SpectatorUIState : UIState
{
    private SpectatorControlsPanel controlsPanel;
    private SpectatorInfoPanel infoPanel;
    private SpectatorInfoEyeOnlyButton infoEyeButton;

    private bool joinPanelOpen;
    private bool infoExpanded = true;

    public override void OnActivate()
    {
        RemoveAllChildren();
    }

    public void Rebuild()
    {
        controlsPanel?.Rebuild();
        infoPanel?.Rebuild();
    }

    public void OpenJoinPanel()
    {
        joinPanelOpen = true;
        UpdateVisiblePanels();
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    public void CloseJoinPanel()
    {
        if (!joinPanelOpen)
            return;

        joinPanelOpen = false;
        UpdateVisiblePanels();
        SoundEngine.PlaySound(SoundID.MenuClose);
    }

    public void ToggleInfoPanel()
    {
        infoExpanded = !infoExpanded;
        UpdateVisiblePanels();
    }

    public void OnLocalModeAccepted(SpectateMode mode)
    {
        joinPanelOpen = false;

        if (mode == SpectateMode.Spectator)
        {
            Main.playerInventory = false;
            EnsureSpectatorHUDStaysOpen();
            UpdateVisiblePanels();
        }

        UpdateVisiblePanels();
    }

    public void EnsureSpectatorHUDStaysOpen()
    {
        controlsPanel ??= new SpectatorControlsPanel();

        if (controlsPanel.Parent is null)
            Append(controlsPanel);
    }

    public override void Update(GameTime gameTime)
    {
        UpdateVisiblePanels();

        if (KeyboardHelper.Pressed(Microsoft.Xna.Framework.Input.Keys.F5))
        {
            Log.Chat("F5 pressed, force rebuilding...");
            Rebuild();
        }

        base.Update(gameTime);

        Player local = Main.LocalPlayer;

        if (local?.active == true && SpectatorModeSystem.IsInSpectateMode(local))
        {
            PlayerHudOverlay.Update();
            controlsPanel?.UpdateTarget();
        }
        else
        {
            PlayerHudOverlay.Clear();
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        Player local = Main.LocalPlayer;

        if (local?.active == true && SpectatorModeSystem.IsInSpectateMode(local))
            PlayerHudOverlay.Draw(sb);
    }

    private void UpdateVisiblePanels()
    {
        Player local = Main.LocalPlayer;
        bool validPlayer = !Main.gameMenu && local?.active == true;
        bool spectating = validPlayer && SpectatorModeSystem.IsInSpectateMode(local);

        if (!validPlayer)
        {
            RemoveSpectatorPanels();
            return;
        }

        if (joinPanelOpen && !spectating)
        {
            RemoveSpectatorPanels();
            return;
        }

        if (spectating)
        {
            EnsureSpectatorHUDStaysOpen();
            EnsureInfoPanelState();
            return;
        }

        RemoveSpectatorPanels();
    }

    private void EnsureInfoPanelState()
    {
        if (infoExpanded)
        {
            infoEyeButton?.Remove();

            if (infoPanel?.Parent is null)
            {
                infoPanel = new SpectatorInfoPanel();
                Append(infoPanel);
            }

            return;
        }

        infoEyeButton ??= new SpectatorInfoEyeOnlyButton();

        infoPanel?.Remove();
        infoPanel = null;

        if (infoEyeButton.Parent is null)
            Append(infoEyeButton);
    }

    private void RemoveSpectatorPanels()
    {
        controlsPanel?.Remove();
        infoPanel?.Remove();
        infoEyeButton?.Remove();
        infoPanel = null;
    }
}

public class SpectatorInfoEyeOnlyButton : UIPanel
{
    public SpectatorInfoEyeOnlyButton()
    {
        HAlign = 1f;
        Left.Set(-SpectatorInfoPanel.RightOffset, 0f);
        Top.Set(SpectatorInfoPanel.TopOffset, 0f);
        Width.Set(SpectatorInfoPanel.HeaderHeight, 0f);
        Height.Set(SpectatorInfoPanel.HeaderHeight, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(63, 82, 151);
        BorderColor = Color.Black;

        OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            ModContent.GetInstance<SpectatorUISystem>().ToggleSpectatorInfoPanel();
        };

        Append(new UIImage(Ass.Icon_Eye.Value)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        BorderColor = IsMouseHovering ? Color.Yellow : Color.Black;

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText("Show spectator info");
        }
    }
}
