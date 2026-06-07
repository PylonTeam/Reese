using Reese.Common.Replayer.GhostHooks;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.Shared.Sections;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;

namespace Reese.Common.Replayer.ReplayHud.ReplayInfo;

internal sealed class SettingsTab : TabPage
{
    private readonly bool showReplayHudSettings;

    public SettingsTab(bool showReplayHudSettings = true)
    {
        this.showReplayHudSettings = showReplayHudSettings;
    }

    protected override float ScrollbarHeight => -36f;
    public override SpectatorTab Tab => SpectatorTab.Settings;
    public override string HeaderText => Language.GetTextValue("LegacyMenu.14");
    public override string TooltipText => Loc.Get("ReplayHud.Settings.TabTooltip");
    public override Asset<Texture2D> Icon => Ass.IconGear;

    public override float IconScale => 1.1f;

    public override Vector2 IconOffset => new Vector2(2, 0);
    public override Vector2 TextOffset => new Vector2(-6, 0);

    protected override void Populate(UIList list)
    {
        AddSection(list, new VisualizationSettings());
        AddSection(list, new SpectateHudSettingsSection());

        if (showReplayHudSettings)
            AddSection(list, new ReplayHudSettingsSection());

        AddSection(list, new DrawSettings());
    }

    private sealed class VisualizationSettings : SettingsSection
    {
        public override string HeaderText => Loc.Get("ReplayHud.Settings.VisualizationHeader");
        public override float Height => 80+26*4;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(Loc.Get("ReplayHud.Settings.Labels.Fullbright"), () => Loc.Get("ReplayHud.Settings.Rows.Fullbright", OnOff(GhostFullbright.Enabled)), GetFullbrightIcon, onLeftClick: () => GhostFullbright.Enabled = !GhostFullbright.Enabled),
                new(Loc.Get("ReplayHud.Settings.Labels.RevealMap"), () => Loc.Get("ReplayHud.Settings.Rows.RevealMap", OnOff(MapRevealHelper.Revealed)), GetRevealMapIcon, onLeftClick: () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
                new(Loc.Get("ReplayHud.Settings.Labels.RightClickTeleport"), () => Loc.Get("ReplayHud.Settings.Rows.RightClickTeleport", OnOff(ReplayClientSettings.RightClickTeleport)), GetRightClickTeleportIcon, onLeftClick: ReplayClientSettings.ToggleRightClickTeleport),
                new(Loc.Get("ReplayHud.Settings.Labels.Zoom"), () => Loc.Get("ReplayHud.Settings.Rows.Zoom", ReplayClientSettings.ReplayZoomPercent), () => Ass.IconEye.Value,
                    slider: new SliderRowConfig(
                        () => ReplayClientSettings.ReplayZoomRatio,
                        ReplayClientSettings.SetReplayZoomRatio))
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

    private sealed class SpectateHudSettingsSection : SettingsSection
    {
        public override string HeaderText => Loc.Get("ReplayHud.Settings.SpectateHudHeader");
        public override float Height => 80+26*4f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(Loc.Get("ReplayHud.Settings.Labels.ShowSpectateHud"), () => Loc.Get("ReplayHud.Settings.Rows.ShowSpectateHud", OnOff(ReplayClientSettings.ShowSpectateHud)), () => Ass.IconEye.Value, onLeftClick: ReplayClientSettings.ToggleShowSpectateHud),
                new(Loc.Get("ReplayHud.Settings.Labels.RowsVisible"), () => Loc.Get("ReplayHud.Settings.Rows.RowsVisible", SpectateHudClientSettings.EffectiveRowsVisible), () => Ass.IconResize.Value, onLeftClick: SpectateHudClientSettings.CycleRowsVisible),
                new(Loc.Get("ReplayHud.Settings.Labels.SortBy"), () => Loc.Get("ReplayHud.Settings.Rows.SortBy", SpectateHudClientSettings.SortModeDisplayName), () => Ass.IconRefresh.Value, onLeftClick: SpectateHudClientSettings.CycleSortMode),
                new(Loc.Get("ReplayHud.Settings.Labels.PlayerHudMode"), () => Loc.Get("ReplayHud.Settings.Rows.PlayerHudMode", SpectateHudClientSettings.EntityHudModeDisplayName), () => Ass.IconPlayerHead.Value, onLeftClick: SpectateHudClientSettings.CycleEntityHudMode, iconScale: 0.8f),
            ];
        }
    }

    private sealed class ReplayHudSettingsSection : SettingsSection
    {
        public override string HeaderText => Loc.Get("ReplayHud.Settings.ReplayHudHeader");
        public override float Height => 314f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(Loc.Get("ReplayHud.Settings.Labels.ShowReplayHud"), () => Loc.Get("ReplayHud.Settings.Rows.ShowReplayHud", OnOff(ReplayClientSettings.ShowPlaybackHud)), () => Ass.IconEye.Value, onLeftClick: ReplayClientSettings.ToggleShowPlaybackHud),
                

