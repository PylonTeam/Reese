using Microsoft.Xna.Framework.Input;
using GhostSpectating.Common;
using Reese.Common.ReplaySpectate.UI;
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

        bool moving = HasMovementInput(local) || (Main.mouseRight && !IsMouseOverAnyInterface());
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

        bool fastGhost = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);
        if (fastGhost)
        {
            Vector2 delta = self.position - oldPosition;
            self.position += delta * 3f;

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, self.whoAmI);
        }

        if (Main.mouseRight && !IsMouseOverAnyInterface())
        {
            Vector2 targetPosition = Main.MouseWorld - new Vector2(self.width * 0.5f, self.height * 0.5f);
            self.Teleport(targetPosition, TeleportationStyleID.RodOfDiscord);

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, self.whoAmI, targetPosition.X, targetPosition.Y, TeleportationStyleID.RodOfDiscord);
        }
    }

    private static bool IsMouseOverAnyInterface()
    {
        return Main.blockMouse ||
               Main.LocalPlayer?.mouseInterface == true ||
               ModContent.GetInstance<ReplayControlsPanelUISystem>().IsMouseOverPanel() ||
               ModContent.GetInstance<SpectatorUISystem>().IsMouseOverSpectatorUI();
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
