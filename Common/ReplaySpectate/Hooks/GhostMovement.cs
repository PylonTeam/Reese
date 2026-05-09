using Microsoft.Xna.Framework.Input;
using Terraria.ID;

namespace Reese.Common.ReplaySpectate.Hooks;

/// <summary>
/// Makes ghosts faster when holding shift and allows click-to-teleport.
/// </summary>
[Autoload(Side = ModSide.Both)]
internal class GhostMovement : ModSystem
{
    public override void Load()
    {
        On_Player.Ghost += OnPlayerGhost;
    }

    public override void Unload()
    {
        On_Player.Ghost -= OnPlayerGhost;
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

        if (Main.mouseLeft && Main.mouseLeftRelease)
        {
            Main.mouseLeftRelease = false;

            Vector2 targetPosition = Main.MouseWorld - new Vector2(self.width * 0.5f, self.height * 0.5f);
            self.Teleport(targetPosition, TeleportationStyleID.RodOfDiscord);

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, self.whoAmI, targetPosition.X, targetPosition.Y, TeleportationStyleID.RodOfDiscord);
        }
    }
}