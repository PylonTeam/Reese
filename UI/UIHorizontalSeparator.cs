using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.UI;

namespace Reese.UI;

public class UIHorizontalSeparator : UIElement
{
    public Asset<Texture2D> _texture;
    public Color Color;
    public int EdgeWidth;

    public UIHorizontalSeparator(int EdgeWidth = 2, bool highlightSideUp = true)
    {
        this.EdgeWidth = EdgeWidth;
        Color = Color.White;
        _texture = Main.Assets.Request<Texture2D>(highlightSideUp
            ? "Images/UI/CharCreation/Separator1"
            : "Images/UI/CharCreation/Separator2");

        Width.Set(_texture.Width(), 0f);
        Height.Set(_texture.Height(), 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dimensions = GetDimensions();
        Utils.DrawPanel(_texture.Value, EdgeWidth, 0, spriteBatch, dimensions.Position(), dimensions.Width, Color);
    }

    public override bool ContainsPoint(Vector2 point)
    {
        return false;
    }
}

public sealed class UIVerticalSeparator : UIHorizontalSeparator
{
    public UIVerticalSeparator(float left, float height, int edgeWidth = 2, bool highlightSideUp = true)
        : base(edgeWidth, highlightSideUp)
    {
        Left.Set(left, 0f);
        Width.Set(edgeWidth, 0f);
        Height.Set(height, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dimensions = GetDimensions();
        Texture2D texture = _texture.Value;
        Rectangle source = new(texture.Width / 2, 0, Math.Min(EdgeWidth, texture.Width), texture.Height);
        spriteBatch.Draw(texture, dimensions.ToRectangle(), source, Color);
    }
}
