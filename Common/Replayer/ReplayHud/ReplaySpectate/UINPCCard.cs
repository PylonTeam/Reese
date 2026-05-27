using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using System;
using System.Globalization;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal sealed class UINPCCard : UIPanel
{
    public int NPCIndex { get; }
    public int ListIndex { get; }

    private readonly float scale;

    public UINPCCard(int npcIndex, int listIndex, float scale = 1f)
    {
        NPCIndex = npcIndex;
        ListIndex = listIndex;
        this.scale = scale;

        SetPadding(0f);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        bool selected = IsValidNPC(NPCIndex) && SpectatorTargetSystem.IsLockedTargeting(Main.npc[NPCIndex]);
        BackgroundColor = selected || !IsMouseHovering ? new Color(20, 27, 62) * 0.95f : new Color(47, 61, 125) * 0.55f;
        BorderColor = selected ? Color.Yellow : IsMouseHovering ? Colors.FancyUIFatButtonMouseOver : Color.Black;
        base.DrawSelf(sb);

        if (!IsValidNPC(NPCIndex))
            return;

        NPC npc = Main.npc[NPCIndex];
        Rectangle rect = GetDimensions().ToRectangle();
        int shrink = (int)MathF.Round(6f * scale);
        int textGap = (int)MathF.Round(1f * scale);
        Rectangle content = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        bool showNPC = SpectateHudClientSettings.ShowPlayer;
        bool showName = SpectateHudClientSettings.ShowPlayerName;
        bool showDistance = SpectateHudClientSettings.ShowPlayerDistance;
        int nameHeight = showName ? (int)MathF.Round(24f * scale) : 0;
        int distanceHeight = showDistance ? (int)MathF.Round(22f * scale) : 0;
        int y = content.Y;

        if (showNPC)
        {
            int previewHeight = Math.Max(0, content.Height - nameHeight - distanceHeight - (showName || showDistance ? textGap : 0));
            Rectangle preview = new(content.X, y, content.Width, previewHeight);
            EntityDrawer.DrawEntityBackground(sb, preview);
            EntityDrawer.DrawNPCPreview(sb, npc, preview);
            y = preview.Bottom + (showName || showDistance ? textGap : 0);
        }

        if (showName)
        {
            Rectangle name = new(content.X, y, content.Width, nameHeight);
            DrawCenteredText(sb, StatDrawer.Truncate(FontAssets.MouseText.Value, npc.FullName, name.Width, 0.95f * scale), name, 1.1f * scale, Color.White);
            y = name.Bottom;
        }

        if (showDistance)
        {
            Rectangle distance = new(content.X, y, content.Width, distanceHeight);
            DrawCenteredText(sb, GetDistanceText(npc), distance, 0.9f * scale, Color.LightGray);
        }
    }

    internal static string GetDistanceText(NPC npc)
    {
        Player local = Main.LocalPlayer;
        float feet = local?.active == true ? Vector2.Distance(local.Center, npc.Center) / 8f : 0f;
        return $"({feet.ToString("F0", CultureInfo.InvariantCulture)} ft)";
    }

    internal static bool IsValidNPC(int npcIndex)
    {
        return npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex]?.active == true;
    }

    private static void DrawCenteredText(SpriteBatch sb, string text, Rectangle area, float scale, Color color)
    {
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f + 3f * scale);
        Utils.DrawBorderString(sb, text, position, color, scale);
    }
}
