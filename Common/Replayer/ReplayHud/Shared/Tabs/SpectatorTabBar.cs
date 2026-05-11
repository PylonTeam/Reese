using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.Tabs;

internal sealed class SpectatorTabBar : UIPanel
{
    private readonly List<SpectatorTabButton> buttons = [];

    public SpectatorTabBar()
    {
        SetPadding(0f);
        BackgroundColor = new Color(20, 20, 60) * 0.85f;
        BorderColor = Color.Black;
    }

    public void BuildTabs(IReadOnlyList<ITab> tabs, Func<ITab> getCurrentTab, Action<SpectatorTab> onTabSelected, float scale = 1f)
    {
        RemoveAllChildren();
        buttons.Clear();

        if (tabs == null || tabs.Count == 0)
            return;

        for (int i = 0; i < tabs.Count; i++)
        {
            ITab capturedTab = tabs[i];
            SpectatorTabButton button = new(
                capturedTab.HeaderText,
                capturedTab.TooltipText,
                capturedTab.Icon,
                capturedTab.IconScale,
                capturedTab.IconOffset,
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
        foreach (SpectatorTabButton button in buttons)
            button.Recalculate();
    }
}
