using Reese.Common.Spectator;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;

/// <summary>
/// Owns the lifetime of <see cref="TeammateHudOverlay"/>.
/// The overlay used to be updated and drawn by ReplayHudState/GhostHudState, which tied it to
/// the HUD panels: toggling the replay HUD off tore down the UIState and took the spectated
/// player's inventory with it. It now lives on its own interface layer, gated only on whether
/// the local client can spectate.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class TeammateOverlaySystem : ModSystem
{
    internal const string LayerName = "Reese: Teammate Overlay";

    public override void UpdateUI(GameTime gameTime)
    {
        if (SpectatorMode.CanSpectate)
            TeammateHudOverlay.Update();
        else
            TeammateHudOverlay.Clear();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!SpectatorMode.CanSpectate)
            return;

        int index = GetOverlayInsertIndex(layers);

        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(LayerName, () =>
        {
            TeammateHudOverlay.Draw(Main.spriteBatch);
            return true;
        }, InterfaceScaleType.UI));
    }

    /// <summary>
    /// The overlay draws on top of the HUD panels, as it did when the HUD states drew it after
    /// their own children. Ordering is deterministic regardless of which system's
    /// ModifyInterfaceLayers runs first: if a HUD layer already exists we insert after it, and if
    /// it does not, the HUD systems anchor themselves before <see cref="LayerName"/> on their pass.
    /// </summary>
    private static int GetOverlayInsertIndex(List<GameInterfaceLayer> layers)
    {
        int hudIndex = layers.FindIndex(l => l.Name is "Reese: Replay HUD" or "Reese: Ghost HUD");

        if (hudIndex != -1)
            return hudIndex + 1;

        int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");

        if (mouseTextIndex != -1)
            return mouseTextIndex;

        return layers.FindIndex(l => l.Name == "Vanilla: Death Text");
    }
}