                // --- Events on the timeline ---
                new(Loc.Get("ReplayHud.Settings.Labels.ShowBossesSummoned"), () => Loc.Get("ReplayHud.Settings.Rows.ShowBossesSummoned", OnOff(ReplayClientSettings.ShowBossesSummoned)), GetBossHeadIcon, onLeftClick: ReplayClientSettings.ToggleShowBossesSummoned),
                new(Loc.Get("ReplayHud.Settings.Labels.ShowBossesDefeated"), () => Loc.Get("ReplayHud.Settings.Rows.ShowBossesDefeated", OnOff(ReplayClientSettings.ShowBossesDefeated)), GetDefeatedBossHeadIcon, onLeftClick: ReplayClientSettings.ToggleShowBossesDefeated),
                new(Loc.Get("ReplayHud.Settings.Labels.ShowPlayerJoinLeave"), () => Loc.Get("ReplayHud.Settings.Rows.ShowPlayerJoinLeave", OnOff(ReplayClientSettings.ShowPlayerJoinLeave)), () => Ass.IconPlayerHead.Value, onLeftClick: ReplayClientSettings.ToggleShowPlayerJoinLeave, iconScale: 0.8f),
                new(Loc.Get("ReplayHud.Settings.Labels.ShowPvpDeaths"), () => Loc.Get("ReplayHud.Settings.Rows.ShowPvpDeaths", OnOff(ReplayClientSettings.ShowPvpDeaths)), () => Ass.IconSword.Value, onLeftClick: ReplayClientSettings.ToggleShowPvpDeaths),
                new(Loc.Get("ReplayHud.Settings.Labels.ShowPveDeaths"), () => Loc.Get("ReplayHud.Settings.Rows.ShowPveDeaths", OnOff(ReplayClientSettings.ShowPveDeaths)), () => TextureAssets.MapDeath.Value, onLeftClick: ReplayClientSettings.ToggleShowPveDeaths),
                new(Loc.Get("ReplayHud.Settings.Labels.ShowInvasions"), () => Loc.Get("ReplayHud.Settings.Rows.ShowInvasions", OnOff(ReplayClientSettings.ShowInvasions)), () => Ass.Party_Center.Value, onLeftClick: ReplayClientSettings.ToggleShowInvasions),

                // --- Hud width (keep this as last row!!) ---
                new(Loc.Get("ReplayHud.Settings.Labels.HudWidth"), () => Loc.Get("ReplayHud.Settings.Rows.HudWidth", ReplayClientSettings.PlaybackHudWidthPercent), () => Ass.IconResize.Value,
                    slider: new SliderRowConfig(
                        () => ReplayClientSettings.PlaybackHudWidthRatio,
                        ReplayClientSettings.SetPlaybackHudWidthRatio)),
            ];
        }

        private static Texture2D defeatedBossHeadIcon;

        private static Texture2D GetBossHeadIcon()
        {
            int head = NPCID.Sets.BossHeadTextures[NPCID.KingSlime];
            return head >= 0 && head < TextureAssets.NpcHeadBoss.Length ? TextureAssets.NpcHeadBoss[head].Value : Ass.IconNPC.Value;
        }

        private static Texture2D GetDefeatedBossHeadIcon()
        {
            if (defeatedBossHeadIcon != null)
                return defeatedBossHeadIcon;

            Texture2D source = GetBossHeadIcon();
            Color[] data = new Color[source.Width * source.Height];

            source.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                byte gray = (byte)((data[i].R * 30 + data[i].G * 59 + data[i].B * 11) / 100);
                data[i] = new Color(gray, gray, gray, data[i].A);
            }

            defeatedBossHeadIcon = new Texture2D(Main.graphics.GraphicsDevice, source.Width, source.Height);
            defeatedBossHeadIcon.SetData(data);
            return defeatedBossHeadIcon;
        }
    }

    private sealed class DrawSettings : SettingsSection
    {
        public override string HeaderText => Loc.Get("ReplayHud.Settings.DrawHeader");
        public override float Height => 278f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(Loc.Get("ReplayHud.Settings.Labels.DrawPlayers"), () => Loc.Get("ReplayHud.Settings.Rows.DrawPlayers", OnOff(ReplayClientSettings.IsDrawPlayersOn)), () => Ass.IconPlayer.Value, onLeftClick: ReplayClientSettings.TogglePlayers, iconScale: 1.5f),
                new(Loc.Get("ReplayHud.Settings.Labels.DrawGhosts"), () => Loc.Get("ReplayHud.Settings.Rows.DrawGhosts", OnOff(ReplayClientSettings.IsDrawGhostsOn)), GetGhostIcon, onLeftClick: ReplayClientSettings.ToggleGhosts, iconScale: 1.0f),
                new(Loc.Get("ReplayHud.Settings.Labels.DrawProjectiles"), () => Loc.Get("ReplayHud.Settings.Rows.DrawProjectiles", OnOff(ReplayClientSettings.IsDrawProjectilesOn)), GetProjectileIcon, onLeftClick: ReplayClientSettings.ToggleProjectiles),
                new(Loc.Get("ReplayHud.Settings.Labels.DrawNpcs"), () => Loc.Get("ReplayHud.Settings.Rows.DrawNpcs", OnOff(ReplayClientSettings.IsDrawNPCsOn)), () => Ass.IconNPC.Value, onLeftClick: ReplayClientSettings.ToggleNPCs),
                new(Loc.Get("ReplayHud.Settings.Labels.DrawItems"), () => Loc.Get("ReplayHud.Settings.Rows.DrawItems", OnOff(ReplayClientSettings.IsDrawItemsOn)), GetItemIcon, onLeftClick: ReplayClientSettings.ToggleItems, iconScale: 0.8f),
                new(Loc.Get("ReplayHud.Settings.Labels.DrawChat"), () => Loc.Get("ReplayHud.Settings.Rows.DrawChat", OnOff(ReplayClientSettings.IsDrawChatOn)), () => Ass.IconEye.Value, onLeftClick: ReplayClientSettings.ToggleChat),
                new(Loc.Get("ReplayHud.Settings.Labels.DrawNameplates"), () => Loc.Get("ReplayHud.Settings.Rows.DrawNameplates", OnOff(ReplayClientSettings.IsNameplatesOn)), GetNameplateIcon, onLeftClick: ReplayClientSettings.ToggleNameplates, iconScale: 0.8f)
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
