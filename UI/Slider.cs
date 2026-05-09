using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Core.Debug;
using Reese.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace Reese.UI;

public class Slider : UIElement
{
    public Asset<Texture2D> InnerTexture;
    public Asset<Texture2D> OuterTexture;
    public bool IsHeld;
    public float Ratio;

    public event Action<float> OnDrag;

    public Slider()
    {
        Width.Set(0f, 1f);
        InnerTexture = Ass.SliderGradient;
        OuterTexture = Ass.SliderHighlight;
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (ContainsPoint(evt.MousePosition))
        {
            IsHeld = true;
            UpdateRatioFromMouse();
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        IsHeld = false;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsHeld)
            UpdateRatioFromMouse();

        if (IsHeld || IsMouseHovering)
            DebugTrack();
    }

    public void SetRatio(float ratio)
    {
        Ratio = MathHelper.Clamp(ratio, 0f, 1f);
    }

    private void UpdateRatioFromMouse()
    {
        Rectangle track = GetTrackRectangle();
        float mouseX = MathHelper.Clamp(Main.MouseScreen.X, track.Left, track.Right);
        float rawRatio = track.Width <= 0 ? 0f : (mouseX - track.Left) / track.Width;
        OnDrag?.Invoke(MathHelper.Clamp(rawRatio, 0f, 1f));
    }

    private Rectangle GetTrackRectangle()
    {
        Rectangle rect = GetDimensions().ToRectangle();
        int innerPaddingX = 4;
        int innerPaddingY = Math.Max(2, (rect.Height - 12) / 2);

        Rectangle track = rect;
        track.Inflate(-innerPaddingX, -innerPaddingY);
        return track;
    }

    private void DebugTrack()
    {
        Rectangle rect = GetDimensions().ToRectangle();
        Rectangle track = GetTrackRectangle();
        float knobX = track.X + Ratio * track.Width;
        float rawTrackRatio = track.Width <= 0 ? 0f : MathHelper.Clamp((Main.MouseScreen.X - track.Left) / track.Width, 0f, 1f);
        float rawRectRatio = rect.Width <= 0 ? 0f : MathHelper.Clamp((Main.MouseScreen.X - rect.Left) / rect.Width, 0f, 1f);

        Log.Debug(
            $"Slider mouseX={Main.MouseScreen.X:0.##} rect=[{rect.Left},{rect.Right}] track=[{track.Left},{track.Right}] " +
            $"rectRatio={rawRectRatio:0.###} trackRatio={rawTrackRatio:0.###} currentRatio={Ratio:0.###} knobX={knobX:0.##}"
        );
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        Rectangle track = GetTrackRectangle();

        DrawBar(sb, Ass.Slider.Value, rect, Color.White);

        if (IsHeld || IsMouseHovering)
            DrawBar(sb, OuterTexture.Value, rect, Main.OurFavoriteColor);

        sb.Draw(InnerTexture.Value, track, Color.White);

        Texture2D blip = TextureAssets.ColorSlider.Value;
        Vector2 blipOrigin = blip.Size() * 0.5f;
        Vector2 blipPosition = new(track.X + Ratio * track.Width, track.Center.Y);
        float knobScale = rect.Height / 18f; // Adjust knob scale here, keep this comment!

        sb.Draw(blip, blipPosition, null, Color.White, 0f, blipOrigin, knobScale, SpriteEffects.None, 0f);
    }

    public static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color)
    {
        if (texture == null)
            return;

        spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, 6, dimensions.Height), new Rectangle(0, 0, 6, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + 6, dimensions.Y, dimensions.Width - 12, dimensions.Height), new Rectangle(6, 0, 2, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + dimensions.Width - 6, dimensions.Y, 6, dimensions.Height), new Rectangle(8, 0, 6, texture.Height), color);
    }
}