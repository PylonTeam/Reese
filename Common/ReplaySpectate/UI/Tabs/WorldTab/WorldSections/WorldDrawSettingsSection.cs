using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.ID;

namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;

internal sealed class WorldDrawSettingsSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.Settings;
    public override string HeaderText => "Replay Settings";
    public override float Height => 214f;

    public override IReadOnlyList<WorldSectionRow> GetRows()
    {
        return
        [
            new("Draw Players:", () => $"Draw Players: {OnOff(SpectatorDrawSettings.IsDrawPlayersOn)}", () => Ass.Icon_Player.Value, onLeftClick: SpectatorDrawSettings.TogglePlayers),
            new("Draw Ghosts:", () => $"Draw Ghosts: {OnOff(SpectatorDrawSettings.IsDrawGhostsOn)}", () => Ass.GhostRight.Value, onLeftClick: SpectatorDrawSettings.ToggleGhosts),
            new("Draw Projectiles:", () => $"Draw Projectiles: {OnOff(SpectatorDrawSettings.IsDrawProjectilesOn)}", GetProjectileIcon, onLeftClick: SpectatorDrawSettings.ToggleProjectiles),
            new("Draw NPCs:", () => $"Draw NPCs: {OnOff(SpectatorDrawSettings.IsDrawNPCsOn)}", () => Ass.Icon_NPC.Value, onLeftClick: SpectatorDrawSettings.ToggleNPCs),
            new("Draw Items:", () => $"Draw Items: {OnOff(SpectatorDrawSettings.IsDrawItemsOn)}", GetItemIcon, onLeftClick: SpectatorDrawSettings.ToggleItems)
        ];
    }

    private static string OnOff(bool value) => value ? "On" : "Off";

    private static Texture2D GetProjectileIcon() => TextureAssets.Projectile[ProjectileID.WoodenArrowFriendly].Value;

    private static Texture2D GetItemIcon() => TextureAssets.Item[ItemID.GoldCoin].Value;
}
