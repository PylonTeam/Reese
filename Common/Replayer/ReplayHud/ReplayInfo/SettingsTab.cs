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
    public override string TooltipText => "Fine-tune your experience";
    public override Asset<Texture2D> Icon => Ass.IconGear;

    public override float IconScale => 1.1f;

    public override Vector2 IconOffset => new Vector2(2, 0);
    public override Vector2 TextOffset => new Vector2(-6, 0);

    protected override void Populate(UIList list)
    {
        AddSection(list, new GhostSettings());
        AddSection(list, new DrawSettings());
        //AddSection(list, new DisplaySettings());
    }
    private sealed class GhostSettings : SettingsSection
    {
        public override string HeaderText => "Visualization";
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
            return GhostFullbright.Enabled ? Ass.IconCandelabraOn.Value : Ass.IconCandelabraOff.Value;
        }

        private static Texture2D GetRevealMapIcon()
        {
            return MapRevealHelper.Revealed ? Ass.IconMapOn.Value : Ass.IconMapOff.Value;
        }

        private static Texture2D GetRightClickTeleportIcon()
        {
            return ReplayClientSettings.RightClickTeleport ? Ass.IconTeleportOn.Value : Ass.IconTeleportOff.Value;
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
                //new("Compact HUD:", () => $"Compact HUD: {OnOff(ReplayClientSettings.IsCompactModeOn)}", GetRightClickTeleportIcon, onLeftClick: ReplayClientSettings.ToggleCompactMode)
            ];
        }

        //private static Texture2D GetRightClickTeleportIcon()
        //{
        //    return ReplayClientSettings.IsCompactModeOn ? Ass.IconCard1.Value : Ass.IconCard3.Value;
        //}
    }

    private sealed class DrawSettings : SettingsSection
    {
        public override string HeaderText => "Draw Settings";
        public override float Height => 244f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Draw Players:", () => $"Draw Players: {OnOff(ReplayClientSettings.IsDrawPlayersOn)}", () => Ass.IconPlayer.Value, onLeftClick: ReplayClientSettings.TogglePlayers, iconScale: 1.5f),
                new("Draw Ghosts:", () => $"Draw Ghosts: {OnOff(ReplayClientSettings.IsDrawGhostsOn)}", GetGhostIcon, onLeftClick: ReplayClientSettings.ToggleGhosts, iconScale: 1.0f),
                new("Draw Projectiles:", () => $"Draw Projectiles: {OnOff(ReplayClientSettings.IsDrawProjectilesOn)}", GetProjectileIcon, onLeftClick: ReplayClientSettings.ToggleProjectiles),
                new("Draw NPCs:", () => $"Draw NPCs: {OnOff(ReplayClientSettings.IsDrawNPCsOn)}", () => Ass.IconNPC.Value, onLeftClick: ReplayClientSettings.ToggleNPCs),
                new("Draw Items:", () => $"Draw Items: {OnOff(ReplayClientSettings.IsDrawItemsOn)}", GetItemIcon, onLeftClick: ReplayClientSettings.ToggleItems, iconScale: 0.8f),
                new("Draw Nameplates:", () => $"Draw Nameplates: {OnOff(ReplayClientSettings.IsNameplatesOn)}", GetNameplateIcon, onLeftClick: ReplayClientSettings.ToggleNameplates, iconScale: 0.8f)
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
            return Ass.IconVenomArrow.Value;
        }

        private static Texture2D GetItemIcon()
        {
            return Ass.IconChest.Value;
        }

        private static Texture2D GetNameplateIcon()
        {
            return Ass.IconPlayerHead.Value;
        }
    }
}