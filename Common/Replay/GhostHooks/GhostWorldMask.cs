using Reese.Common.Spectator;
using System.Collections.Generic;

namespace Reese.Common.Replay.GhostHooks;

/// <summary>
/// Temporarily clears <c>Player.active</c> on ghosts so vanilla world logic cannot see them.
/// <para/>
/// Every vanilla loop that lets a ghost influence spawning, despawning, invasion size or the blood moon
/// roll tests <c>Main.player[i].active</c> and nothing else, so this is the single lever that closes all
/// of them. Callers are expected to pair <see cref="Push"/> with <see cref="Pop"/> in a finally block.
/// <para/>
/// Reference counted because masked scopes nest: NPC AI calls Main.StartInvasion when a Martian Probe
/// escapes (NPC.cs:32825), which happens inside the NPC phase that <see cref="GhostSpawnMaskSystem"/>
/// has already masked.
/// </summary>
internal static class GhostWorldMask
{
    // Player references, not slot indices: a client can drop mid-scope, and RemoteClient.Reset assigns a
    // brand new Player to that slot. Restoring by reference touches the orphan, not the incoming player.
    private static readonly List<Player> masked = [];
    private static int depth;

    /// <param name="onlyIfRealPlayerRemains">
    /// Skip masking when every active player is a ghost. Needed by callers like Player.FindClosest that
    /// promise to return an active player index and fall back to slot 0 when they find nobody; without
    /// this they would hand out an inactive player once the whole server is spectating.
    /// <para/>
    /// Only consulted at depth 0. Depth still increments either way, so <see cref="Pop"/> stays balanced.
    /// </param>
    public static void Push(bool onlyIfRealPlayerRemains = false)
    {
        if (depth++ > 0)
            return;

        if (onlyIfRealPlayerRemains && !AnyRealPlayerActive())
            return;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (!SpectatorMode.IsSpectatingForWorldLogic(player))
                continue;

            player.active = false;
            masked.Add(player);
        }
    }

    public static void Pop()
    {
        if (depth == 0 || --depth > 0)
            return;

        Unmask();
    }

    /// <summary>Drops a mask that an exception left behind by skipping its Pop.</summary>
    public static void Reset()
    {
        depth = 0;
        Unmask();
    }

    private static bool AnyRealPlayerActive()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player.active && !SpectatorMode.IsSpectatingForWorldLogic(player))
                return true;
        }

        return false;
    }

    private static void Unmask()
    {
        foreach (Player player in masked)
            player.active = true;

        masked.Clear();
    }
}
