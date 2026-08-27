using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replay.Hud.Shared.Sections;
using Reese.Common.Replay.Hud.Shared.Tabs;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

internal abstract class TabPage : UIElement, ITab
{
    protected UIList List { get; private set; }

    public abstract SpectatorTab Tab { get; }
    public abstract string HeaderText { get; }
    public abstract string TooltipText { get; }
    public abstract Asset<Texture2D> Icon { get; }
    public abstract float IconScale { get; }
    public virtual Vector2 IconOffset => Vector2.Zero;
    public virtual Vector2 TextOffset => Vector2.Zero;

    protected virtual float ScrollbarLeft => -22f;
    protected virtual float ScrollbarTop => 14f;
    protected virtual float ScrollbarHeight => -66f;
    protected virtual float ListTop => 10f;
    protected virtual float ListLeft => 6f;
    protected virtual float ListWidth => -30f;
    protected virtual float ListHeight => -30f;
    protected virtual float ListPadding => 12f;

    protected TabPage()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);
    }

    public virtual void Refresh()
    {
        Build();
    }

    protected abstract void Populate(UIList list);

    protected void AddSection(UIList list, SpectatorSectionBase section)
    {
        list.Add(new UISpectatorSectionElement(section));
    }

    private void Build()
    {
        RemoveAllChildren();

        UIScrollbar scrollbar = new();
        scrollbar.Left.Set(ScrollbarLeft, 1f);
        scrollbar.Top.Set(ScrollbarTop, 0f);
        scrollbar.Height.Set(ScrollbarHeight, 1f);
        Append(scrollbar);

        List = new UIList
        {
            ListPadding = ListPadding,
            ManualSortMethod = _ => { }
        };

        List.Top.Set(ListTop, 0f);
        List.Left.Set(ListLeft, 0f);
        List.Width.Set(ListWidth, 1f);
        List.Height.Set(ListHeight, 1f);
        List.SetScrollbar(scrollbar);
        Append(List);

        Populate(List);

        List.Recalculate();
        Recalculate();
    }
}
