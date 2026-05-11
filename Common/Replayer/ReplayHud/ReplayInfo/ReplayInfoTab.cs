using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Terraria.GameContent.UI.Elements;

namespace Reese.Common.Replayer.ReplayHud.ReplayInfo;

internal sealed class ReplayInfoTab : TabPage
{
    public override Shared.Tabs.SpectatorTab Tab => Shared.Tabs.SpectatorTab.Replay;
    public override string HeaderText => "Replay";
    public override string TooltipText => "Replay info";
    public override Asset<Texture2D> Icon => Ass.Icon_CameraSmall;

    public override float IconScale => 1.25f;

    public override Vector2 IconOffset => new Vector2(0,0);

    protected override void Populate(UIList list)
    {
        AddSection(list, new ReplayInfo());
    }

    private sealed class ReplayInfo : Shared.Sections.InfoSection
    {
        public override string HeaderText => "Replay Info";
        public override float Height => 248f;

        public override IReadOnlyList<Shared.Sections.SpectatorSectionRow> GetRows()
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