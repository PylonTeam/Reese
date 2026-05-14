using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria.GameContent;

namespace Reese.Core.Debug;

//#if DEBUG
internal static class DebugDrawer
{
    // Structs
    private readonly record struct DebugButton(string Text, string Tooltip, Func<bool> IsEnabled, Action Toggle);
    internal readonly record struct DebugStatGroup(string Header, Color HeaderColor, Func<bool> IsEnabled, string[] Rows);

    // Toggles
    internal static bool ShowDebugRecorderStats { get; private set; } = true;
    internal static bool ShowDebugReplayerStats { get; private set; } = true;
    internal static bool ShowDebugClientNetplayStats { get; private set; } = false;
    internal static bool ShowDebugLocalPlayerStats { get; private set; } = false;
    internal static bool ShowDebugHudUiStats { get; private set; } = false;
    internal static bool ShowDebugEntityStats { get; private set; } = false;
    internal static bool ShowDebugMiscStats { get; private set; } = false;
    internal static bool ShowChat { get; private set; } = true;
    internal static bool ShowRectangles { get; private set; } = true;

    // Content
    private static readonly List<(Rectangle rect, Color color)> Rectangles = [];
    private static readonly List<(string text, Vector2 pos, Color color, float scale)> Texts = [];
    
    internal static void DrawRectangle(Rectangle rect, Color? color = null, bool drawSize = false)
    {
        if (!ShowRectangles)
            return;

        Rectangles.Add((rect, color ?? Color.White));

        if (drawSize)
            DrawText($"{rect.Width}x\n{rect.Height}", new Vector2(rect.X + 2, rect.Y + 2), color ?? Color.White);
    }

    internal static void DrawText(string content, Vector2 position, Color? color = null, float scale = 0.8f)
    {
        Texts.Add((content, position, color ?? Color.White, scale));
    }

    internal static void DrawButtons()
    {
        Texture2D back = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel").Value;
        Texture2D border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder").Value;

        DebugButton[] buttons =
        [
            new("ST1", "Debug Recorder Stats", () => ShowDebugRecorderStats, () => ShowDebugRecorderStats = !ShowDebugRecorderStats),
            new("ST2", "Debug Replayer Stats", () => ShowDebugReplayerStats, () => ShowDebugReplayerStats = !ShowDebugReplayerStats),
            new("ST3", "Debug Netplay", () => ShowDebugClientNetplayStats, () => ShowDebugClientNetplayStats = !ShowDebugClientNetplayStats),
            new("ST4", "Debug Local Player", () => ShowDebugLocalPlayerStats, () => ShowDebugLocalPlayerStats = !ShowDebugLocalPlayerStats),
            new("ST5", "Debug HUD", () => ShowDebugHudUiStats, () => ShowDebugHudUiStats = !ShowDebugHudUiStats),
            new("ST6", "Debug Entities", () => ShowDebugEntityStats, () => ShowDebugEntityStats = !ShowDebugEntityStats),
            new("ST7", "Debug Misc", () => ShowDebugMiscStats, () => ShowDebugMiscStats = !ShowDebugMiscStats),
            new("CH", "Show Chat", () => ShowChat, () => ShowChat = !ShowChat),
            new("RC", "Show Debug Rectangles", () => ShowRectangles, () => ShowRectangles = !ShowRectangles)
        ];

        const int spacing = 6;
        const int startX = 10;
        const int startY = 85;
        const float textScale = 0.68f;

        DrawText("Debug mode enabled!", new Vector2(startX, 64f), Color.Yellow);

        for (int i = 0; i < buttons.Length; i++)
        {
            DebugButton button = buttons[i];
            Rectangle rect = new(startX + i * (back.Width + spacing), startY, back.Width, back.Height);
            bool hovered = rect.Contains(Main.MouseScreen.ToPoint());
            bool selected = button.IsEnabled();

            if (hovered)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(button.Tooltip);
            }

            if (hovered && Main.mouseLeft && Main.mouseLeftRelease)
            {
                button.Toggle();
                Main.mouseLeftRelease = false;
            }

            Vector2 center = rect.Center.ToVector2();
            Color fillColor = selected ? new Color(70, 145, 90) : new Color(145, 70, 70);
            Color borderColor = hovered ? Color.Yellow : selected ? Color.LimeGreen : Color.Black;
            float opacity = selected || hovered ? 1f : 0.82f;

            Main.spriteBatch.Draw(back, center, null, fillColor * opacity, 0f, back.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(border, center, null, borderColor, 0f, border.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            Vector2 size = FontAssets.MouseText.Value.MeasureString(button.Text) * textScale;
            DrawText(button.Text, new Vector2(rect.Center.X - size.X * 0.5f, rect.Center.Y - size.Y * 0.5f), Color.White, textScale);
        }
    }

    internal static void DrawDebugInfo()
    {
        Vector2 origin = new(10f, 122f);
        float nextY = origin.Y;

        foreach (DebugStatGroup group in DebugDrawerStats.BuildStats())
        {
            if (!group.IsEnabled())
                continue;

            DrawStatGroup(group, new Vector2(origin.X, nextY));
            nextY += GetStatGroupHeight(group) + 14f;
        }
    }

    internal static void Flush(SpriteBatch sb)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        foreach ((Rectangle rect, Color color) in Rectangles)
        {
            if (ShowRectangles)
                DrawRectangle(sb, pixel, rect, color);
        }

        foreach ((string text, Vector2 pos, Color color, float scale) in Texts)
            Utils.DrawBorderString(sb, text, pos, color, scale);

        Rectangles.Clear();
        Texts.Clear();
    }

