using Terraria.ID;

namespace Reese.Common.Replayer.ReplayEvents;

internal static class ReplayInvasionDefinitions
{
    public static ReplayTimelineEvent CreateEvent(uint tick, int invasionType)
    {
        string name = GetName(invasionType);
        int itemId = GetIconItemId(invasionType);
        ReplayEventIconKind iconKind = itemId > 0 ? ReplayEventIconKind.Item : ReplayEventIconKind.None;

        return new ReplayTimelineEvent(tick, ReplayEventCategory.InvasionStarted, $"invasion:{invasionType}", $"{name} started", iconKind, itemId);
    }

    private static string GetName(int invasionType)
    {
        return invasionType switch
        {
            InvasionID.GoblinArmy => "Goblin Army",
            InvasionID.SnowLegion => "Snow Legion",
            InvasionID.PirateInvasion => "Pirate Invasion",
            InvasionID.MartianMadness => "Martian Madness",
            _ => $"Invasion {invasionType}"
        };
    }

    private static int GetIconItemId(int invasionType)
    {
        return invasionType switch
        {
            InvasionID.GoblinArmy => ItemID.GoblinBattleStandard,
            InvasionID.SnowLegion => ItemID.SnowGlobe,
            InvasionID.PirateInvasion => ItemID.PirateMap,
            InvasionID.MartianMadness => ItemID.CosmicCarKey,
            _ => 0
        };
    }
}
