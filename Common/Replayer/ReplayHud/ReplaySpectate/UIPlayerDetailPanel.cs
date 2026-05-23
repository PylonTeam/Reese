using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

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
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
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

    #region Action buttons
    private readonly record struct PlayerCardAction(
        Asset<Texture2D> Icon,
        Asset<Texture2D> SelectedIcon,
        string HoverText,
        string SelectedHoverText,
        Action<int> Click,
        Func<int, bool> Selected
    );
    private static PlayerCardAction[] GetPlayerCardActions()
    {
        return
        [
            new PlayerCardAction(
                Ass.IconInventoryClosed,
                Ass.IconInventoryOpen,
                "Open inventory",
                "Close inventory",
                TeammateHudOverlay.Toggle,
                TeammateHudOverlay.IsOpen)
            ];
    }

    internal static void AddActionButtons(UIElement parent, int playerIndex, float scale, float left, float top)
    {
        float buttonSize = 32f * scale;
        float buttonGap = 2f * scale;
        PlayerCardAction[] actions = GetPlayerCardActions();

        for (int i = 0; i < actions.Length; i++)
        {
            AddActionButton(parent, playerIndex, actions[i], left, top, buttonSize);
            left += buttonSize + buttonGap;
        }
    }

    private static void AddActionButton(UIElement parent, int playerIndex, PlayerCardAction action, float left, float top, float size)
    {
        PlayerCardActionButton button = new(playerIndex, action);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        parent.Append(button);
    }

    private sealed class PlayerCardActionButton : UIElement
    {
        private readonly int playerIndex;
        private readonly PlayerCardAction action;

        public PlayerCardActionButton(int playerIndex, PlayerCardAction action)
        {
            this.playerIndex = playerIndex;
            this.action = action;

            OnLeftClick += (_, _) =>
            {
                Main.LocalPlayer.mouseInterface = true;

                if (!IsValidPlayer())
                    return;

                action.Click(playerIndex);
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(IsSelected() ? action.SelectedHoverText : action.HoverText);
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool isSelected = IsSelected();

            Texture2D background = isSelected
                ? TextureAssets.InventoryBack14.Value
                : IsMouseHovering
                    ? TextureAssets.InventoryBack7.Value
                    : TextureAssets.InventoryBack.Value;

            Asset<Texture2D> iconAsset = isSelected && action.SelectedIcon is not null ? action.SelectedIcon : action.Icon;

            if (iconAsset is null)
                return;

            Texture2D icon = iconAsset.Value;
            float scale = Math.Min((box.Width - 8f) / icon.Width, (box.Height - 8f) / icon.Height);
            Color color = isSelected || IsMouseHovering ? Color.White : Color.White * 0.8f;

            sb.Draw(background, box, Color.White * 0.85f);
            sb.Draw(icon, box.Center.ToVector2(), null, color, 0f, icon.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);
        }

        private bool IsSelected()
        {
            return IsValidPlayer() && action.Selected(playerIndex);
        }

        private bool IsValidPlayer()
        {
            return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex]?.active == true;
        }
    }

    #endregion
}
