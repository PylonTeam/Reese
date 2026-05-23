using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

/// <summary>
/// Shows spectate status info such as 
/// "spectating player/NPC" or 
/// "ghost mode enabled"
/// </summary>
internal sealed class UIStatusPanel : UIPanel
{
    private string lastText = "";
    private bool lastGhost;
    private float lastWidth = -1f;
    private readonly float scale;

    public UIStatusPanel(float scale)
    {
        this.scale = scale;
        Width.Set(0, 1f);
        Height.Set(44f * scale, 0f);
        SetPadding(3f * scale);
        //BackgroundColor = new Color(35, 54, 96) * 0.85f;
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
        BorderColor = Color.Black;
    }

    public void SetStatus(string text, bool showGhost)
    {
        float width = GetDimensions().Width;

        if (text == lastText && showGhost == lastGhost && Math.Abs(width - lastWidth) < 0.5f)
            return;

        lastText = text;
        lastGhost = showGhost;
        lastWidth = width;

        RemoveAllChildren();

        float height = GetDimensions().Height - (12f * scale);
        float textScale = 1.4f * scale;
        float maxWidth = GetDimensions().Width - (12f * scale);

        if (showGhost)
        {
            float iconSize = height, gap = 4f * scale;
            float fittedScale = Fit(text, textScale, maxWidth - iconSize - gap, height);
            float textWidth = FontAssets.MouseText.Value.MeasureString(text).X * fittedScale;

            // Container to keep icon + text centered as one unit
            UIElement container = new()
            {
                Width = { Pixels = iconSize + gap + textWidth },
                Height = { Percent = 1f },
                HAlign = 0.5f
            };
            container.Append(new GhostIcon(iconSize) { VAlign = 0.5f });
            container.Append(new UIText(text, fittedScale) { Left = { Pixels = iconSize + gap }, VAlign = 0.5f });
            Append(container);
        }
        else
        {
            Append(new UIText(text, Fit(text, textScale, maxWidth, height)) { HAlign = 0.5f, VAlign = 0.5f });
        }
    }

    private float Fit(string text, float baseScale, float maxWidth, float maxHeight)
    {
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text);
        return string.IsNullOrEmpty(text) ? baseScale : Math.Min(baseScale, Math.Min(maxWidth / textSize.X, maxHeight / textSize.Y));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        var rect = GetDimensions().ToRectangle();
        //DebugDrawer.DrawRectangle(rect, drawSize: true);
    }

    private class GhostIcon : UIElement
    {
        public GhostIcon(float size)
        {
            Width.Set(size, 0);
            Height.Set(size, 0);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!TextureAssets.Ghost.IsLoaded) return;

            var texture = TextureAssets.Ghost.Value;
            var dimensions = GetDimensions();

            // Get the first image in the ghost spritesheet (there are 4 in total)
            Rectangle sourceRect = new Rectangle(0, 0, texture.Width, texture.Height / 4);

            float widthRatio = dimensions.Width / sourceRect.Width;
            float heightRatio = dimensions.Height / sourceRect.Height;
            float uniformScale = Math.Min(widthRatio, heightRatio);

            int targetWidth = (int)(sourceRect.Width * uniformScale);
            int targetHeight = (int)(sourceRect.Height * uniformScale);

            // Center
            Rectangle destinationRect = new Rectangle(
                (int)(dimensions.X + (dimensions.Width - targetWidth) / 2),
                (int)(dimensions.Y + (dimensions.Height - targetHeight) / 2),
                targetWidth,
                targetHeight
            );

            //DebugDrawer.DrawRectangle(destinationRect, drawSize: true);

            SpriteEffects spriteDirection = Main.LocalPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(texture, destinationRect, sourceRect, Color.White * 0.9f, 0f, Vector2.Zero, spriteDirection, 0f);
        }
    }
}
