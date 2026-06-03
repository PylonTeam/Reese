using System.Collections.Generic;
using System.Linq;

namespace Reese.Common.Replayer.ReplayEvents;

internal static class ReplayTimelineRecorder
{
    private static readonly List<ReplayTimelineEvent> events = [];
    private static bool active;

    public static void Begin()
    {
        events.Clear();
        active = true;
    }

    public static ReplayTimelineEvent[] Finish()
    {
        active = false;

        return [.. events
            .OrderBy(e => e.Tick)
            .ThenBy(e => e.Category)
            .ThenBy(e => e.Key)];
    }

    public static void Cancel()
    {
        active = false;
        events.Clear();
    }

    public static void Add(ReplayTimelineEvent timelineEvent)
    {
        if (!active)
            return;

        timelineEvent = Normalize(timelineEvent);

        if (events.Any(e => e.Tick == timelineEvent.Tick && e.Category == timelineEvent.Category && e.Key == timelineEvent.Key && e.Text == timelineEvent.Text))
            return;

        events.Add(timelineEvent);
    }

    public static void RecordPlayerDeath(Player player, uint tick)
    {
        if (player == null || player.whoAmI == ReplayPlayback.RecordClientIndex)
            return;

        string playerName = GetPlayerName(player);
        Add(new ReplayTimelineEvent(tick, ReplayEventCategory.PlayerDeath, $"player-death:{player.whoAmI}:{tick}", $"{playerName} died", ReplayEventIconKind.MapDeath));
    }

    public static void RecordActivePlayersJoined(uint tick)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player?.active == true)
                RecordPlayerJoined(player, tick);
        }
    }

    public static void RecordPlayerJoined(Player player, uint tick)
    {
        if (player == null || player.whoAmI == ReplayPlayback.RecordClientIndex)
            return;

        string playerName = GetPlayerName(player);
        Add(new ReplayTimelineEvent(
            tick,
            ReplayEventCategory.PlayerJoined,
            $"player-join:{player.whoAmI}:{tick}",
            $"{playerName} joined",
            ReplayEventIconKind.PlayerHead,
            player.whoAmI,
            ReplayPlayerHeadSnapshot.FromPlayer(player)));
    }

    public static void RecordPlayerLeft(Player player, uint tick)
    {
        if (player == null || player.whoAmI == ReplayPlayback.RecordClientIndex)
            return;

        string playerName = GetPlayerName(player);
        Add(new ReplayTimelineEvent(
            tick,
            ReplayEventCategory.PlayerLeft,
            $"player-left:{player.whoAmI}:{tick}",
            $"{playerName} left",
            ReplayEventIconKind.PlayerHead,
            player.whoAmI,
            ReplayPlayerHeadSnapshot.FromPlayer(player)));
    }

    private static ReplayTimelineEvent Normalize(ReplayTimelineEvent timelineEvent)
    {
        string key = string.IsNullOrWhiteSpace(timelineEvent.Key) ? $"{timelineEvent.Category}:{timelineEvent.Tick}" : timelineEvent.Key.Trim();
        string text = string.IsNullOrWhiteSpace(timelineEvent.Text) ? timelineEvent.Category.ToString() : timelineEvent.Text.Trim();

        return timelineEvent with
        {
            Key = key,
            Text = text
        };
    }

    private static string GetPlayerName(Player player)
    {
        return string.IsNullOrWhiteSpace(player?.name) ? $"Player {(player?.whoAmI ?? 0) + 1}" : player.name.Trim();
    }
}
