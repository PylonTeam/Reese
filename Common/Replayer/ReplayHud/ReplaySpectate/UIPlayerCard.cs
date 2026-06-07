using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal sealed class UIPlayerCard : UIEntityCard<Player>
{
    public int PlayerIndex => EntityIndex;

    public UIPlayerCard(int playerIndex, int listIndex, float scale = 1f) : base(playerIndex, listIndex, scale)
    {
        if (SpectateHudClientSettings.EntityHudMode != EntityHudMode.Detailed)
            return;

        float buttonSize = 32f * scale;
        AddActionButtons(this, playerIndex, scale, 5f * scale, UIEntityCard<Player>.DetailHeight * scale - 5f * scale - buttonSize);
    }

    protected override bool TryGetEntity(int index, out Player player)
    {
        player = index >= 0 && index < Main.maxPlayers ? Main.player[index] : null;
        return player?.active == true;
    }

    protected override bool IsSelected(Player player)
    {
        return SpectatorTargetSystem.IsLockedTargeting(player);
    }

    protected override string GetDisplayName(Player player)
    {
        return PlayerIndex == Main.myPlayer ? Loc.Get("ReplayHud.Spectate.You") : player.name;
    }

    protected override Color GetTextColor(Player player)
    {
        return GetPlayerTextColor(player);
    }

    protected override float GetFullNameScale()
    {
        return 1.2f;
    }

    protected override void DrawPreview(SpriteBatch sb, Player player, Rectangle area)
    {
        EntityDrawer.DrawPlayerCardPreview(sb, player, area);
    }

    protected override void DrawHeadIcon(SpriteBatch sb, Player player, Rectangle area)
    {
        //Vector2 headPos = area.TopLeft() + new Vector2(8, 4);
        Vector2 headPos = area.Center.ToVector2() + new Vector2(2,-2);
        //float scale = Math.Min(area.Width, area.Height) / 26f;
        float scale = 0.6f;
        EntityDrawer.DrawPlayerHead(sb, player, headPos, scale: scale);
    }

    protected override void DrawStats(SpriteBatch sb, Player player, Rectangle stat, int statGap, float scale)
    {
        StatDrawer.DrawPlayerStat(sb, stat, PlayerStats.Life(player), scale);
        stat = NextStat(stat, statGap);

        StatDrawer.DrawPlayerStat(sb, stat, PlayerStats.Mana(player), scale);
        stat = NextStat(stat, statGap);

        StatDrawer.DrawPlayerStat(sb, stat, PlayerStats.Biome(player), scale);
    }

    internal static Color GetPlayerTextColor(Player player)
    {
        return player.team > 0 ? Main.teamColor[player.team] : Color.White;
    }

    internal static bool IsValidPlayer(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex]?.active == true;
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
                Loc.Get("ReplayHud.Spectate.OpenInventory"),
                Loc.Get("ReplayHud.Spectate.CloseInventory"),
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

            if (!IsMouseHovering)
                return;

            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(IsSelected() ? action.SelectedHoverText : action.HoverText);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool selected = IsSelected();

            Texture2D background = selected
                ? TextureAssets.InventoryBack14.Value
                : IsMouseHovering
                    ? TextureAssets.InventoryBack7.Value
                    : TextureAssets.InventoryBack.Value;

            Asset<Texture2D> iconAsset = selected && action.SelectedIcon is not null ? action.SelectedIcon : action.Icon;

            if (iconAsset is null)
                return;

            Texture2D icon = iconAsset.Value;
            float scale = Math.Min((box.Width - 8f) / icon.Width, (box.Height - 8f) / icon.Height);
            Color color = selected || IsMouseHovering ? Color.White : Color.White * 0.8f;

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
