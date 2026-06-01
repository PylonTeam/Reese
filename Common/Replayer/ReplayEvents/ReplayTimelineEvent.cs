namespace Reese.Common.Replayer.ReplayEvents;

public enum ReplayEventCategory : byte
{
    BossDefeated = 1,
    PlayerDeath = 2,
    InvasionStarted = 3,
    Custom = 255
}

public enum ReplayEventIconKind : byte
{
    None = 0,
    BossHead = 1,
    MapDeath = 2,
    Item = 3
}

public readonly record struct ReplayTimelineEvent(
    uint Tick,
    ReplayEventCategory Category,
    string Key,
    string Text,
    ReplayEventIconKind IconKind = ReplayEventIconKind.None,
    int IconId = 0);
