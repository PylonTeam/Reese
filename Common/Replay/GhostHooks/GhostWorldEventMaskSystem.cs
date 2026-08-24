using Reese.Common.Spectator;

namespace Reese.Common.Replay.GhostHooks;

/// <summary>
/// Hides ghosts from the vanilla player-array scans that live outside the NPC phase.
/// <para/>
/// Player.Update returns into Player.Ghost before it touches tiles, pressure plates or GrabItems, so a
/// ghost cannot act on the world directly. What leaks is code that iterates the player array from the
/// outside. <see cref="GhostSpawnMaskSystem"/> covers the NPC phase; these are the rest, and none of them
/// filter on ghost, so a spectator currently counts as a participant.
/// <para/>
/// Main.UpdateTime is masked wholesale because it holds four separate leaks and every one of its player
/// scans should ignore spectators:
/// <list type="bullet">
/// <item>boss target selection picks the first active, non-dead, surface player and drops the Eye of
/// Cthulhu (Main.cs:62883), the mech bosses (62907) or Deerclops (62939) on them — a ghost at a low slot
/// index wins and the boss lands on someone who cannot fight it. Masking makes vanilla's own loop fall
/// through to the next eligible player, and leaves WorldGen.spawnEye set to retry rather than consuming it</item>
/// <item>Moon Lord spawns on Player.FindClosest when the countdown expires (62833)</item>
/// <item>UpdateTime_StartNight rolls the blood moon against any player with 2+ life crystals, and the Eye
/// of Cthulhu against any with 5+ crystals and more than 10 defense</item>
/// <item>AnyPlayerReadyToFightKingSlime gates natural slime rain: when false the roll is 5x rarer and,
/// outside expert mode, blocked outright. Slime rain spawns via NPC.SlimeRainSpawns, which runs before
/// EditSpawnRate and cannot be vetoed by it</item>
/// </list>
/// Everything else UpdateTime touches was checked and is unaffected: UpdateTime_SpawnTownNPCs counts
/// players into a variable vanilla never reads, WorldGen.SpawnTravelNPC only checks that the merchant
/// would not land inside a player, and the pylon, sandstorm, party, lantern and cultist helpers use
/// direct <c>Main.player[index]</c> access rather than active scans.
/// </summary>
[Autoload(Side = ModSide.Both)]
internal sealed class GhostWorldEventMaskSystem : ModSystem
{
    public override void Load()
    {
        On_Main.UpdateTime += OnUpdateTime;
        On_Main.CanStartInvasion += OnCanStartInvasion;
        On_Main.StartInvasion += OnStartInvasion;
        On_Player.FindClosest += OnFindClosest;
        On_NPC.SpawnOnPlayer += OnSpawnOnPlayer;
    }

    public override void Unload()
    {
        On_Main.UpdateTime -= OnUpdateTime;
        On_Main.CanStartInvasion -= OnCanStartInvasion;
        On_Main.StartInvasion -= OnStartInvasion;
        On_Player.FindClosest -= OnFindClosest;
        On_NPC.SpawnOnPlayer -= OnSpawnOnPlayer;
    }

    private static void OnUpdateTime(On_Main.orig_UpdateTime orig)
    {
        GhostWorldMask.Push();

        try
        {
            orig();
        }
        finally
        {
            GhostWorldMask.Pop();
        }
    }

    /// <summary>
    /// Still needed separately from <see cref="OnUpdateTime"/>: invasions also start from a boss summon
    /// item (Player.cs:40253), from the server handling that item use (MessageBuffer.cs:2807/2881), and
    /// from NPC AI when a Martian Probe escapes (NPC.cs:32825).
    /// </summary>
    private static void OnStartInvasion(On_Main.orig_StartInvasion orig, int type)
    {
        GhostWorldMask.Push();

        try
        {
            orig(type);
        }
        finally
        {
            GhostWorldMask.Pop();
        }
    }

    private static bool OnCanStartInvasion(On_Main.orig_CanStartInvasion orig, int type, bool ignoreDelay)
    {
        GhostWorldMask.Push();

        try
        {
            return orig(type, ignoreDelay);
        }
        finally
        {
            GhostWorldMask.Pop();
        }
    }

    /// <summary>
    /// Player.FindClosest (Player.cs:5372) filters on active and dead but not ghost, so a spectator can be
    /// "the closest player" for its 29 vanilla callers. The costly ones are the luck lookups
    /// (Player.cs:16247-16275), which feed drop rates, ore and gem generation and fishing: a ghost parked
    /// near a real player silently substitutes its own luck for theirs.
    /// <para/>
    /// Masked with the real-player guard because this method contractually returns an active index.
    /// </summary>
    private static byte OnFindClosest(On_Player.orig_FindClosest orig, Vector2 position, int width, int height)
    {
        GhostWorldMask.Push(onlyIfRealPlayerRemains: true);

        try
        {
            return orig(position, width, height);
        }
        finally
        {
            GhostWorldMask.Pop();
        }
    }

    /// <summary>
    /// Backstop for the one caller that cannot be fixed by masking alone. Moon Lord spawns on
    /// <c>Player.FindClosest(...)</c> (Main.cs:62833), and FindClosest falls back to slot 0 when it matches
    /// nobody. With every player spectating that fallback is an inactive player sitting at the world
    /// origin, and SpawnOnPlayer would place the boss there off <c>Main.player[plr].Center</c>.
    /// <para/>
    /// Dropping the spawn loses that Moon Lord, which only happens if literally no real player is online
    /// at the tick the countdown expires. That is strictly better than corrupting the world.
    /// </summary>
    private static void OnSpawnOnPlayer(On_NPC.orig_SpawnOnPlayer orig, int plr, int type)
    {
        if (plr < 0 || plr >= Main.maxPlayers)
            return;

        Player player = Main.player[plr];

        if (player?.active != true || SpectatorMode.IsSpectatingForWorldLogic(player))
            return;

        orig(plr, type);
    }
}
