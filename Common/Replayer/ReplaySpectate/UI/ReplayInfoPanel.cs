using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplaySpectate.UI.Tabs;
using Reese.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplaySpectate.UI;

internal sealed class ReplayInfoPanel : UIElement
{
    internal static float PanelWidth => UINPCCard.CardWidth + 48f;
    internal const float PanelHeight = 475f;
    internal const float HeaderHeight = 32f;
    internal const float TopOffset = 335f;
    internal const float RightOffset = 4f;

    private const float TabHeight = 36f;
    private const float PlayerHudOpenOffset = 200f;

    public UIPanel TitlePanel;
    public UIPanel ContentPanel;
    public UIPanel EyeTogglePanel;

    private readonly List<ITab> tabs = [];
    private readonly List<SpectatorTabButton> tabButtons = [];
    private ITab currentTab;
    private UIPanel tabPanel;
    private bool isShiftedForPlayerHud;

    public ReplayInfoPanel()
    {
        HAlign = 1f;
        SetPanelLeft(PlayerHudOverlay.IsAnyOpen);
        Top.Set(TopOffset, 0f);
        Width.Set(PanelWidth, 0f);

        tabs.Add(new ReplayTab());
        tabs.Add(new WorldTab());
        tabs.Add(new NPCTab());
        currentTab = tabs[0];

        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();
        tabButtons.Clear();

        TitlePanel = null;
        ContentPanel = null;
        EyeTogglePanel = null;
        tabPanel = null;

        Height.Set(PanelHeight, 0f);

        BuildTitlePanel();
        Append(TitlePanel);

        BuildTabPanel();
        Append(tabPanel);

        ContentPanel = new UIPanel
        {
            Top = new StyleDimension(HeaderHeight + TabHeight, 0f),
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(-(HeaderHeight + TabHeight), 1f),
            BackgroundColor = new Color(20, 20, 60) * 0.7f,
            BorderColor = Color.Black
        };
        ContentPanel.SetPadding(0f);
        Append(ContentPanel);

        ShowTab(currentTab?.Tab ?? SpectatorTab.World);
    }

    public override void Update(GameTime gameTime)
    {
        UpdatePanelPosition();
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    private void UpdatePanelPosition()
    {
        bool shouldShift = PlayerHudOverlay.IsAnyOpen;

        if (shouldShift == isShiftedForPlayerHud)
            return;

        SetPanelLeft(shouldShift);
        Recalculate();
    }

    private void SetPanelLeft(bool shiftedForPlayerHud)
    {
        isShiftedForPlayerHud = shiftedForPlayerHud;
        Left.Set(-RightOffset - (shiftedForPlayerHud ? PlayerHudOpenOffset : 0f), 0f);
    }

    private void BuildTitlePanel()
    {
        TitlePanel = new UIPanel();
        TitlePanel.Height.Set(HeaderHeight, 0f);
        TitlePanel.Width.Set(0f, 1f);
        TitlePanel.SetPadding(0f);
        TitlePanel.BackgroundColor = new Color(63, 82, 151);
        TitlePanel.BorderColor = Color.Black;

        UIText titleText = new("Replay Info", large: false, textScale: 1f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        TitlePanel.Append(titleText);

        EyeTogglePanel = new PanelEyeToggleButton();

        TitlePanel.Append(EyeTogglePanel);
    }

    private void BuildTabPanel()
    {
        tabPanel = new UIPanel();
        tabPanel.Top.Set(HeaderHeight, 0f);
        tabPanel.Width.Set(0f, 1f);
        tabPanel.Height.Set(TabHeight, 0f);
        tabPanel.SetPadding(0f);
        tabPanel.BackgroundColor = new Color(20, 20, 60) * 0.85f;
        tabPanel.BorderColor = Color.Black;

        for (int i = 0; i < tabs.Count; i++)
        {
            ITab capturedTab = tabs[i];
            SpectatorTabButton button = new(capturedTab.HeaderText, capturedTab.TooltipText, capturedTab.Icon, capturedTab.IconScale, capturedTab.IconOffset, () => currentTab == capturedTab, () => ShowTab(capturedTab.Tab));
            button.Left.Set(0f, i / (float)tabs.Count);
            button.Width.Set(0f, 1f / tabs.Count);

            tabPanel.Append(button);
            tabButtons.Add(button);
        }
    }

    internal sealed class PanelEyeToggleButton : UIPanel
    {
        public PanelEyeToggleButton()
        {
            Height = new StyleDimension(0f, 1f);
            Width = new StyleDimension(HeaderHeight, 0f);
            HAlign = 1f;
            VAlign = 0.5f;
            SetPadding(0f);

            OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                ModContent.GetInstance<ReplayUISystem>().ToggleSpectatorInfoPanel();
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
                Main.instance.MouseText("Hide Replay Info");
            }
        }
    }

    private void ShowTab(SpectatorTab tab)
    {
        ITab nextTab = GetTab(tab);

        if (nextTab is null || ContentPanel is null)
            return;

        ContentPanel.RemoveAllChildren();
        currentTab = nextTab;

        UIElement element = (UIElement)currentTab;
        element.Width.Set(0f, 1f);
        element.Height.Set(0f, 1f);
        element.SetPadding(0f);

        ContentPanel.Append(element);
        currentTab.Refresh();

        foreach (SpectatorTabButton button in tabButtons)
            button.Recalculate();

        Recalculate();
    }

    private ITab GetTab(SpectatorTab tab)
    {
        foreach (ITab candidate in tabs)
        {
            if (candidate.Tab == tab)
                return candidate;
        }

        return null;
    }

    private sealed class SpectatorTabButton : UIPanel
    {
        private readonly Func<bool> isSelected;
        private readonly string hoverText;

        public SpectatorTabButton(string headerText, string tooltipText, Asset<Texture2D> icon, float iconScale, Vector2 iconOffset, Func<bool> isSelected, Action onClick)
        {
            this.isSelected = isSelected;
            hoverText = tooltipText;

            Height.Set(0f, 1f);
            VAlign = 0.5f;
            SetPadding(0f);

            OnLeftClick += (_, _) => onClick();

            Append(new UIImage(icon.Value)
            {
                Left = new StyleDimension(6f + iconOffset.X, 0f),
                Top = new StyleDimension(-5f + iconOffset.Y, 0f),
                VAlign = 0.5f,
                Width = new StyleDimension(20f, 0f),
                Height = new StyleDimension(20f, 0f),
                ImageScale = iconScale
            });

            Append(new UIText(headerText, textScale: 0.85f)
            {
                Left = new StyleDimension(38f, 0f),
                VAlign = 0.5f
            });
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            BackgroundColor = isSelected() ? new Color(83, 97, 168) : new Color(63, 82, 151) * 0.85f;
            BorderColor = IsMouseHovering ? Color.Yellow : isSelected() ? Color.White : Color.Black;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(hoverText);
            }
        }
    }
}
