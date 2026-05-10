using Microsoft.Xna.Framework.Input;
using Reese.Common.ReplaySpectate.UI;
using Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;
using System;
using Terraria.ID;
using Reese.Common.ReplayControls;
using Reese.Common.ReplayControls.TimeScale;

namespace Reese.Common.ReplaySpectate.Hooks;

/// <summary>
/// Makes ghosts faster when holding shift and allows right-click-to-teleport.
/// </summary>
[Autoload(Side = ModSide.Both)]
internal class GhostMovement : ModSystem
{
    private const float CursorTeleportLerp = 0.18f;

    public override void Load()
    {
        On_Player.Ghost += OnPlayerGhost;
        On_Main.DoUpdate += OnMainDoUpdate;
    }

    public override void Unload()
    {
        On_Player.Ghost -= OnPlayerGhost;
        On_Main.DoUpdate -= OnMainDoUpdate;
    }

    private void OnMainDoUpdate(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

        if (Main.dedServ || Main.gameMenu)
            return;

        if (ModContent.GetInstance<TimeScaleSystem>().TimeScale > 0f)
            return;

        Player local = Main.LocalPlayer;
        if (local?.active != true || !local.ghost || local.whoAmI != Main.myPlayer)
            return;

        bool moving = HasMovementInput(local) || IsRightClickTeleporting();
        if (moving)
            SpectatorTargetSystem.ClearTarget(moveCameraToLocal: false);

        local.Ghost();

        if (moving)
            CenterCameraOn(local);
    }

    private void OnPlayerGhost(On_Player.orig_Ghost orig, Player self)
    {
        Vector2 oldPosition = self.position;
        orig(self);

        if (!self.ghost || self.whoAmI != Main.myPlayer)
            return;

        ApplyGhostDirection(self);

        bool fastGhost = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);
        if (fastGhost)
        {
            Vector2 delta = self.position - oldPosition;
            self.position += delta * 3f;

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, self.whoAmI);
        }

        if (IsRightClickTeleporting())
            SmoothMoveToCursor(self);
    }

    internal static void ApplyGhostDirection(Player player)
    {
        int direction = GetGhostDirection(player);
        player.direction = direction;
        player.ghostDir = direction;
    }

    private static int GetGhostDirection(Player player)
    {
        if (IsRightClickTeleporting())
            return GetMouseDirection(player);

        if (player.controlLeft && !player.controlRight)
            return -1;

        if (player.controlRight && !player.controlLeft)
            return 1;

        return GetMouseDirection(player);
    }

    private static int GetMouseDirection(Player player) => Main.MouseWorld.X < player.Center.X ? -1 : 1;

    private static void SmoothMoveToCursor(Player player)
    {
        Vector2 targetPosition = Main.MouseWorld - new Vector2(player.width * 0.5f, player.height * 0.5f);
        Vector2 nextPosition = Vector2.Lerp(player.position, targetPosition, CursorTeleportLerp);

        if (Vector2.DistanceSquared(nextPosition, targetPosition) < 4f)
            nextPosition = targetPosition;

        player.position = nextPosition;
        player.velocity = Vector2.Zero;

        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);
    }

    // Obsolete because we're using right click instead of left click, which doesn't have the same issues with UI interaction.
    // Keep this commented out.
    //private static bool IsMouseOverAnyInterface()
    //{
    //    return Main.blockMouse ||
    //           Main.LocalPlayer?.mouseInterface == true ||
    //           ModContent.GetInstance<ReplayControlsPanelUISystem>().IsMouseOverPanel() ||
    //           ModContent.GetInstance<SpectatorUISystem>().IsMouseOverSpectatorUI();
    //}

    private static bool IsRightClickTeleporting()
    {
        return SpectatorClientSettings.RightClickTeleport && Main.mouseRight;
    }

    private static bool HasMovementInput(Player player)
    {
        return player.controlLeft || player.controlRight || player.controlUp || player.controlDown || player.controlJump;
    }

    private static void CenterCameraOn(Player player)
    {
        Vector2 screenPosition = player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
        float maxX = Math.Max(0f, Main.maxTilesX * 16f - Main.screenWidth);
        float maxY = Math.Max(0f, Main.maxTilesY * 16f - Main.screenHeight);
        Main.screenPosition = new Vector2(MathHelper.Clamp(screenPosition.X, 0f, maxX), MathHelper.Clamp(screenPosition.Y, 0f, maxY));
    }
}
