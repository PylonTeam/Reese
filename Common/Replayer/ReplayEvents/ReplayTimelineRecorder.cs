using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Reese.Common.Replayer.ReplayHud;
using Terraria.DataStructures;
using Terraria.ID;

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

    public static void RecordPlayerDeath(Player player, PlayerDeathReason damageSource, uint tick)
    {
        if (player == null || player.whoAmI == ReplayPlayback.RecordClientIndex)
            return;

        if (TryRecordPlayerKill(player, damageSource, tick))
            return;

        string playerName = GetPlayerName(player);
        Add(new ReplayTimelineEvent(tick, ReplayEventCategory.PlayerDeath, $"player-death:{player.whoAmI}:{tick}", Loc.Get("ReplayHud.Events.PlayerDied", playerName), ReplayEventIconKind.MapDeath));
    }

    private static bool TryRecordPlayerKill(Player killedPlayer, PlayerDeathReason damageSource, uint tick)
    {
        int killerIndex = damageSource.SourcePlayerIndex;
        if (killerIndex < 0 ||
            killerIndex >= Main.maxPlayers ||
            killerIndex == killedPlayer.whoAmI ||
            killerIndex == ReplayPlayback.RecordClientIndex)
            return false;

        Player killerPlayer = Main.player[killerIndex];
        if (killerPlayer == null || !killerPlayer.active)
            return false;

        string killerName = GetPlayerName(killerPlayer);
        string killedName = GetPlayerName(killedPlayer);
        int weaponItemId = GetKillWeaponItemId(damageSource);

        Add(new ReplayTimelineEvent(
            tick,
            ReplayEventCategory.PlayerKill,
            $"player-kill:{killerIndex}:{killedPlayer.whoAmI}:{tick}",
            Loc.Get("ReplayHud.Events.PlayerKilled", killerName, killedName),
            ReplayEventIconKind.PlayerHead,
            weaponItemId,
            ReplayPlayerHeadSnapshot.FromPlayer(killerPlayer)));

        return true;
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
            Loc.Get("ReplayHud.Events.PlayerJoined", playerName),
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
            Loc.Get("ReplayHud.Events.PlayerLeft", playerName),
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
        return string.IsNullOrWhiteSpace(player?.name) ? Loc.Get("ReplayHud.Events.FallbackPlayerName", (player?.whoAmI ?? 0) + 1) : player.name.Trim();
    }

    private static int GetKillWeaponItemId(PlayerDeathReason damageSource)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object sourceItem = typeof(PlayerDeathReason).GetField("SourceItem", flags)?.GetValue(damageSource)
                            ?? typeof(PlayerDeathReason).GetProperty("SourceItem", flags)?.GetValue(damageSource);

        return sourceItem switch
        {
            Item item when item.type > ItemID.None => item.type,
            int itemId when itemId > ItemID.None => itemId,
            short itemId when itemId > ItemID.None => itemId,
            _ => ItemID.Skull
        };
    }
}
