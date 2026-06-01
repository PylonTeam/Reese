using Reese.Common.Replayer.ReplayHud.ReplayInfo;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Ghost;

internal sealed class GhostHud : UIElement
{
    private const float TabHeight = 36f;
    private const float PlayerHudOpenOffset = 200f;

    public UIPanel TitlePanel;
    public UIPanel ContentPanel;

    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private TabBar tabBar;
    private UIText titleText;
    private bool isShiftedForTeammateAccessoriesHud;

    public GhostHud()
    {
        HAlign = 1f;
        SetPanelLeft(TeammateHudOverlay.IsAnyOpen);
        Top.Set(InfoHud.TopOffset, 0f);
        Width.Set(InfoHud.PanelWidth, 0f);

        tabs.Add(new SettingsTab());
        tabs.Add(new WorldInfoTab());
        currentTab = tabs[0];

        Rebuild();
    }

    private void Rebuild()
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
            Top = new StyleDimension(InfoHud.HeaderHeight + TabHeight, 0f),
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(-(InfoHud.HeaderHeight + TabHeight), 1f),
            BackgroundColor = new Color(12, 18, 42) * 0.96f,
            BorderColor = Color.Black
        };
        ContentPanel.SetPadding(0f);
        Append(ContentPanel);

        ShowTab(currentTab?.Tab ?? SpectatorTab.World);
    }

    public override void Update(GameTime gameTime)
    {
        UpdatePanelPosition();
        UpdateTitleText();
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    private void UpdatePanelPosition()
    {
        bool shouldShift = TeammateHudOverlay.IsAnyOpen;

        if (shouldShift == isShiftedForTeammateAccessoriesHud)
            return;

        SetPanelLeft(shouldShift);
        Recalculate();
    }

    private void SetPanelLeft(bool shiftedForPlayerHud)
    {
        isShiftedForTeammateAccessoriesHud = shiftedForPlayerHud;
        Left.Set(-InfoHud.RightOffset - (shiftedForPlayerHud ? PlayerHudOpenOffset : 0f), 0f);
    }

    private void BuildTitlePanel()
    {
        TitlePanel = new UIPanel();
        TitlePanel.Height.Set(InfoHud.HeaderHeight, 0f);
        TitlePanel.Width.Set(0f, 1f);
        TitlePanel.SetPadding(0f);
        TitlePanel.BackgroundColor = new Color(31, 43, 95);
        TitlePanel.BorderColor = Color.Black;

        titleText = new UIText(GetGhostTitle(), large: false, textScale: 1f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        TitlePanel.Append(titleText);
    }

    private void BuildTabPanel()
    {
        tabBar = new TabBar();
        tabBar.Top.Set(InfoHud.HeaderHeight, 0f);
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

    private void UpdateTitleText()
    {
        titleText?.SetText(GetGhostTitle());
    }

    private static string GetGhostTitle()
    {
        return "Ghost";
    }
}
