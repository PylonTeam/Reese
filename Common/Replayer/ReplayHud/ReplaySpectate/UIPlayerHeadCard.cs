using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal sealed class UIPlayerHeadCard : UIPanel
{
    public int PlayerIndex { get; }
    public int ListIndex { get; }

    private readonly float scale;

    public UIPlayerHeadCard(int playerIndex, int listIndex, float scale = 1f)
    {
        PlayerIndex = playerIndex;
        ListIndex = listIndex;
        this.scale = scale;

        SetPadding(0f);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        bool isSelected = PlayerIndex >= 0 &&
            PlayerIndex < Main.maxPlayers &&
            Main.player[PlayerIndex]?.active == true &&
            SpectatorTargetSystem.IsLockedTargeting(Main.player[PlayerIndex]);

        BackgroundColor = isSelected || !IsMouseHovering ? new Color(20, 27, 62) * 0.95f : new Color(47, 61, 125) * 0.55f;
        BorderColor = isSelected ? Color.Yellow : IsMouseHovering ? Colors.FancyUIFatButtonMouseOver : Color.Black;
        base.DrawSelf(sb);

        if (PlayerIndex is < 0 or >= Main.maxPlayers || Main.player[PlayerIndex]?.active != true)
            return;

        Player player = Main.player[PlayerIndex];
        Rectangle rect = GetDimensions().ToRectangle();
        int shrink = (int)MathF.Round(6f * scale);
        int textGap = (int)MathF.Round(1f * scale);
        int nameHeight = (int)MathF.Round(24f * scale);
        int distanceHeight = (int)MathF.Round(22f * scale);
        Rectangle content = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        Rectangle head = new(content.X, content.Y, content.Width, Math.Max(0, content.Height - nameHeight - distanceHeight - textGap));
        float headScale = Math.Min(head.Width, head.Height) / 42f;
        Color textColor = UIPlayerCard.GetPlayerTextColor(player);

        EntityDrawer.DrawPlayerHead(sb, player, head.Center.ToVector2(), headScale);

        Rectangle name = new(content.X, head.Bottom + textGap, content.Width, nameHeight);
        string displayName = PlayerIndex == Main.myPlayer ? "You" : player.name;
        UIPlayerCard.DrawCenteredText(sb, StatDrawer.Truncate(FontAssets.MouseText.Value, displayName, name.Width, 0.95f * scale), name, 1.2f * scale, textColor);

        Rectangle distance = new(content.X, name.Bottom, content.Width, distanceHeight);
        UIPlayerCard.DrawCenteredText(sb, UIPlayerCard.GetDistanceText(player), distance, 0.9f * scale, textColor);
    }
}
