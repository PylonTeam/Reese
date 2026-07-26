using Reese.Common.Spectator;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Stops a ghost from generating natural NPC spawns of its own.
/// <para/>
/// NPC.SpawnNPC runs one iteration per active, non-dead player and gates it on
/// <c>nearbyActiveNPCs &lt; maxSpawns &amp;&amp; Main.rand.Next(spawnRate) == 0</c>, with EditSpawnRate as the
/// last edit before that gate. <see cref="GhostSpawnMaskSystem"/> already stops the iteration from
/// happening at all; this stays as a cheap, mod-friendly backstop in case anything detours around the mask.
/// </summary>
internal sealed class GhostSpawnRateNPC : GlobalNPC
{
    // Not int.MaxValue: later GlobalNPCs are free to multiply spawnRate, and overflowing to a negative
    // value makes Main.rand.Next(spawnRate) throw, which aborts the spawn cycle for every player.
    private const int NoSpawnRate = int.MaxValue / 8;

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!SpectatorMode.IsSpectatingForWorldLogic(player))
            return;

        // maxSpawns is the hard gate, nearbyActiveNPCs is never negative; spawnRate is belt and braces.
        spawnRate = NoSpawnRate;
        maxSpawns = 0;
    }
}
