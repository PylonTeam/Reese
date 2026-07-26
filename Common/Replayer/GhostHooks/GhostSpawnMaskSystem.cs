using Terraria.ID;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Hides ghosts from the whole vanilla NPC phase so they cannot influence spawning or despawning.
/// <para/>
/// Masking <c>Player.active</c> (see <see cref="GhostWorldMask"/>) is what actually turns a ghost into a
/// spectator here, because these loops test nothing else:
/// <list type="bullet">
/// <item>the active-player count that scales maxSpawns during invasions and the pumpkin/frost moons</item>
/// <item>the per-player spawn loop, and NPC.SlimeRainSpawns which runs before EditSpawnRate can veto it</item>
/// <item>the check that rejects a spawn tile landing inside <i>any</i> player's safe rectangle, roughly
/// 2044x1150 pixels, which is how a ghost currently suppresses spawns around real players</item>
/// <item>NPC.CheckActive, where a ghost otherwise refreshes timeLeft and keeps enemies alive forever</item>
/// </list>
/// The mask spans PreUpdateNPCs to PostUpdateNPCs because that window contains NPC.SpawnNPC, the
/// nearbyActiveNPCs/townNPCs reset, and the NPC.UpdateNPC loop that calls CheckActive.
/// <para/>
/// Only applied where spawning is decided. A multiplayer client never runs NPC.SpawnNPC and never
/// despawns an NPC itself, so masking there would only desync local prediction for no benefit.
/// </summary>
[Autoload(Side = ModSide.Both)]
internal sealed class GhostSpawnMaskSystem : ModSystem
{
    public override void PreUpdateNPCs()
    {
        // NPC AI exceptions are swallowed per-NPC by Main.ignoreErrors, so never trust the previous frame.
        GhostWorldMask.Reset();

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        GhostWorldMask.Push();
    }

    public override void PostUpdateNPCs()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        GhostWorldMask.Pop();
    }

    public override void OnWorldUnload() => GhostWorldMask.Reset();
}
