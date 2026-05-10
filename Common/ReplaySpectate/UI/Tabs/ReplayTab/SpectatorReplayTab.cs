using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.UI.Tabs.WorldTab;
using Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.ReplaySpectate.UI.Tabs.ReplayTab;

internal sealed class SpectatorReplayTab : UIElement, ISpectatorTab
{
    private readonly WorldSectionBase[] sections =
    [
        new WorldDrawSettingsSection(),
        new WorldSpectatorSettingsSection(),
        new ReplayInfoSection()
    ];

    public SpectatorTab Tab => SpectatorTab.Replay;
    public string HeaderText => "Replay";
    public string TooltipText => "Replay settings";
    public Asset<Texture2D> Icon => Ass.Icon_CameraSmall;

    public SpectatorReplayTab()
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
