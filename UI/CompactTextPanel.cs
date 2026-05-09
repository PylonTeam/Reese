using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.UI;

public class CompactTextPanel<T> : UIPanel
{
    private T text;
    private float textScale = 1f;
    private bool isLarge;
    private Color textColor = Color.White;
    private Vector2 textSize;
    private bool drawPanel = true;
    private string asterisks;
    private readonly bool highlightOnHover;

    public float TextHAlign { get; set; } = 0.5f;
    public float TextVAlign { get; set; } = 0.5f;
    public Vector2 TextOffset { get; set; } = Vector2.Zero;
    public bool HideContents { get; set; }

    public T Value => text;
    public float TextScale => textScale;
    public Vector2 TextSize => textSize;

    public bool DrawPanel
    {
        get => drawPanel;
        set => drawPanel = value;
    }

    public string Text => text?.ToString() ?? string.Empty;

    public Color TextColor
    {
        get => textColor;
        set => textColor = value;
    }

    public CompactTextPanel(
        T text,
        float textScale = 1f,
        bool large = false,
        Action leftClick = null,
        Action rightClick = null,
        bool highlightOnHover = true,
        Color? backgroundColor = null,
        float padding = 4f,
        float textHAlign = 0.5f,
        float textVAlign = 0.5f,
        Vector2? textOffset = null)
    {
        this.highlightOnHover = highlightOnHover;

        SetPadding(padding);
        BackgroundColor = backgroundColor ?? new Color(44, 57, 105);
        TextHAlign = textHAlign;
        TextVAlign = textVAlign;
        TextOffset = textOffset ?? Vector2.Zero;

        SetText(text, textScale, large);

        if (highlightOnHover)
        {
            OnMouseOver += HighlightOn;
            OnMouseOut += HighlightOff;
        }

        if (leftClick != null)
            OnLeftClick += (_, _) => leftClick();

        if (rightClick != null)
            OnRightClick += (_, _) => rightClick();
    }

    public override void Recalculate()
    {
        UpdateTextMetrics();
        base.Recalculate();
    }

    public void SetText(T text)
    {
        SetText(text, textScale, isLarge);
    }

    public virtual void SetText(T text, float textScale, bool large)
    {
        this.text = text;
        this.textScale = textScale;
        isLarge = large;
        UpdateTextMetrics();
    }

    public CompactTextPanel<T> WithFullWidth(float height, float top = 0f)
    {
        Width.Set(0f, 1f);
        Height.Set(height, 0f);
        Top.Set(top, 0f);
        return this;
    }

    private void UpdateTextMetrics()
    {
        DynamicSpriteFont font = isLarge ? FontAssets.DeathText.Value : FontAssets.MouseText.Value;
        string value = Text;

        textSize = Terraria.UI.Chat.ChatManager.GetStringSize(font, value, new Vector2(textScale));
        textSize.Y = (isLarge ? 32f : 16f) * textScale;

        MinWidth.Set(textSize.X + PaddingLeft + PaddingRight, 0f);
        //MinHeight.Set(0f, 0f);
        //MinHeight.Set(textSize.Y + PaddingTop + PaddingBottom, 0f);
        MinHeight.Set(30f, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (drawPanel)
            base.DrawSelf(spriteBatch);

        DrawText(spriteBatch);
    }

    private void DrawText(SpriteBatch spriteBatch)
    {
        CalculatedStyle innerDimensions = GetInnerDimensions();
        Vector2 position = innerDimensions.Position();

        position.X += (innerDimensions.Width - textSize.X) * TextHAlign;
        position.Y += (innerDimensions.Height - textSize.Y) * TextVAlign;
        position += TextOffset;

        string value = Text;
        if (HideContents)
        {
            if (asterisks == null || asterisks.Length != value.Length)
                asterisks = new string('*', value.Length);

            value = asterisks;
        }

        if (isLarge)
            Utils.DrawBorderStringBig(spriteBatch, value, position, textColor, textScale);
        else
            Utils.DrawBorderString(spriteBatch, value, position, textColor, textScale);
    }

    private static void HighlightOn(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is UIPanel panel)
            panel.BorderColor = Color.Yellow;
    }

    private static void HighlightOff(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is UIPanel panel)
            panel.BorderColor = Color.Black;
    }
}