using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.UI;

public class IconActionButton : UIElement
{
    private readonly string hoverText;
    private readonly Asset<Texture2D> iconTexture;
    private readonly Asset<Texture2D> panelTexture;
    private readonly Asset<Texture2D> borderTexture;
    private readonly Asset<Texture2D> highlightTexture;

    private bool selected;
    private bool hovered;

    public IconActionButton(Asset<Texture2D> texture, string hoverText, UIElement.MouseEvent onClick)
    {
        this.hoverText = hoverText;
        iconTexture = texture;

        panelTexture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel", AssetRequestMode.ImmediateLoad);
        borderTexture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder", AssetRequestMode.ImmediateLoad);
        highlightTexture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight", AssetRequestMode.ImmediateLoad);

        Width.Set(88f, 0f);
        Height.Set(panelTexture.Height(), 0f);

        OnLeftClick += onClick;
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
    }

    public float GetOuterWidth() => Width.Pixels;

    public void SetOuterWidth(float width)
    {
        Width.Set(width, 0f);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        hovered = true;
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        hovered = false;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle rect = GetDimensions().ToRectangle();

        DrawSliced(spriteBatch, panelTexture.Value, rect, Color.White * (IsMouseHovering ? 1f : 0.65f));

        if (hovered)
            DrawSliced(spriteBatch, borderTexture.Value, rect, Color.White);

        if (selected)
            DrawSliced(spriteBatch, highlightTexture.Value, rect, Color.White);

        Texture2D icon = iconTexture.Value;
        Vector2 iconPosition = rect.Center.ToVector2();
        Vector2 iconOrigin = icon.Bounds.Size() * 0.5f;

        spriteBatch.Draw(icon, iconPosition, null, Color.White, 0f, iconOrigin, 1f, SpriteEffects.None, 0f);

        if (IsMouseHovering)
            Main.instance.MouseText(hoverText);
    }

    private static void DrawSliced(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, Color color)
    {
        int capWidth = texture.Width / 3;
        int middleWidth = texture.Width - capWidth * 2;

        Rectangle srcLeft = new(0, 0, capWidth, texture.Height);
        Rectangle srcMiddle = new(capWidth, 0, middleWidth, texture.Height);
        Rectangle srcRight = new(capWidth + middleWidth, 0, capWidth, texture.Height);

        Rectangle dstLeft = new(destination.X, destination.Y, capWidth, destination.Height);
        Rectangle dstMiddle = new(destination.X + capWidth, destination.Y, destination.Width - capWidth * 2, destination.Height);
        Rectangle dstRight = new(destination.Right - capWidth, destination.Y, capWidth, destination.Height);

        spriteBatch.Draw(texture, dstLeft, srcLeft, color);
        spriteBatch.Draw(texture, dstMiddle, srcMiddle, color);
        spriteBatch.Draw(texture, dstRight, srcRight, color);
    }
}