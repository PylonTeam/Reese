using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplaySpectate.TeammateOverlay;
using Reese.Core.Debug;
using Reese.Core.Utilities;

namespace Reese.Common.Replayer.ReplaySpectate.Hooks;

/// <summary>
/// Allows inventory access while dead.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal class InventoryControlSystem : ModSystem
{
    public override void Load()
    {
        On_Main.DrawInterface_26_InterfaceLogic3 += ModifyInterfaceLogic;
        On_Player.TryOpeningInGameOptionsBasedOnInput += ModifyIngameOptionsInput;
    }
    public override void Unload()
    {
        On_Main.DrawInterface_26_InterfaceLogic3 -= ModifyInterfaceLogic;
        On_Player.TryOpeningInGameOptionsBasedOnInput -= ModifyIngameOptionsInput;
    }

    private void ModifyIngameOptionsInput(On_Player.orig_TryOpeningInGameOptionsBasedOnInput orig, Player self)
    {
        bool inSpectateMode = SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer);

        if ((inSpectateMode || self.ghost) && KeyboardHelper.Pressed(Keys.Escape))
        {
            CloseOwnInventory();
            ToggleSpectatedPlayerInventory();
            return;
        }

        // Spectator special case
        if (inSpectateMode || self.ghost)
        {
            CloseOwnInventory();
            if (!Main.ingameOptionsWindow)
                return;
        }

        orig(self);
    }

    private static void ToggleSpectatedPlayerInventory()
    {
        Player target = SpectatorTargetSystem.GetPlayerTarget();

        if (target?.active == true)
        {
            Log.Chat("Toggle spectated player's inventory for " + target.name);
            PlayerHudOverlay.Toggle(target.whoAmI);
            return;
        }

        Log.Chat("Inventory is disabled as a spectator unless you are spectating another player.");
    }

    private void ModifyInterfaceLogic(On_Main.orig_DrawInterface_26_InterfaceLogic3 orig)
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
        {
            orig();
            return;
        }

        //if (SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || Main.LocalPlayer.ghost)
        //{
        //    CloseOwnInventory();
        //    return;
        //}

        orig();
    }

    private static void CloseOwnInventory()
    {
        //Log.Chat("Closing ghost/spectator inventory");
        Main.playerInventory = false;
        Main.LocalPlayer.chest = -1;
        Main.InGuideCraftMenu = false;
        Main.InReforgeMenu = false;
    }
}
