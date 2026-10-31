using ReLogic.Content;
using System;
using Terraria.UI;

namespace Reese.Common.Replayer;

internal sealed class PanelVisibilityToggle : UIElement
{
    internal const float SlotSize = 32f;

    private readonly Func<bool> isPanelVisible;
    private readonly Asset<Texture2D> background;
    private readonly Asset<Texture2D> border;

    internal PanelVisibilityToggle(Func<bool> isPanelVisible, Action togglePanelVisible)
    {
        this.isPanelVisible = isPanelVisible;
        background = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel");
        border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder");

        HAlign = 1f;
        Width.Set(SlotSize, 0f);
        Height.Set(SlotSize, 0f);
        OnLeftClick += (_, _) => togglePanelVisible();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        Vector2 center = rect.Center.ToVector2();
        Texture2D panel = background.Value;
        float scale = rect.Width / (float)panel.Width;

        spriteBatch.Draw(panel, center, null, Color.White * (IsMouseHovering ? 1f : 0.85f),
            0f, panel.Size() * 0.5f, scale, SpriteEffects.None, 0f);

        if (IsMouseHovering)
        {
            Texture2D highlight = border.Value;
            spriteBatch.Draw(highlight, center, null, Color.White,
                0f, highlight.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            Main.instance.MouseText(Loc.Get(isPanelVisible()
                ? "ReplayHud.Settings.HidePanel"
                : "ReplayHud.Settings.ShowPanel"));
        }

        Rectangle iconRect = rect;
        iconRect.Inflate(-2, -2);
        spriteBatch.Draw(Ass.IconEye.Value, iconRect, Color.White * (isPanelVisible() ? 1f : 0.45f));
    }
}
