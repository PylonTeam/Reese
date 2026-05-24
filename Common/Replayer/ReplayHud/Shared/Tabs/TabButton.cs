using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.Tabs;

internal sealed class TabButton : UIPanel
{
    private readonly Func<bool> isSelected;
    private readonly string hoverText;
    private readonly UIText label;
    private readonly UIElement content;
    private readonly Texture2D iconTexture;
    private readonly float iconScale;
    private readonly Vector2 textOffset;
    private readonly float scale;

    public TabButton(
        string headerText,
        string tooltipText,
        Asset<Texture2D> icon,
        float iconScale,
        Vector2 iconOffset,
        Vector2 textOffset,
        Func<bool> isSelected,
        Action onClick,
        float scale)
    {
        this.isSelected = isSelected;
        this.iconScale = iconScale;
        this.textOffset = textOffset;
        this.scale = scale;
        hoverText = tooltipText;
        iconTexture = icon.Value;

        Height.Set(0f, 1f);
        VAlign = 0.5f;
        SetPadding(0f);

        OnLeftClick += (_, _) => onClick();

        float textScale = 0.85f * scale;
        float gap = 8f * scale;
        float iconDrawWidth = iconTexture.Width * iconScale * scale;
        float iconDrawHeight = iconTexture.Height * iconScale * scale;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(headerText) * textScale;

        content = new UIElement
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            Width = new StyleDimension(iconDrawWidth + gap + textSize.X, 0f),
            Height = new StyleDimension(Math.Max(iconDrawHeight, textSize.Y), 0f)
        };

        Append(content);

        content.Append(new UIImage(iconTexture)
        {
            Left = new StyleDimension(iconOffset.X * scale, 0f),
            Top = new StyleDimension(iconOffset.Y * scale, 0f),
            VAlign = 0.5f,
            Width = new StyleDimension(iconDrawWidth, 0f),
            Height = new StyleDimension(iconDrawHeight, 0f),
            ImageScale = iconScale * scale
        });

        label = new UIText(headerText, textScale: textScale)
        {
            Left = new StyleDimension(iconDrawWidth + gap + textOffset.X * scale, 0f),
            Top = new StyleDimension(textOffset.Y * scale, 0f),
            VAlign = 0.5f
        };

        content.Append(label);
    }

    public void SetHeaderText(string text)
    {
        label.SetText(text);

        float textScale = 0.85f * scale;
        float gap = 8f * scale;
        float iconDrawWidth = iconTexture.Width * iconScale * scale;
        float iconDrawHeight = iconTexture.Height * iconScale * scale;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * textScale;

        content.Width.Set(iconDrawWidth + gap + textSize.X + Math.Abs(textOffset.X) * scale, 0f);
        content.Height.Set(Math.Max(iconDrawHeight, textSize.Y), 0f);
        content.Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool selected = isSelected();

        BackgroundColor = selected ? new Color(47, 61, 125) : new Color(31, 43, 95) * 0.92f;
        BorderColor = IsMouseHovering ? Color.Yellow : selected ? Color.White : Color.Black;

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(hoverText);
        }
    }
}
