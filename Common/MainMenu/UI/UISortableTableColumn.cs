using System;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.MainMenu.UI;

internal sealed class UISortableTableColumn : UIElement
{
    private enum SortDirection
    {
        None,
        Ascending,
        Descending
    }

    private readonly string text;
    private readonly UIText label;

    private SortDirection sortDirection;

    public UISortableTableColumn(string text, float width)
    {
        this.text = text;

        Width.Set(width, 0f);
        Height.Set(28f, 0f);

        label = new UIText(text, 0.85f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            IgnoresMouseInteraction = true
        };
        Append(label);
    }

    public static UISortableTableColumn AppendHeader(UIElement parent, string text, float left, float width)
    {
        UISortableTableColumn column = new(text, width)
        {
            Left = { Pixels = left }
        };
        parent.Append(column);
        return column;
    }

    public void SetSortState(bool active, bool ascending)
    {
        sortDirection = !active ? SortDirection.None : ascending ? SortDirection.Ascending : SortDirection.Descending;
        label.SetText(text);
        
        if (label.Text == "Length")
        {
            label.Left.Set(active ? -6f : 0f, 0f);
            label.Recalculate();
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle bounds = GetDimensions().ToRectangle();

        DrawNineSlice(spriteBatch, Ass.ButtonTableColumn.Value, bounds, Color.White);

        if (sortDirection != SortDirection.None)
            DrawNineSlice(spriteBatch, Ass.ButtonTableColumn_Selected.Value, bounds, Color.White);
        else if (IsMouseHovering)
            DrawNineSlice(spriteBatch, Ass.ButtonTableColumn_Border.Value, bounds, Color.White);

        if (sortDirection != SortDirection.None)
            DrawSortArrow(spriteBatch, bounds);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private void DrawSortArrow(SpriteBatch spriteBatch, Rectangle bounds)
    {
        Texture2D texture = (sortDirection == SortDirection.Ascending ? Ass.Icon_ArrowUp : Ass.Icon_ArrowDown).Value;

        const int arrowRightPadding = 12;
        const int arrowMaxSize = 12;

        float scale = Math.Min(1f, Math.Min((float)arrowMaxSize / texture.Width, (float)arrowMaxSize / texture.Height));
        int width = Math.Max(1, (int)Math.Round(texture.Width * scale));
        int height = Math.Max(1, (int)Math.Round(texture.Height * scale));

        Rectangle target = new(
            bounds.Right - arrowRightPadding - width,
            bounds.Y + (bounds.Height - height) / 2,
            width,
            height
        );

        spriteBatch.Draw(texture, target, Color.White);
    }

    private static void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture, Rectangle target, Color color)
    {
        const int edge = 3;

        int sourceWidth = texture.Width;
        int sourceHeight = texture.Height;
        int middleSourceWidth = sourceWidth - edge * 2;
        int middleSourceHeight = sourceHeight - edge * 2;
        int middleTargetWidth = Math.Max(0, target.Width - edge * 2);
        int middleTargetHeight = Math.Max(0, target.Height - edge * 2);

        spriteBatch.Draw(texture, new Rectangle(target.X, target.Y, edge, edge), new Rectangle(0, 0, edge, edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.Right - edge, target.Y, edge, edge), new Rectangle(sourceWidth - edge, 0, edge, edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.X, target.Bottom - edge, edge, edge), new Rectangle(0, sourceHeight - edge, edge, edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.Right - edge, target.Bottom - edge, edge, edge), new Rectangle(sourceWidth - edge, sourceHeight - edge, edge, edge), color);

        spriteBatch.Draw(texture, new Rectangle(target.X + edge, target.Y, middleTargetWidth, edge), new Rectangle(edge, 0, middleSourceWidth, edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.X + edge, target.Bottom - edge, middleTargetWidth, edge), new Rectangle(edge, sourceHeight - edge, middleSourceWidth, edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.X, target.Y + edge, edge, middleTargetHeight), new Rectangle(0, edge, edge, middleSourceHeight), color);
        spriteBatch.Draw(texture, new Rectangle(target.Right - edge, target.Y + edge, edge, middleTargetHeight), new Rectangle(sourceWidth - edge, edge, edge, middleSourceHeight), color);
        spriteBatch.Draw(texture, new Rectangle(target.X + edge, target.Y + edge, middleTargetWidth, middleTargetHeight), new Rectangle(edge, edge, middleSourceWidth, middleSourceHeight), color);
    }
}
