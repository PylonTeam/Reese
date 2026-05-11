using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.Tabs;

internal sealed class TabButton : UIPanel
{
    private readonly Func<bool> isSelected;
    private readonly string hoverText;

    public TabButton(
        string headerText,
        string tooltipText,
        Asset<Texture2D> icon,
        float iconScale,
        Vector2 iconOffset,
        Func<bool> isSelected,
        Action onClick,
        float scale)
    {
        this.isSelected = isSelected;
        hoverText = tooltipText;

        Height.Set(0f, 1f);
        VAlign = 0.5f;
        SetPadding(0f);

        OnLeftClick += (_, _) => onClick();

        float iconSize = 20f * scale;
        float iconLeft = 6f * scale + iconOffset.X * scale;
        float iconTop = -5f * scale + iconOffset.Y * scale;

        Append(new UIImage(icon.Value)
        {
            Left = new StyleDimension(iconLeft, 0f),
            Top = new StyleDimension(iconTop, 0f),
            VAlign = 0.5f,
            Width = new StyleDimension(iconSize, 0f),
            Height = new StyleDimension(iconSize, 0f),
            ImageScale = iconScale * scale
        });

        Append(new UIText(headerText, textScale: 0.85f * scale)
        {
            Left = new StyleDimension(38f * scale, 0f),
            VAlign = 0.5f
        });
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        BackgroundColor = isSelected() ? new Color(83, 97, 168) : new Color(63, 82, 151) * 0.85f;
        BorderColor = IsMouseHovering ? Color.Yellow : isSelected() ? Color.White : Color.Black;

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(hoverText);
        }
    }
}
