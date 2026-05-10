using Reese.Common.ReplaySpectate.UI.Tabs.WorldTab;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ReplayMetadata = global::Reese.Common.Replayer.ReplayMetadata;
using ReplayRuntime = global::Reese.Common.Replayer.Replayer;
using ReplaySession = global::Reese.Common.Replayer.ReplaySession;

namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;

internal sealed class ReplayInfoSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.ReplayInfo;
    public override string HeaderText => "Replay info";
    public override float Height => 650f;
    public override bool UsesCommonRowTooltips => true;

    public override IReadOnlyList<WorldSectionRow> GetRows()
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

    private static ReplayMetadata Metadata => ReplayRuntime.ActiveMetadata;

    private static string GetFileText() => "File: " + (string.IsNullOrWhiteSpace(ReplaySession.CurrentPath) ? "-" : Path.GetFileName(ReplaySession.CurrentPath));

    private static string GetFormatText() => $"Format: v{Metadata?.FormatVersion ?? 0}";

    private static string GetCreatedText()
    {
        string created = Metadata?.CreatedUtc;
        if (DateTime.TryParse(created, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date))
            return "Created: " + date.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return "Created: -";
    }

    private static string GetPlayerText() => "Player: " + EmptyToDash(Metadata?.PlayerName);

    private static string GetWorldText() => "World: " + EmptyToDash(Metadata?.WorldName);

    private static string GetWorldIdText() => $"World ID: {Metadata?.WorldId ?? 0}";

    private static string GetTickRateText() => $"Tick Rate: {Metadata?.TickRate ?? 0}";

    private static string GetDurationText()
    {
        int tickRate = Math.Max(1, Metadata?.TickRate ?? 60);
        uint ticks = Metadata?.DurationTicks > 0 ? Metadata.DurationTicks : ReplayRuntime.ActiveDurationTicks;
        TimeSpan duration = TimeSpan.FromSeconds(ticks / (double)tickRate);
        return $"Duration: {(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private static string GetBlocksText() => $"Blocks: {Metadata?.BlockCount ?? 0:N0}";

    private static string GetPacketsText() => $"Packets: {Metadata?.PacketCount ?? 0:N0}";

    private static string GetPacketBytesText() => $"Packet Bytes: {Metadata?.PacketDataBytes ?? 0:N0}";

    private static string GetBaselineBytesText() => $"Baseline Bytes: {Metadata?.BaselineBytes ?? 0:N0}";

    private static string GetMalformedText() => $"Malformed Packets: {Metadata?.MalformedPacketDataCount ?? 0:N0}";

    private static string GetFinalizedText() => $"Finalized: {(Metadata?.Finalized == true ? "Yes" : "No")}";

    private static string GetEndReasonText() => "End Reason: " + EmptyToDash(Metadata?.EndReason);

    private static string GetModVersionText() => "Mod Version: " + EmptyToDash(Metadata?.ModVersion);

    private static string GetTmlVersionText() => "tML Version: " + EmptyToDash(Metadata?.TmlVersion);

    private static string GetModsText()
    {
        string[] mods = Metadata?.ModNames ?? [];
        return mods.Length == 0 ? "Mods: -" : $"Mods ({mods.Length:N0}): {string.Join(", ", mods)}";
    }

    private static string EmptyToDash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
}
