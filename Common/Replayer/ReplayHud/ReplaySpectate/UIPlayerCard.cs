using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using ReLogic.Content;
using System;
using System.Globalization;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal sealed class UIPlayerCard : UIPanel
{
    internal static int CardWidth => 150;
    internal static int DetailHeight => 65 * 2; // biome BG is 65 height default

    public int PlayerIndex { get; }
    public int ListIndex { get; }

    private readonly float scale;

    public UIPlayerCard(int playerIndex, int listIndex, float scale = 1f)
    {
        PlayerIndex = playerIndex;
        ListIndex = listIndex;
        this.scale = scale;

        SetPadding(0f);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        // Update border and background color if this player card is selected
        bool isSelected = PlayerIndex >= 0 &&
            PlayerIndex < Main.maxPlayers &&
            Main.player[PlayerIndex]?.active == true &&
            SpectatorTargetSystem.IsLockedTargeting(Main.player[PlayerIndex]);

        if (isSelected)
        {
            BackgroundColor = new Color(20, 27, 62) * 0.95f;
            BorderColor = Color.Yellow;
        }
        else if (IsMouseHovering)
        {
            BackgroundColor = new Color(47, 61, 125) * 0.55f;
            BorderColor = Colors.FancyUIFatButtonMouseOver;
        }
        else
        {
            //BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BackgroundColor = new Color(20, 27, 62) * 0.95f;
            //BorderColor = new Color(116, 154, 255) * 0.75f;
            BorderColor = Color.Black;
        }

        base.DrawSelf(sb);

        // Null checks
        if (PlayerIndex is < 0 or >= Main.maxPlayers)
            return;

        Player player = Main.player[PlayerIndex];

        if (player is null || !player.active)
            return;

        Rectangle rect = GetDimensions().ToRectangle();

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        // Layout
        int shrink = (int)MathF.Round(6f * scale);
        int textGap = (int)MathF.Round(1f * scale);
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        bool showPlayer = SpectateHudClientSettings.ShowPlayer;
        bool showName = SpectateHudClientSettings.ShowPlayerName;
        bool showDistance = SpectateHudClientSettings.ShowPlayerDistance;
        int nameHeight = showName ? (int)MathF.Round(24f * scale) : 0;
        int distanceHeight = showDistance ? (int)MathF.Round(22f * scale) : 0;
        int y = contentRect.Y;

        if (showPlayer)
        {
            int previewHeight = Math.Max(0, contentRect.Height - nameHeight - distanceHeight - (showName || showDistance ? textGap : 0));
            Rectangle playerPreviewRect = new(contentRect.X, y, contentRect.Width, previewHeight);
            EntityDrawer.DrawEntityBackground(sb, playerPreviewRect);
            EntityDrawer.DrawPlayerCardPreview(sb, player, playerPreviewRect);
            y = playerPreviewRect.Bottom + (showName || showDistance ? textGap : 0);
        }

        Color textColor = GetPlayerTextColor(player);

        if (showName)
        {
            Rectangle nameRect = new(contentRect.X, y, contentRect.Width, nameHeight);
            string name = PlayerIndex == Main.myPlayer ? "You" : player.name;
            DrawCenteredText(sb, StatDrawer.Truncate(FontAssets.MouseText.Value, name, nameRect.Width, 0.95f * scale), nameRect, 1.2f * scale, textColor);
            y = nameRect.Bottom;
        }

        if (showDistance)
        {
            Rectangle distanceRect = new(contentRect.X, y, contentRect.Width, distanceHeight);
            DrawCenteredText(sb, GetDistanceText(player), distanceRect, 0.9f * scale, textColor);
        }
    }

    internal static string GetDistanceText(Player player)
    {
        Player local = Main.LocalPlayer;
        float feet = local?.active == true ? Vector2.Distance(local.Center, player.Center) / 8f : 0f;
        return $"({feet.ToString("F0", CultureInfo.InvariantCulture)} ft)";
    }

    internal static Color GetPlayerTextColor(Player player)
    {
        return player.team > 0 ? Main.teamColor[player.team] : Color.White;
    }

    private static void DrawCenteredText(SpriteBatch sb, string text, Rectangle area, float scale, Color color)
    {
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f + 3f * scale);
        Utils.DrawBorderString(sb, text, position, color, scale);
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