    private static void DrawRectangle(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color)
    {
        sb.Draw(pixel, rect, color * 0.3f);

        Color black = Color.Black;
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), black);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), black);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), black);
        sb.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), black);
    }
    private static void DrawStatGroup(DebugStatGroup group, Vector2 origin)
    {
        const float headerScale = 1.0f;
        const float rowScale = 0.8f;
        const float headerGap = 22f;
        const float rowStep = 15f;
        const float columnGap = 8f;

        DrawText(group.Header, origin, group.HeaderColor, headerScale);

        float labelColumnWidth = GetLabelColumnWidth(group.Rows, rowScale);
        float valueX = origin.X + labelColumnWidth + columnGap;

        for (int i = 0; i < group.Rows.Length; i++)
        {
            string row = group.Rows[i];
            string label = GetStatLabel(row);
            string value = GetStatValue(row);

            Vector2 rowPosition = origin + new Vector2(0f, headerGap + i * rowStep);

            if (string.IsNullOrEmpty(value))
            {
                DrawText(row, rowPosition, Color.White, rowScale);
                continue;
            }

            DrawText(label, rowPosition, Color.LightGray, rowScale);
            DrawText(value, new Vector2(valueX, rowPosition.Y), Color.White, rowScale);
        }
    }

    private static float GetLabelColumnWidth(string[] rows, float scale)
    {
        float width = 0f;

        foreach (string row in rows)
        {
            string label = GetStatLabel(row);

            if (string.IsNullOrEmpty(label))
                continue;

            width = Math.Max(width, FontAssets.MouseText.Value.MeasureString(label).X * scale);
        }

        return width;
    }

    private static string GetStatLabel(string row)
    {
        if (string.IsNullOrEmpty(row))
            return string.Empty;

        int separator = row.IndexOf(':');

        if (separator < 0)
            return string.Empty;

        return row[..(separator + 1)];
    }

    private static string GetStatValue(string row)
    {
        if (string.IsNullOrEmpty(row))
            return string.Empty;

        int separator = row.IndexOf(':');

        if (separator < 0 || separator >= row.Length - 1)
            return string.Empty;

        return row[(separator + 1)..].TrimStart();
    }

    private static float GetStatGroupHeight(DebugStatGroup group)
    {
        const float headerGap = 19f;
        const float rowStep = 15f;

        return headerGap + group.Rows.Length * rowStep;
    }

}
//#endif