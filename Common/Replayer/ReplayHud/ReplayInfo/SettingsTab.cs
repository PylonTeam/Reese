using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.GhostHooks;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.Shared.Sections;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;

namespace Reese.Common.Replayer.ReplayHud.ReplayInfo;

internal sealed class SettingsTab : TabPage
{
    public override SpectatorTab Tab => SpectatorTab.Settings;
    public override string HeaderText => "Settings";
    public override string TooltipText => "Replay settings";
    public override Asset<Texture2D> Icon => Ass.Icon_Gear;

    public override float IconScale => 1.25f;

    public override Vector2 IconOffset => new Vector2(4, 4);

    protected override void Populate(UIList list)
    {
        AddSection(list, new GhostSettings());
        AddSection(list, new DrawSettings());
        //AddSection(list, new DisplaySettings());
    }
    private sealed class GhostSettings : SettingsSection
    {
        public override string HeaderText => "Replay Settings";
        public override float Height => 146f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Fullbright:", () => $"Fullbright: {OnOff(GhostFullbright.Enabled)}", GetFullbrightIcon, onLeftClick: () => GhostFullbright.Enabled = !GhostFullbright.Enabled),
                new("Reveal Map:", () => $"Reveal Map: {OnOff(MapRevealHelper.Revealed)}", GetRevealMapIcon, onLeftClick: () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
                new("Right Click Teleport:", () => $"Right Click Teleport: {OnOff(ReplayClientSettings.RightClickTeleport)}", GetRightClickTeleportIcon, onLeftClick: ReplayClientSettings.ToggleRightClickTeleport)
            ];
        }

        private static Texture2D GetFullbrightIcon()
        {
            return GhostFullbright.Enabled ? Ass.Icon_CandelabraOn.Value : Ass.Icon_CandelabraOff.Value;
        }

        private static Texture2D GetRevealMapIcon()
        {
            return MapRevealHelper.Revealed ? Ass.Icon_MapOn.Value : Ass.Icon_MapOff.Value;
        }

        private static Texture2D GetRightClickTeleportIcon()
        {
            return ReplayClientSettings.RightClickTeleport ? Ass.Icon_TeleportOn.Value : Ass.Icon_TeleportOff.Value;
        }
    }

    private sealed class DisplaySettings : SettingsSection
    {
        public override string HeaderText => "Display Settings";
        public override float Height => 86f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Compact HUD:", () => $"Compact HUD: {OnOff(ReplayClientSettings.IsCompactModeOn)}", GetRightClickTeleportIcon, onLeftClick: ReplayClientSettings.ToggleCompactMode)
            ];
        }

        private static Texture2D GetRightClickTeleportIcon()
        {
            return ReplayClientSettings.IsCompactModeOn ? Ass.Icon_Card1.Value : Ass.Icon_Card3.Value;
        }
    }

    private sealed class DrawSettings : SettingsSection
    {
        public override string HeaderText => "Draw Settings";
        public override float Height => 214f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Draw Players:", () => $"Draw Players: {OnOff(ReplayClientSettings.IsDrawPlayersOn)}", () => Ass.Icon_Player.Value, onLeftClick: ReplayClientSettings.TogglePlayers, iconScale: 1.5f),
                new("Draw Ghosts:", () => $"Draw Ghosts: {OnOff(ReplayClientSettings.IsDrawGhostsOn)}", GetGhostIcon, onLeftClick: ReplayClientSettings.ToggleGhosts, iconScale: 1.0f),
                new("Draw Projectiles:", () => $"Draw Projectiles: {OnOff(ReplayClientSettings.IsDrawProjectilesOn)}", GetProjectileIcon, onLeftClick: ReplayClientSettings.ToggleProjectiles),
                new("Draw NPCs:", () => $"Draw NPCs: {OnOff(ReplayClientSettings.IsDrawNPCsOn)}", () => Ass.Icon_NPC.Value, onLeftClick: ReplayClientSettings.ToggleNPCs),
                new("Draw Items:", () => $"Draw Items: {OnOff(ReplayClientSettings.IsDrawItemsOn)}", GetItemIcon, onLeftClick: ReplayClientSettings.ToggleItems, iconScale: 0.8f)
            ];
        }

        private static Texture2D ghostIcon;

        private static Texture2D GetGhostIcon()
        {
            if (ghostIcon != null)
                return ghostIcon;

            Texture2D source = TextureAssets.Ghost.Value;
            Rectangle frame = new(0, 0, source.Width, source.Height / 4);
            Color[] data = new Color[frame.Width * frame.Height];

            source.GetData(0, frame, data, 0, data.Length);
            ghostIcon = new Texture2D(Main.graphics.GraphicsDevice, frame.Width, frame.Height);
            ghostIcon.SetData(data);
            return ghostIcon;
        }

        private static Texture2D GetProjectileIcon()
        {
            return Ass.Icon_Arrow.Value;
        }

        private static Texture2D GetItemIcon()
        {
            return Ass.Icon_Chest.Value;
        }
    }
}