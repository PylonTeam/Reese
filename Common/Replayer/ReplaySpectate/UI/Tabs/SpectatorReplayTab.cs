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

internal sealed class SpectatorReplayTab : TabPage
{
    public override SpectatorTab Tab => SpectatorTab.Replay;
    public override string HeaderText => "Replay";
    public override string TooltipText => "Replay settings";
    public override Asset<Texture2D> Icon => Ass.Icon_CameraSmall;

    protected override void Populate(UIList list)
    {
        AddSection(list, new DrawSettings());
        AddSection(list, new ReplaySettings());
        AddSection(list, new ReplayInfo());
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
        public override string HeaderText => "Replay info";
        public override float Height => 650f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(GetFileText(), GetFileText),
                new(GetFormatText(), GetFormatText),
                new(GetCreatedText(), GetCreatedText),
                new(GetPlayerText(), GetPlayerText),
                new(GetWorldText(), GetWorldText),
                new(GetWorldIdText(), GetWorldIdText),
                new(GetTickRateText(), GetTickRateText),
                new(GetDurationText(), GetDurationText),
                new(GetBlocksText(), GetBlocksText),
                new(GetPacketsText(), GetPacketsText),
                new(GetPacketBytesText(), GetPacketBytesText),
                new(GetBaselineBytesText(), GetBaselineBytesText),
                new(GetMalformedText(), GetMalformedText),
                new(GetFinalizedText(), GetFinalizedText),
                new(GetEndReasonText(), GetEndReasonText),
                new(GetModVersionText(), GetModVersionText),
                new(GetTmlVersionText(), GetTmlVersionText),
                new(GetModsText(), GetModsText)
            ];
        }

        private static ReplayMetadata Metadata => Replayer.ActiveMetadata;

        private static string GetFileText()
        {
            return "File: " + (string.IsNullOrWhiteSpace(ReplaySession.CurrentPath) ? "-" : Path.GetFileName(ReplaySession.CurrentPath));
        }

        private static string GetFormatText()
        {
            return $"Format: v{Metadata?.FormatVersion ?? 0}";
        }

        private static string GetCreatedText()
        {
            string created = Metadata?.CreatedUtc;
            if (DateTime.TryParse(created, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date))
                return "Created: " + date.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            return "Created: -";
        }

        private static string GetPlayerText()
        {
            return "Player: " + EmptyToDash(Metadata?.PlayerName);
        }

        private static string GetWorldText()
        {
            return "World: " + EmptyToDash(Metadata?.WorldName);
        }

        private static string GetWorldIdText()
        {
            return $"World ID: {Metadata?.WorldId ?? 0}";
        }

        private static string GetTickRateText()
        {
            return $"Tick Rate: {Metadata?.TickRate ?? 0}";
        }

        private static string GetDurationText()
        {
            int tickRate = Math.Max(1, Metadata?.TickRate ?? 60);
            uint ticks = Metadata?.DurationTicks > 0 ? Metadata.DurationTicks : Replayer.ActiveDurationTicks;
            TimeSpan duration = TimeSpan.FromSeconds(ticks / (double)tickRate);
            return $"Duration: {(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }

        private static string GetBlocksText()
        {
            return $"Blocks: {Metadata?.BlockCount ?? 0:N0}";
        }

        private static string GetPacketsText()
        {
            return $"Packets: {Metadata?.PacketCount ?? 0:N0}";
        }

        private static string GetPacketBytesText()
        {
            return $"Packet Bytes: {Metadata?.PacketDataBytes ?? 0:N0}";
        }

        private static string GetBaselineBytesText()
        {
            return $"Baseline Bytes: {Metadata?.BaselineBytes ?? 0:N0}";
        }

        private static string GetMalformedText()
        {
            return $"Malformed Packets: {Metadata?.MalformedPacketDataCount ?? 0:N0}";
        }

        private static string GetFinalizedText()
        {
            return $"Finalized: {(Metadata?.Finalized == true ? "Yes" : "No")}";
        }

        private static string GetEndReasonText()
        {
            return "End Reason: " + EmptyToDash(Metadata?.EndReason);
        }

        private static string GetModVersionText()
        {
            return "Mod Version: " + EmptyToDash(Metadata?.ModVersion);
        }

        private static string GetTmlVersionText()
        {
            return "tML Version: " + EmptyToDash(Metadata?.TmlVersion);
        }

        private static string GetModsText()
        {
            string[] mods = Metadata?.ModNames ?? [];
            return mods.Length == 0 ? "Mods: -" : $"Mods ({mods.Length:N0}): {string.Join(", ", mods)}";
        }

        private static string EmptyToDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }
}