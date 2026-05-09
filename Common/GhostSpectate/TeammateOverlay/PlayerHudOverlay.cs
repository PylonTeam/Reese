using Microsoft.Xna.Framework.Graphics;
using Reese.Common.GhostSpectate.TeammateOverlay.Drawers;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.GameInput;

namespace Reese.Common.GhostSpectate.TeammateOverlay;

internal static class PlayerHudOverlay
{
    private static int playerIndex = -1;
    private static bool releaseInventory = true;
    private static bool acceptingOverlayHover;
    public static bool IsAnyOpen => IsValidPlayerIndex(playerIndex);
    internal static bool AcceptsMouseInput => acceptingOverlayHover || !PlayerInput.IgnoreMouseInterface;

    public static void Update()
    {
        Player local = Main.LocalPlayer;

        if (local?.active != true)
        {
            Clear();
            releaseInventory = true;
            return;
        }

        if (!local.controlInv)
        {
            releaseInventory = true;
            return;
        }

        if (!releaseInventory)
            return;

        releaseInventory = false;

        //Toggle(target);
    }

    public static bool IsOpen(Player player)
    {
        return player?.active == true && playerIndex == player.whoAmI;
    }

    public static bool IsOpen(int targetPlayerIndex)
    {
        return IsValidPlayerIndex(targetPlayerIndex) && playerIndex == targetPlayerIndex;
    }

    public static void Toggle(Player player)
    {
        if (player?.active != true)
        {
            Clear();
            return;
        }

        playerIndex = IsOpen(player) ? -1 : player.whoAmI;
    }

    public static void Toggle(int targetPlayerIndex)
    {
        if (!IsValidPlayerIndex(targetPlayerIndex))
        {
            Clear();
            return;
        }

        playerIndex = IsOpen(targetPlayerIndex) ? -1 : targetPlayerIndex;
    }

    public static void Clear()
    {
        playerIndex = -1;
        ClearOwnedHover();
    }

    public static void ClearOwnedHover()
    {
        //InventoryDrawer.ClearOwnedHover();
        HotbarDrawer.ClearOwnedHover();
        BuffDrawer.ClearOwnedHover();
    }

    public static void Draw(SpriteBatch sb)
    {
        Player player = GetDrawTarget();

        if (player?.active != true)
        {
            Clear();
            return;
        }

        DrawPlayerHud(sb, player, IsOpen(player));
    }

    public static void DrawPlayerHud(SpriteBatch sb, Player player, bool inventoryOpen)
    {
        if (player?.active != true)
            return;

        acceptingOverlayHover = true;
        try
        {
            Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);

            if (inventoryOpen)
            {
                HotbarDrawer.ClearOwnedHover();
                DrawResourceBars(sb, player);

                Vector2 inventoryPosition = new(20f, 20f);
                //InventoryDrawer.DrawInventory(sb, inventoryPosition, player, viewport);
                DrawInventoryBuffs(sb, player, viewport, inventoryPosition);

                return;
            }

            //InventoryDrawer.ClearOwnedHover();
            DrawHotbarHud(sb, player);
            DrawResourceAndBuffHud(sb, player);
        }
        finally
        {
            acceptingOverlayHover = false;
        }
    }

    public static void DrawInventoryHud(SpriteBatch sb, Player player)
    {
        DrawPlayerHud(sb, player, inventoryOpen: true);
    }

    public static void DrawCompactHud(SpriteBatch sb, Player player)
    {
        DrawPlayerHud(sb, player, inventoryOpen: false);
    }

    public static void DrawHotbarHud(SpriteBatch sb, Player player)
    {
        if (player?.active != true)
            return;

        //InventoryDrawer.ClearOwnedHover();
        BuffDrawer.ClearOwnedHover();
        DrawHotbar(sb, player);
    }

    public static void DrawResourceAndBuffHud(SpriteBatch sb, Player player)
    {
        if (player?.active != true)
            return;

        //InventoryDrawer.ClearOwnedHover();
        DrawResourceBarsHud(sb, player);
        DrawBuffHud(sb, player);
    }

    public static void DrawResourceBarsHud(SpriteBatch sb, Player player)
    {
        if (player?.active != true)
            return;

        //InventoryDrawer.ClearOwnedHover();
        DrawResourceBars(sb, player);
    }

    public static void DrawBuffHud(SpriteBatch sb, Player player)
    {
        if (player?.active != true)
            return;

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);

        //InventoryDrawer.ClearOwnedHover();
        DrawBuffs(sb, player, viewport);
    }

    private static void DrawInventoryBuffs(SpriteBatch spriteBatch, Player player, Rectangle viewport, Vector2 inventoryPosition)
    {
        const float slotSize = 52f;
        const int inventoryRows = 5;

        Vector2 position = new(inventoryPosition.X, inventoryPosition.Y + inventoryRows * slotSize + 14f);
        BuffDrawer.DrawBuffs(spriteBatch, position, player, viewport);
    }

    private static Player GetDrawTarget()
    {
        if (IsValidPlayerIndex(playerIndex))
            return Main.player[playerIndex];

        return null;
    }

    private static void DrawHotbar(SpriteBatch sb, Player player)
    {
        string text = $"{player.name}";
        //Vector2 position = new(4f, 2f);
        //Utils.DrawBorderString(sb, text, position, Color.White, 0.9f);

        sb.DrawString(FontAssets.MouseText.Value, text, new Vector2(4f, 0f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        HotbarDrawer.DrawHotbar(player);
    }

    private static void DrawBuffs(SpriteBatch spriteBatch, Player player, Rectangle viewport)
    {
        Vector2 position = new(20f, 88f);
        BuffDrawer.DrawBuffs(spriteBatch, position, player, viewport);
    }

    private static void DrawResourceBars(SpriteBatch spriteBatch, Player player)
    {
        ResourceBarsDrawer.DrawResourceBarsLikeVanilla(player);
    }

    private static bool IsValidPlayerIndex(int targetPlayerIndex)
    {
        return targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers && Main.player[targetPlayerIndex]?.active == true;
    }
}
