using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using System;
using System.Globalization;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

/// <summary>
/// Compact card (half the height of UIPlayerCard) used by PlayerHudMode.Head.
//// Layout: player head on the left, name + distance stacked to its right.
////
////  ┌──────────────────────────────┐
////  │  ╔══╗  PlayerName            │  ← 65 px total
////  │  ╚══╝    (123 ft)            │ <- LMAO this is kinda sick
////  └──────────────────────────────┘
/// </summary>
internal abstract class UIEntityCard<T> : UIPanel where T : Entity
{
    internal static int CardWidth => 150;
    internal static int DetailHeight => 65 * 2;
    internal static int CompactCardHeight => DetailHeight / 2;

    public int EntityIndex { get; }
    public int ListIndex { get; }

    private readonly float scale;

    protected UIEntityCard(int entityIndex, int listIndex, float scale = 1f)
    {
        EntityIndex = entityIndex;
        ListIndex = listIndex;
        this.scale = scale;
        SetPadding(0f);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        bool valid = TryGetEntity(EntityIndex, out T entity);
        bool selected = valid && IsSelected(entity);

        BackgroundColor = selected || !IsMouseHovering ? new Color(20, 27, 62) * 0.95f : new Color(47, 61, 125) * 0.55f;
        BorderColor = selected ? Color.Yellow : IsMouseHovering ? Colors.FancyUIFatButtonMouseOver : Color.Black;

        base.DrawSelf(sb);

        if (!valid)
            return;

        Rectangle rect = GetDimensions().ToRectangle();

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        switch (SpectateHudClientSettings.EntityHudMode)
        {
            case EntityHudMode.Head:
                DrawHead(sb, entity, rect);
                return;

            case EntityHudMode.Detailed:
                DrawDetail(sb, entity, rect);
                return;

            default:
                DrawFull(sb, entity, rect);
                return;
        }
    }

    private void DrawFull(SpriteBatch sb, T entity, Rectangle rect)
    {
        int shrink = (int)MathF.Round(6f * scale);
        int textGap = (int)MathF.Round(1f * scale);
        Rectangle content = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);

        int nameHeight = (int)MathF.Round(24f * scale);
        int distanceHeight = (int)MathF.Round(22f * scale);
        int previewHeight = Math.Max(0, content.Height - nameHeight - distanceHeight - textGap);

        Rectangle preview = new(content.X, content.Y, content.Width, previewHeight);
        EntityDrawer.DrawEntityBackground(sb, preview);
        DrawPreview(sb, entity, preview);

        Color textColor = GetTextColor(entity);
        Rectangle name = new(content.X, preview.Bottom + textGap, content.Width, nameHeight);
        Rectangle distance = new(content.X, name.Bottom, content.Width, distanceHeight);

        DrawCenteredText(sb, Truncate(GetDisplayName(entity), name.Width, 0.95f * scale), name, GetFullNameScale() * scale, textColor);
        DrawCenteredText(sb, GetDistanceText(entity), distance, 0.9f * scale, GetDistanceColor(entity));
    }

    private void DrawHead(SpriteBatch sb, T entity, Rectangle rect)
    {
        int shrink = (int)MathF.Round(6f * scale);
        Rectangle content = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);

        int iconSize = (int)MathF.Round(Math.Min(content.Height, 42f * scale));
        Rectangle icon = new(content.X, content.Y + (content.Height - iconSize) / 2, iconSize, iconSize);
        DrawHeadIcon(sb, entity, icon);

        int gap = (int)MathF.Round(6f * scale);
        int indent = (int)MathF.Round(4f * scale);
        int textLeft = icon.Right + gap;
        int textWidth = Math.Max(0, content.Right - textLeft);

        int nameHeight = (int)MathF.Round(24f * scale);
        int distanceHeight = (int)MathF.Round(20f * scale);
        int lineGap = (int)MathF.Round(2f * scale);
        int blockTop = content.Y + (content.Height - nameHeight - lineGap - distanceHeight) / 2;

        Color textColor = GetTextColor(entity);
        string name = Truncate(GetDisplayName(entity), textWidth, 0.95f * scale);

        Utils.DrawBorderString(sb, name, new Vector2(textLeft, blockTop), textColor, 0.95f * scale);
        Utils.DrawBorderString(sb, GetDistanceText(entity), new Vector2(textLeft + indent, blockTop + nameHeight + lineGap), GetDistanceColor(entity) * 0.75f, 0.82f * scale);
    }

    private void DrawDetail(SpriteBatch sb, T entity, Rectangle rect)
    {
        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(32f * scale);
        int buttonGap = (int)MathF.Round(2f * scale);
        int previewWidth = buttonSize * 2 + buttonGap;
        Rectangle content = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        Rectangle preview = new(content.X, content.Y, previewWidth, content.Height - buttonSize - (int)MathF.Round(3f * scale));
        Rectangle info = new(preview.Right + (int)MathF.Round(6f * scale), content.Y + (int)MathF.Round(6f * scale), content.Right - preview.Right - (int)MathF.Round(14f * scale), content.Height);
        Rectangle name = new(info.X, info.Y - 2, info.Width, (int)MathF.Round(24f * scale));

        EntityDrawer.DrawEntityBackground(sb, preview);
        DrawPreview(sb, entity, preview);
        DrawDetailName(sb, entity, name);

        int statHeight = (int)MathF.Round(27f * scale);
        int statGap = (int)MathF.Round(3f * scale);
        Rectangle stat = new(info.X, name.Bottom + (int)MathF.Round(2f * scale), info.Width, statHeight);

        DrawStats(sb, entity, stat, statGap, scale);
    }

    private void DrawDetailName(SpriteBatch sb, T entity, Rectangle area)
    {
        string text = Truncate(GetDisplayName(entity), area.Width, scale);
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new(area.X, area.Y + (area.Height - size.Y) * 0.5f + 4f);
        Utils.DrawBorderString(sb, text, position, GetTextColor(entity), scale);
    }

    protected static Rectangle NextStat(Rectangle rect, int gap)
    {
        return new Rectangle(rect.X, rect.Bottom + gap, rect.Width, rect.Height);
    }

    internal static string GetDistanceText(Entity entity)
    {
        Player local = Main.LocalPlayer;
        float feet = local?.active == true ? Vector2.Distance(local.Center, entity.Center) / 8f : 0f;
        return $"({feet.ToString("F0", CultureInfo.InvariantCulture)} ft)";
    }

    internal static void DrawCenteredText(SpriteBatch sb, string text, Rectangle area, float scale, Color color)
    {
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f + 3f * scale);
        Utils.DrawBorderString(sb, text, position, color, scale);
    }

    private static string Truncate(string text, int width, float scale)
    {
        return StatDrawer.Truncate(FontAssets.MouseText.Value, text, width, scale);
    }

    protected virtual float GetFullNameScale()
    {
        return 1.1f;
    }

    protected virtual Color GetDistanceColor(T entity)
    {
        return GetTextColor(entity);
    }

    protected abstract bool TryGetEntity(int index, out T entity);
    protected abstract bool IsSelected(T entity);
    protected abstract string GetDisplayName(T entity);
    protected abstract Color GetTextColor(T entity);
    protected abstract void DrawPreview(SpriteBatch sb, T entity, Rectangle area);
    protected abstract void DrawHeadIcon(SpriteBatch sb, T entity, Rectangle area);
    protected abstract void DrawStats(SpriteBatch sb, T entity, Rectangle stat, int statGap, float scale);
}