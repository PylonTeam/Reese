using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplaySpectate.Hooks;
using Reese.Common.Replayer.ReplaySpectate.UI.Sections;
using Reese.Common.Replayer.ReplaySpectate.UI.Settings;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplaySpectate.UI.Tabs;

internal sealed class ReplayTab : TabPage
{
    public override SpectatorTab Tab => SpectatorTab.Replay;
    public override string HeaderText => "Replay";
    public override string TooltipText => "Replay settings";
    public override Asset<Texture2D> Icon => Ass.Icon_CameraSmall;

    public override float IconScale => 1.25f;

    public override Vector2 IconOffset => new Vector2(0,0);

    protected override void Populate(UIList list)
    {
        AddSection(list, new ReplaySettings());
        AddSection(list, new ReplayInfo());
        AddSection(list, new DrawSettings());
    }

    private sealed class DrawSettings : SettingsSection
    {
        public override string HeaderText => "Draw Settings";
        public override float Height => 214f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
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

        private static Texture2D GetProjectileIcon()
        {
            return TextureAssets.Projectile[ProjectileID.WoodenArrowFriendly].Value;
        }

        private static Texture2D GetItemIcon()
        {
            return TextureAssets.Item[ItemID.GoldCoin].Value;
        }
    }

    private sealed class ReplaySettings : SettingsSection
    {
        public override string HeaderText => "Replay Settings";
        public override float Height => 146f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("Fullbright:", () => $"Fullbright: {OnOff(FullbrightSpectatorSystem.Enabled)}", GetFullbrightIcon, onLeftClick: () => FullbrightSpectatorSystem.Enabled = !FullbrightSpectatorSystem.Enabled),
                new("Reveal Map:", () => $"Reveal Map: {OnOff(MapRevealHelper.Revealed)}", GetRevealMapIcon, onLeftClick: () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
                new("Right Click Teleport:", () => $"Right Click Teleport: {OnOff(SpectatorClientSettings.RightClickTeleport)}", GetRightClickTeleportIcon, onLeftClick: SpectatorClientSettings.ToggleRightClickTeleport)
            ];
        }

        private static Texture2D GetFullbrightIcon()
        {
            return FullbrightSpectatorSystem.Enabled ? Ass.Icon_CandelabraOn.Value : Ass.Icon_CandelabraOff.Value;
        }

        private static Texture2D GetRevealMapIcon()
        {
            return MapRevealHelper.Revealed ? Ass.Icon_MapOn.Value : Ass.Icon_MapOff.Value;
        }

        private static Texture2D GetRightClickTeleportIcon()
        {
            return SpectatorClientSettings.RightClickTeleport ? Ass.Icon_TeleportOn.Value : Ass.Icon_TeleportOff.Value;
        }
    }

    private sealed class ReplayInfo : InfoSection
    {
        public override string HeaderText => "Replay Info";
        public override float Height => 248f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(GetFileText(), GetFileText),
                new(GetRecordedText(), GetRecordedText),
                new(GetPlaybackText(), GetPlaybackText),
                new(GetLengthText(), GetLengthText),
                new(GetWorldText(), GetWorldText),
                new(GetModsText(), GetModsText, tooltip: GetModsTooltip())
                ];
        }

        private static ReplayMetadata Metadata => Replayer.ActiveMetadata;

        private static string GetFileText()
        {
            return "File: " + (string.IsNullOrWhiteSpace(ReplaySession.CurrentPath) ? "-" : Path.GetFileName(ReplaySession.CurrentPath));
        }

        private static string GetRecordedText()
        {
            string created = Metadata?.CreatedUtc;
            if (DateTime.TryParse(created, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date))
                return "Recorded: " + date.ToLocalTime().ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture);

            return "Recorded: -";
        }

        private static string GetPlaybackText()
        {
            uint currentTick = Replayer.CurrentTick;
            uint durationTicks = GetDurationTicks();
            int tickRate = GetTickRate();

            if (durationTicks == 0)
                return $"Playback: {FormatDuration(currentTick, tickRate)}";

            return $"Playback: {FormatDuration(currentTick, tickRate)} / {FormatDuration(durationTicks, tickRate)}";
        }

        private static string GetLengthText()
        {
            uint durationTicks = GetDurationTicks();
            return "Length: " + (durationTicks == 0 ? "-" : FormatDuration(durationTicks, GetTickRate()));
        }

        private static string GetWorldText()
        {
            return "World: " + EmptyToDash(Metadata?.WorldName);
        }

        private static string GetModsText()
        {
            string[] mods = Metadata?.ModNames ?? [];
            return mods.Length == 0 ? "Mods: -" : $"Mods: {mods.Length:N0} loaded";
        }

        private static string GetModsTooltip()
        {
            string[] mods = Metadata?.ModNames ?? [];

            if (mods.Length == 0)
                return "Mods: -";

            return "Mods:\n" + string.Join(", ", mods);
        }

        private static uint GetDurationTicks()
        {
            return Metadata?.DurationTicks > 0 ? Metadata.DurationTicks : Replayer.ActiveDurationTicks;
        }

        private static int GetTickRate()
        {
            return Math.Max(1, Metadata?.TickRate ?? 60);
        }

        private static string FormatDuration(uint ticks, int tickRate)
        {
            TimeSpan duration = TimeSpan.FromSeconds(ticks / (double)tickRate);
            return duration.TotalHours >= 1d
                ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes:00}:{duration.Seconds:00}";
        }

        private static string EmptyToDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }
}