using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayInfo;

internal sealed class InfoHud : UIElement
{
    internal const float PanelWidth = 278f;
    internal const float PanelHeight = 475f;
    internal const float HeaderHeight = 32f;
    internal const float TopOffset = 335f;
    internal const float RightOffset = 4f;

    private const float TabHeight = 36f;
    private const float PlayerHudOpenOffset = 200f;

    public UIPanel TitlePanel;
    public UIPanel ContentPanel;

    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private TabBar tabBar;
    private bool isShiftedForPlayerHud;

    public InfoHud()
    {
        HAlign = 1f;
        SetPanelLeft(TeammateHudOverlay.IsAnyOpen);
        Top.Set(TopOffset, 0f);
        Width.Set(PanelWidth, 0f);

        tabs.Add(new ReplayInfoTab());
        tabs.Add(new SettingsTab());
        tabs.Add(new WorldInfoTab());
        currentTab = tabs[0];

        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();
        TitlePanel = null;
        ContentPanel = null;
        tabBar = null;

        Height.Set(525, 0f);

        BuildTitlePanel();
        Append(TitlePanel);

        BuildTabPanel();
        Append(tabBar);

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
        bool shouldShift = TeammateHudOverlay.IsAnyOpen;

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

        UIPanel closePanel = new()
        {
            Height = new StyleDimension(0f, 1f),
            Width = new StyleDimension(40f, 0f),
            HAlign = 1f,
            VAlign = 0.5f,
            BackgroundColor = TitlePanel.BackgroundColor
        };
        closePanel.SetPadding(0f);
        closePanel.Append(new UIText("X", large: true, textScale: 0.55f) { HAlign = 0.5f, VAlign = 0.5f });
        TitlePanel.Append(closePanel);

    }

    private void BuildTabPanel()
    {
        tabBar = new TabBar();
        tabBar.Top.Set(HeaderHeight, 0f);
        tabBar.Width.Set(0f, 1f);
        tabBar.Height.Set(TabHeight, 0f);
        tabBar.BuildTabs(tabs, () => currentTab, ShowTab, 1f);
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

        tabBar?.RefreshButtons();

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

}
