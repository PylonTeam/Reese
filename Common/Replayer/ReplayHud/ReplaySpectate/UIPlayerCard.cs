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

internal sealed class UIPlayerCard : UIPanel
{
    internal static int CardWidth => 115 * 2; // biome BG is 115 width default
    internal static int CardHeight => 65*2; // biome BG is 65 height default

    public int PlayerIndex { get; }
    public int ListIndex { get; }

    private readonly float scale;
    private readonly SpectateHud owner;

    public UIPlayerCard(int playerIndex, int listIndex, SpectateHud owner, float scale = 1f)
    {
        PlayerIndex = playerIndex;
        ListIndex = listIndex;
        this.scale = scale;
        this.owner = owner;

        SetPadding(0f);
        AddActionButtons(GetPlayerCardActions());
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
            BackgroundColor = Color.Yellow;
            BorderColor = Color.Yellow;
        }
        else if (IsMouseHovering)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BorderColor = Colors.FancyUIFatButtonMouseOver;
        }
        else
        {
            //BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BackgroundColor = new Color(28, 36, 76) * 0.92f;
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
        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(32f * scale);
        int buttonGap = (int)MathF.Round(2f * scale);
        int buttonCount = 2;
        int buttonRowHeight = buttonSize;
        int buttonRowGap = (int)MathF.Round(3f * scale);
        int buttonContentWidth = buttonSize * buttonCount + buttonGap * (buttonCount - 1);
        int infoGap = (int)MathF.Round(6f * scale);
        int infoTopPadding = (int)MathF.Round(6f * scale);
        int infoRightPadding = (int)MathF.Round(8f * scale);

        Rectangle backgroundRect = rect;
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);

        Rectangle playerPreviewRect = new(
            contentRect.X,
            contentRect.Y,
            buttonContentWidth,
            contentRect.Height - buttonRowHeight - buttonRowGap);

        Rectangle infoRect = new(
            playerPreviewRect.Right + infoGap,
            contentRect.Y + infoTopPadding,
            contentRect.Right - playerPreviewRect.Right - infoGap - infoRightPadding,
            contentRect.Height - infoTopPadding * 2);

        Rectangle nameRect = new(infoRect.X, infoRect.Y - 2, infoRect.Width, (int)MathF.Round(24f * scale));

        // Draw biome BG
        //BiomeBackgroundDrawer.DrawMapFullscreenBackground(sb, backgroundRect, player.Center, shrinkPadding: shrink);

        // Draw player preview background + player preview
        EntityDrawer.DrawEntityBackground(sb, playerPreviewRect);
        EntityDrawer.DrawPlayerCardPreview(sb, player, playerPreviewRect);

        // Draw player info to the right of the preview

        // Draw name
        string name = PlayerIndex == Main.myPlayer ? "You" : player.name;
        float textScale = 1.0f;
        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, name, nameRect.Width, textScale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName) * textScale;
        Vector2 namePosition = new(nameRect.X, nameRect.Y + (nameRect.Height - nameSize.Y) * 0.5f + 4f);

        Utils.DrawBorderString(sb, displayName, namePosition, Color.White, textScale);

        // --- Draw player stats ---
        int statH = (int)MathF.Round(27f * scale);
        int statG = (int)MathF.Round(3f * scale);

        // Stat 1: Life
        Rectangle stat1Rect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(2f * scale), infoRect.Width, statH);
        StatDrawer.DrawPlayerStat(sb, stat1Rect, PlayerStats.Life(player), scale);

        // Stat 2: Mana
        Rectangle stat2Rect = new(infoRect.X, stat1Rect.Bottom + statG, infoRect.Width, statH);
        StatDrawer.DrawPlayerStat(sb, stat2Rect, PlayerStats.Mana(player), scale);

        // Stat 3: Defense
        Rectangle stat3Rect = new(infoRect.X, stat2Rect.Bottom + statG, infoRect.Width, statH);
        StatDrawer.DrawPlayerStat(sb, stat3Rect, PlayerStats.Biome(player), scale);

        // Debug draw rectangles
        //DebugDrawer.DrawRectangle(playerPreviewRect, drawSize: true);
        //DebugDrawer.DrawRectangle(nameRect, drawSize: true);
        //DebugDrawer.DrawRectangle(stat1Rect, drawSize: true);
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
                Ass.IconEye,
                Ass.IconEye,
                "Spectate player",
                "Stop spectating",
                static playerIndex => SpectatorTargetSystem.TogglePlayerTarget(playerIndex, moveCameraToLocal: false),
                static playerIndex => Main.player[playerIndex]?.active == true && SpectatorTargetSystem.IsLockedTargeting(Main.player[playerIndex])),

            new PlayerCardAction(
                Ass.IconInventoryClosed,
                Ass.IconInventoryOpen,
                "Open inventory",
                "Close inventory",
                TeammateHudOverlay.Toggle,
                TeammateHudOverlay.IsOpen)
            ];
    }

    private void AddActionButtons(PlayerCardAction[] actions)
    {
        float buttonSize = 32f * scale;
        float buttonGap = 2f * scale;
        float buttonTop = CardHeight * scale - 5f * scale - buttonSize;
        float buttonLeft = 5f * scale;

        for (int i = 0; i < actions.Length; i++)
        {
            AddActionButton(actions[i], buttonLeft, buttonTop, buttonSize);
            buttonLeft += buttonSize + buttonGap;
        }
    }

    private void AddActionButton(PlayerCardAction action, float left, float top, float size)
    {
        PlayerCardActionButton button = new(PlayerIndex, action, owner);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        Append(button);
    }

    private sealed class PlayerCardActionButton : UIElement
    {
        private readonly int playerIndex;
        private readonly PlayerCardAction action;
        private readonly SpectateHud owner;

        public PlayerCardActionButton(int playerIndex, PlayerCardAction action, SpectateHud owner)
        {
            this.playerIndex = playerIndex;
            this.action = action;
            this.owner = owner;

            OnLeftClick += (_, _) =>
            {
                Main.LocalPlayer.mouseInterface = true;

                if (!IsValidPlayer())
                    return;

                action.Click(playerIndex);
                owner?.UpdateTarget();
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
