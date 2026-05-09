using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria.GameContent;
using Terraria.ModLoader.UI;

namespace Reese.Common.GhostSpectate.Drawers;

public static class StatDrawer
{
    public static void DrawPlayerStat(SpriteBatch spriteBatch, Rectangle area, in PlayerStatSnapshot stat, float scale = 1f)
    {
        DrawStat(spriteBatch, area, stat.Icon.Value, stat.IconFrame, stat.Text, scale);
    }

    internal static void DrawNPCStat(SpriteBatch spriteBatch, Rectangle area, in NPCStatSnapshot stat, float scale = 1f)
    {
        DrawStat(spriteBatch, area, stat.Icon.Value, stat.IconFrame, stat.Text, scale);
    }

    #region Main menu drawing
    public static void DrawPlayerNameStatInMainMenu(SpriteBatch spriteBatch, Rectangle area, string playerName, float scale = 1f)
    {
        PlayerStatSnapshot stat = PlayerStats.BuildMainMenuPlayerNameStat(playerName);
        DrawPlayerStat(spriteBatch, area, stat, scale);
        DrawMainMenuTooltip(area, stat.HoverText);
    }

    public static void DrawWorldNameStatInMainMenu(SpriteBatch spriteBatch, Rectangle area, string worldName, float scale = 1f)
    {
        PlayerStatSnapshot stat = PlayerStats.BuildMainMenuWorldNameStat(worldName);
        DrawPlayerStat(spriteBatch, area, stat, scale);
        DrawMainMenuTooltip(area, stat.HoverText);
    }

    private static void DrawMainMenuTooltip(Rectangle area, string hoverText)
    {
        if (!area.Contains(Main.MouseScreen.ToPoint()))
            return;

        Main.LocalPlayer.mouseInterface = true;
        UICommon.TooltipMouseText(hoverText);
    }
    #endregion

    #region Shared draw helpers
    public static void DrawBack(SpriteBatch spriteBatch, Rectangle area, float scale = 1f)
    {
        Color color = Color.White * 0.7f;

        Asset<Texture2D> texture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
        int slice = Math.Max(1, (int)MathF.Round(8f * scale));

        spriteBatch.Draw(texture.Value, new Rectangle(area.X, area.Y, slice, area.Height), new Rectangle(0, 0, 8, texture.Height()), color);
        spriteBatch.Draw(texture.Value, new Rectangle(area.X + slice, area.Y, Math.Max(0, area.Width - slice * 2), area.Height), new Rectangle(8, 0, 8, texture.Height()), color);
        spriteBatch.Draw(texture.Value, new Rectangle(area.Right - slice, area.Y, slice, area.Height), new Rectangle(16, 0, 8, texture.Height()), color);
    }

    public static string Truncate(DynamicSpriteFont font, string text, float maxWidth, float scale)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (font.MeasureString(text).X * scale <= maxWidth)
            return text;

        const string ellipsis = "..";

        for (int i = text.Length - 1; i >= 0; i--)
        {
            string candidate = text[..i] + ellipsis;

            if (font.MeasureString(candidate).X * scale <= maxWidth)
                return candidate;
        }

        return ellipsis;
    }

    private static void DrawStat(SpriteBatch spriteBatch, Rectangle area, Texture2D texture, Rectangle? frame, string text, float scale=1f)
    {
        DrawBack(spriteBatch, area, scale);

        int iconPaddingX = (int)MathF.Round(5f * scale);
        int iconPaddingY = (int)MathF.Round(4f * scale);
        int iconSize = Math.Max(1, (int)MathF.Round(18f * scale));

        Rectangle iconArea = new(area.X + iconPaddingX, area.Y + iconPaddingY, iconSize, iconSize);
        Rectangle source = frame ?? texture.Bounds;

        if (source.Width > 0 && source.Height > 0)
        {
            float iconScale = Math.Min(iconArea.Width / (float)source.Width, iconArea.Height / (float)source.Height);
            int width = Math.Max(1, (int)Math.Round(source.Width * iconScale));
            int height = Math.Max(1, (int)Math.Round(source.Height * iconScale));

            spriteBatch.Draw(texture, new Rectangle(iconArea.X, iconArea.Y + (iconArea.Height - height) / 2, width, height), source, Color.White);
        }

        float textScale = 0.9f * scale;
        int textLeft = area.X + (int)MathF.Round(28f * scale);
        int textTop = area.Y + (int)MathF.Round(3f * scale);
        Rectangle textArea = new(textLeft, textTop, area.Right - textLeft - (int)MathF.Round(4f * scale), area.Height);

        string truncatedText = Truncate(FontAssets.MouseText.Value, text, textArea.Width, textScale);

        Utils.DrawBorderString(spriteBatch, truncatedText, new Vector2(textArea.X, textArea.Y), Color.White, textScale);

        // Show tooltip if text is truncated
        if (truncatedText != text && area.Contains(Main.mouseX, Main.mouseY))
        {
            UICommon.TooltipMouseText(text);
        }
    }

    public static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, Texture2D texture, string text, string hoverText = null, Color? textColor = null, float scale = 1f, float iconScale = 1f)
    {
        DrawBack(spriteBatch, area, scale);

        int iconPaddingX = (int)MathF.Round(3f * scale);
        int iconPaddingY = (int)MathF.Round(3f * scale);
        int iconBoxSize = Math.Max(1, (int)MathF.Round(22f * scale));
        Rectangle iconArea = new(area.X + iconPaddingX, area.Y + iconPaddingY, iconBoxSize, iconBoxSize);

        if (texture != null)
        {
            Rectangle source = texture.Bounds;
            float drawScale = Math.Min(iconArea.Width / (float)source.Width, iconArea.Height / (float)source.Height) * iconScale;
            int width = Math.Max(1, (int)Math.Round(source.Width * drawScale));
            int height = Math.Max(1, (int)Math.Round(source.Height * drawScale));

            spriteBatch.Draw(texture, new Rectangle(iconArea.X + (iconArea.Width - width) / 2, iconArea.Y + (iconArea.Height - height) / 2, width, height), source, Color.White);
        }

        float textScale = 0.75f * scale;
        int textLeft = area.X + (int)MathF.Round(31f * scale);
        int textTop = area.Y + (int)MathF.Round(5f * scale);
        Rectangle textArea = new(textLeft, textTop, area.Right - textLeft - (int)MathF.Round(4f * scale), area.Height);
        string displayText = Truncate(FontAssets.MouseText.Value, text, textArea.Width, textScale);

        Utils.DrawBorderString(spriteBatch, displayText, new Vector2(textArea.X, textArea.Y), textColor ?? Color.White, textScale);

        if (!string.IsNullOrEmpty(hoverText) && area.Contains(Main.MouseScreen.ToPoint()))
        {
            Main.LocalPlayer.mouseInterface = true;
            UICommon.TooltipMouseText(hoverText);
        }
    }
    #endregion
}
