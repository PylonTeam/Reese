using Microsoft.Xna.Framework.Graphics;
using Reese.Common.GhostSpectate.UI.Tabs.World.WorldSections;
using Reese.Common.ReplaySpectate.UI.Tabs;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.ReplaySpectate.UI.Tabs.World;

internal sealed class SpectatorWorldTab : UIElement, ISpectatorTab
{
    private readonly WorldSectionBase[] sections =
    [
        new WorldSettingsSection(),
        new WorldInfoSection(),
        new WorldBossInfoSection()
    ];

    public SpectatorTab Tab => SpectatorTab.World;
    public string HeaderText => "World";
    public string TooltipText => "World stats";
    public Asset<Texture2D> Icon => Ass.Icon_World;

    public SpectatorWorldTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);
    }

    public void Refresh() => Build();

    private void Build()
    {
        RemoveAllChildren();

        UIScrollbar scrollbar = new();
        scrollbar.Left.Set(-22f, 1f);
        scrollbar.Top.Set(14f, 0f);
        scrollbar.Height.Set(-66f, 1f);
        Append(scrollbar);

        UIList sectionList = new()
        {
            ListPadding = 12f,
            ManualSortMethod = _ => { }
        };

        sectionList.Top.Set(10f, 0f);
        sectionList.Left.Set(6f, 0f);
        sectionList.Width.Set(-30f, 1f);
        sectionList.Height.Set(-30f, 1f);
        sectionList.SetScrollbar(scrollbar);
        Append(sectionList);

        foreach (WorldSectionBase section in sections)
            sectionList.Add(new UIWorldSectionElement(section));

        sectionList.Recalculate();
        Recalculate();
    }
}
