using Reese.Common.Replay.ReplayHud;
using Terraria.ID;
using Terraria.Localization;

namespace Reese.Common.Replay.ReplayEvents;

internal static class ReplayInvasionDefinitions
{
    public static ReplayTimelineEvent CreateEvent(uint tick, int invasionType)
    {
        string name = GetName(invasionType);
        int itemId = GetIconItemId(invasionType);
        ReplayEventIconKind iconKind = itemId > 0 ? ReplayEventIconKind.Item : ReplayEventIconKind.None;

        return new ReplayTimelineEvent(tick, ReplayEventCategory.InvasionStarted, $"invasion:{invasionType}", Loc.Get("ReplayHud.Events.InvasionStarted", name), iconKind, itemId);
    }

    private static string GetName(int invasionType)
    {
        return invasionType switch
        {
            InvasionID.GoblinArmy => Language.GetTextValue("LegacyInterface.88"),
            InvasionID.SnowLegion => Language.GetTextValue("LegacyInterface.87"),
            InvasionID.PirateInvasion => Language.GetTextValue("LegacyInterface.86"),
            InvasionID.MartianMadness => Language.GetTextValue("LegacyInterface.85"),
            _ => Loc.Get("ReplayHud.Events.Invasion.Unknown", invasionType)
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
