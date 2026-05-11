using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Core.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.MainMenu.UI;

internal sealed class UISortableTableColumn : UIElement
{
    private const int Edge = 3;
    private const float HeightPixels = 28f;

    private readonly string text;
    private readonly UIText label;

    public UISortableTableColumn(string text, float width)
    {
        this.text = text;

        Width.Set(width, 0f);
        Height.Set(HeightPixels, 0f);

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

    public static UIElement CreateLeftTextCell(string value, float left, float width, string tooltip = null)
    {
        UIElement cell = new()
        {
            Left = { Pixels = left },
            Width = { Pixels = width },
            Height = { Percent = 1f }
        };

        UIText label = new(value, 0.98f)
        {
            Left = { Pixels = 14f },
            VAlign = 0.5f,
            TextOriginX = 0f,
            TextOriginY = 0.5f,
            TextColor = new Color(230, 235, 255),
            IgnoresMouseInteraction = true
        };
        cell.Append(label);

        if (!string.IsNullOrWhiteSpace(tooltip))
        {
            cell.OnUpdate += _ =>
            {
                if (cell.IsMouseHovering)
                    UICommon.TooltipMouseText(tooltip);
            };
        }

        return cell;
    }

    public void SetSortState(bool active, bool ascending)
    {
        //label.SetText(active ? $"{text} {(ascending ? "(ascending)" : "(descending)")}" : text);
        label.SetText(text);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle bounds = GetDimensions().ToRectangle();
        DrawNineSlice(spriteBatch, Ass.ButtonTableColumn.Value, bounds, Color.White);

        if (IsMouseHovering)
            DrawNineSlice(spriteBatch, Ass.ButtonTableColumn_Border.Value, bounds, Color.White);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private static void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture, Rectangle target, Color color)
    {
        int sourceWidth = texture.Width;
        int sourceHeight = texture.Height;
        int middleSourceWidth = sourceWidth - Edge * 2;
        int middleSourceHeight = sourceHeight - Edge * 2;
        int middleTargetWidth = Math.Max(0, target.Width - Edge * 2);
        int middleTargetHeight = Math.Max(0, target.Height - Edge * 2);

        spriteBatch.Draw(texture, new Rectangle(target.X, target.Y, Edge, Edge), new Rectangle(0, 0, Edge, Edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.Right - Edge, target.Y, Edge, Edge), new Rectangle(sourceWidth - Edge, 0, Edge, Edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.X, target.Bottom - Edge, Edge, Edge), new Rectangle(0, sourceHeight - Edge, Edge, Edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.Right - Edge, target.Bottom - Edge, Edge, Edge), new Rectangle(sourceWidth - Edge, sourceHeight - Edge, Edge, Edge), color);

        spriteBatch.Draw(texture, new Rectangle(target.X + Edge, target.Y, middleTargetWidth, Edge), new Rectangle(Edge, 0, middleSourceWidth, Edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.X + Edge, target.Bottom - Edge, middleTargetWidth, Edge), new Rectangle(Edge, sourceHeight - Edge, middleSourceWidth, Edge), color);
        spriteBatch.Draw(texture, new Rectangle(target.X, target.Y + Edge, Edge, middleTargetHeight), new Rectangle(0, Edge, Edge, middleSourceHeight), color);
        spriteBatch.Draw(texture, new Rectangle(target.Right - Edge, target.Y + Edge, Edge, middleTargetHeight), new Rectangle(sourceWidth - Edge, Edge, Edge, middleSourceHeight), color);
        spriteBatch.Draw(texture, new Rectangle(target.X + Edge, target.Y + Edge, middleTargetWidth, middleTargetHeight), new Rectangle(Edge, Edge, middleSourceWidth, middleSourceHeight), color);
    }
}
