using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal sealed class UIPlayerDetailPanel : UIPanel
{
    public int PlayerIndex { get; }

    private readonly float scale;

    public UIPlayerDetailPanel(int playerIndex, float scale)
    {
        PlayerIndex = playerIndex;
        this.scale = scale;

        SetPadding(0f);
        float buttonSize = 32f * scale;
        UIPlayerCard.AddActionButtons(this, playerIndex, scale, 5f * scale, UIPlayerCard.DetailHeight * scale - 5f * scale - buttonSize);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        BackgroundColor = new Color(20, 27, 62) * 0.95f;
        BorderColor = Color.Yellow;
        base.DrawSelf(sb);

        if (PlayerIndex is < 0 or >= Main.maxPlayers || Main.player[PlayerIndex]?.active != true)
            return;

        Player player = Main.player[PlayerIndex];
        Rectangle rect = GetDimensions().ToRectangle();
        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(32f * scale);
        int buttonGap = (int)MathF.Round(2f * scale);
        int previewWidth = buttonSize * 2 + buttonGap;
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        Rectangle previewRect = new(contentRect.X, contentRect.Y, previewWidth, contentRect.Height - buttonSize - (int)MathF.Round(3f * scale));
        Rectangle infoRect = new(previewRect.Right + (int)MathF.Round(6f * scale), contentRect.Y + (int)MathF.Round(6f * scale), contentRect.Right - previewRect.Right - (int)MathF.Round(14f * scale), contentRect.Height);
        Rectangle nameRect = new(infoRect.X, infoRect.Y - 2, infoRect.Width, (int)MathF.Round(24f * scale));

        EntityDrawer.DrawEntityBackground(sb, previewRect);
        EntityDrawer.DrawPlayerCardPreview(sb, player, previewRect);

        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, player.name, nameRect.Width, scale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName) * scale;
        Utils.DrawBorderString(sb, displayName, new Vector2(nameRect.X, nameRect.Y + (nameRect.Height - nameSize.Y) * 0.5f + 4f), UIPlayerCard.GetPlayerTextColor(player), scale);

        int statH = (int)MathF.Round(27f * scale);
        int statG = (int)MathF.Round(3f * scale);
        Rectangle lifeRect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(2f * scale), infoRect.Width, statH);
        Rectangle manaRect = new(infoRect.X, lifeRect.Bottom + statG, infoRect.Width, statH);
        Rectangle biomeRect = new(infoRect.X, manaRect.Bottom + statG, infoRect.Width, statH);

        StatDrawer.DrawPlayerStat(sb, lifeRect, PlayerStats.Life(player), scale);
        StatDrawer.DrawPlayerStat(sb, manaRect, PlayerStats.Mana(player), scale);
        StatDrawer.DrawPlayerStat(sb, biomeRect, PlayerStats.Biome(player), scale);
    }
}
