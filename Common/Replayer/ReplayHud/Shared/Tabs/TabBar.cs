using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.Tabs;

internal sealed class TabBar : UIPanel
{
    private readonly List<TabButton> buttons = [];
    private IReadOnlyList<ITab> tabs;

    public TabBar()
    {
        SetPadding(0f);
        BackgroundColor = new Color(12, 18, 42) * 0.96f;
        BorderColor = Color.Black;
    }

    public void BuildTabs(IReadOnlyList<ITab> tabs, Func<ITab> getCurrentTab, Action<SpectatorTab> onTabSelected, float scale = 1f)
    {
        this.tabs = tabs;

        RemoveAllChildren();
        buttons.Clear();

        if (tabs == null || tabs.Count == 0)
            return;

        for (int i = 0; i < tabs.Count; i++)
        {
            ITab capturedTab = tabs[i];
            TabButton button = new(
                capturedTab.HeaderText,
                capturedTab.TooltipText,
                capturedTab.Icon,
                capturedTab.IconScale,
                capturedTab.IconOffset,
                capturedTab.TextOffset,
                () => getCurrentTab() == capturedTab,
                () => onTabSelected(capturedTab.Tab),
                scale);
            button.Left.Set(0f, i / (float)tabs.Count);
            button.Width.Set(0f, 1f / tabs.Count);

            Append(button);
            buttons.Add(button);
        }
    }

    public void RefreshButtons()
    {
        foreach (TabButton button in buttons)
            button.Recalculate();
    }

    public void RefreshHeaders()
    {
        if (tabs == null)
            return;

        int count = Math.Min(buttons.Count, tabs.Count);

        for (int i = 0; i < count; i++)
            buttons[i].SetHeaderText(tabs[i].HeaderText);
    }
}
