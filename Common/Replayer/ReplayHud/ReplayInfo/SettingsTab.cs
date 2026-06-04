using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.GhostHooks;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.Shared.Sections;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

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
        list.Add(new ZoomSettingsSection());
        list.Add(new PlaybackHudWidthSettingsSection());
        AddSection(list, new GhostSettings());
        AddSection(list, new DrawSettings());
        AddSection(list, new SpectateHudSettingsSection());
        AddSection(list, new ReplayHudSettingsSection());
        AddSection(list, new EventsSettingsSection());
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

    private sealed class ZoomSettingsSection : UIPanel
    {
        private readonly UIText zoomLabel;
        private readonly Slider zoomSlider;

        private string currentText = "";

        public ZoomSettingsSection()
        {
            Width.Set(0f, 1f);
            Height.Set(62f, 0f);
            SetPadding(0f);

            BackgroundColor = new Color(33, 43, 79) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;

            zoomLabel = new UIText("", 0.86f)
            {
                Left = new StyleDimension(12f, 0f),
                Top = new StyleDimension(8f, 0f),
                TextColor = Color.White
            };

            zoomSlider = new Slider
            {
                Left = new StyleDimension(12f, 0f),
                Top = new StyleDimension(34f, 0f),
                Height = new StyleDimension(18f, 0f),
                HighlightColor = Main.OurFavoriteColor
            };

            zoomSlider.Width.Set(-24f, 1f);
            zoomSlider.SetRatio(ReplayClientSettings.ReplayZoomRatio);
            zoomSlider.OnDrag += ReplayClientSettings.SetReplayZoomRatio;
            zoomSlider.OnRelease += ReplayClientSettings.SetReplayZoomRatio;

            Append(zoomLabel);
            Append(zoomSlider);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!zoomSlider.IsHeld)
                zoomSlider.SetRatio(ReplayClientSettings.ReplayZoomRatio);

            zoomSlider.HighlightColor = Main.OurFavoriteColor;

            string text = $"Zoom: {ReplayClientSettings.ReplayZoomPercent}%";

            if (currentText == text)
                return;

            currentText = text;
            zoomLabel.SetText(text);
        }
    }

    private sealed class PlaybackHudWidthSettingsSection : UIPanel
    {
        private readonly UIText widthLabel;
        private readonly Slider widthSlider;

        private string currentText = "";

        public PlaybackHudWidthSettingsSection()
        {
            Width.Set(0f, 1f);
            Height.Set(62f, 0f);
            SetPadding(0f);

            BackgroundColor = new Color(33, 43, 79) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;

            widthLabel = new UIText("", 0.86f)
            {
                Left = new StyleDimension(12f, 0f),
                Top = new StyleDimension(8f, 0f),
                TextColor = Color.White
            };

            widthSlider = new Slider
            {
                Left = new StyleDimension(12f, 0f),
                Top = new StyleDimension(34f, 0f),
                Height = new StyleDimension(18f, 0f),
                HighlightColor = Main.OurFavoriteColor
            };

            widthSlider.Width.Set(-24f, 1f);
            widthSlider.SetRatio(ReplayClientSettings.PlaybackHudWidthRatio);
            widthSlider.OnDrag += ReplayClientSettings.SetPlaybackHudWidthRatio;
            widthSlider.OnRelease += ReplayClientSettings.SetPlaybackHudWidthRatio;

            Append(widthLabel);
            Append(widthSlider);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!widthSlider.IsHeld)
                widthSlider.SetRatio(ReplayClientSettings.PlaybackHudWidthRatio);

            widthSlider.HighlightColor = Main.OurFavoriteColor;

            string text = $"Playback HUD Width: {ReplayClientSettings.PlaybackHudWidthPercent}%";

            if (currentText == text)
                return;

            currentText = text;
            widthLabel.SetText(text);
        }
    }

    //private sealed class DisplaySettings : SettingsSection
    //{
    //    public override string HeaderText => "Display Settings";
    //    public override float Height => 86f;

    //    public override IReadOnlyList<SpectatorSectionRow> GetRows()
    //    {
    //        return
    //        [
    //            new("Compact HUD:", () => $"Compact HUD: {OnOff(ReplayClientSettings.IsCompactModeOn)}", GetRightClickTeleportIcon, onLeftClick: ReplayClientSettings.ToggleCompactMode)
    //        ];
    //    }

    //    private static Texture2D GetRightClickTeleportIcon()
    //    {
    //        return ReplayClientSettings.IsCompactModeOn ? Ass.IconCard1.Value : Ass.IconCard3.Value;
    //    }
    //}

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

    private sealed class SpectateHudSettingsSection : SettingsSection
    {
        public override string HeaderText => "Spectate HUD Settings";
        public override float Height => 244f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Show Spectate HUD:", () => $"Show Spectate HUD: {OnOff(ReplayClientSettings.ShowSpectateHud)}", () => Ass.IconEye.Value, onLeftClick: ReplayClientSettings.ToggleShowSpectateHud),
                new("Rows Visible:", () => $"Rows Visible: {SpectateHudClientSettings.RowsVisible}", () => Ass.IconResize.Value, onLeftClick: SpectateHudClientSettings.CycleRowsVisible),
                new("Sort By:", () => $"Sort By: {SpectateHudClientSettings.SortModeDisplayName}", () => Ass.IconRefresh.Value, onLeftClick: SpectateHudClientSettings.CycleSortMode),
                new("Show Player:", () => $"Show Player: {OnOff(SpectateHudClientSettings.ShowPlayer)}", () => Ass.IconPlayer.Value, onLeftClick: SpectateHudClientSettings.ToggleShowPlayer, iconScale: 1.5f),
                new("Show Name/Distance:", () => $"Show Name/Distance: {OnOff(SpectateHudClientSettings.ShowPlayerNameAndDistance)}", () => Ass.IconPlayerHead.Value, onLeftClick: SpectateHudClientSettings.ToggleShowPlayerNameAndDistance, iconScale: 0.8f),
                new("Show Description:", () => $"Show Description: {OnOff(SpectateHudClientSettings.ShowDescription)}", () => Ass.IconEye.Value, onLeftClick: SpectateHudClientSettings.ToggleShowDescription)
            ];
        }
    }

    private sealed class ReplayHudSettingsSection : SettingsSection
    {
        public override string HeaderText => "Replay HUD Settings";
        public override float Height => 180f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Show Replay HUD:", () => $"Show Replay HUD: {OnOff(ReplayClientSettings.ShowPlaybackHud)}", () => Ass.IconEye.Value, onLeftClick: ReplayClientSettings.ToggleShowPlaybackHud),
                new("Show Speed:", () => $"Show Speed: {OnOff(ReplayClientSettings.ShowReplayHudSpeed)}", () => Ass.IconSpeedUp.Value, onLeftClick: ReplayClientSettings.ToggleShowReplayHudSpeed),
                new("Show Playback Controls:", () => $"Show Playback Controls: {OnOff(ReplayClientSettings.ShowReplayHudPlaybackControls)}", () => Ass.IconPlay.Value, onLeftClick: ReplayClientSettings.ToggleShowReplayHudPlaybackControls),
                new("Show Seekbar:", () => $"Show Seekbar: {OnOff(ReplayClientSettings.ShowReplayHudSeekbar)}", () => Ass.SliderHighlight.Value, onLeftClick: ReplayClientSettings.ToggleShowReplayHudSeekbar)
            ];
        }
    }

    private sealed class EventsSettingsSection : SettingsSection
    {
        public override string HeaderText => "Events";
        public override float Height => 146f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Show Bosses Defeated:", () => $"Show Bosses Defeated: {OnOff(ReplayClientSettings.ShowBossesDefeated)}", () => Ass.IconCheckmarkGreen.Value, onLeftClick: ReplayClientSettings.ToggleShowBossesDefeated),
                new("Show Player Deaths:", () => $"Show Player Deaths: {OnOff(ReplayClientSettings.ShowPlayerDeaths)}", () => TextureAssets.MapDeath.Value, onLeftClick: ReplayClientSettings.ToggleShowPlayerDeaths),
                new("Show Invasions:", () => $"Show Invasions: {OnOff(ReplayClientSettings.ShowInvasions)}", () => Ass.IconSword.Value, onLeftClick: ReplayClientSettings.ToggleShowInvasions)
            ];
        }
    }
}
