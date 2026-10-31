using Reese.Common.Replayer.GhostHooks;
using Reese.Common.Spectator;
using System;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.ID;

namespace Reese.Common.Replayer;

/// <summary>Excludes spectators from NPC AI, including cached targets and pending ranged attacks.</summary>
[Autoload(Side = ModSide.Both)]
internal sealed class GhostNPCTargetSystem : ModSystem
{
    private readonly record struct PlayerState(Player Player, bool Active, bool Dead);

    private static readonly List<PlayerState> maskedPlayers = [];
    private static int maskDepth;
    private static NPC currentNPC;
    private static bool enteredWithSpectatorTarget;
    private static bool preparingBlockedShot;

    public override void Load()
    {
        On_NPC.AI += RunNPCWithoutSpectators;
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += SpawnProjectile;
    }

    public override void Unload()
    {
        On_NPC.AI -= RunNPCWithoutSpectators;
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float -= SpawnProjectile;
        Reset();
    }

    public override void OnWorldLoad() => Reset();
    public override void OnWorldUnload() => Reset();

    private static void RunNPCWithoutSpectators(On_NPC.orig_AI orig, NPC npc)
    {
        NPC previousNPC = currentNPC;
        bool previousSpectatorTarget = enteredWithSpectatorTarget;
        currentNPC = npc;

        try
        {
            PushMask();
            enteredWithSpectatorTarget = HasSpectatorTarget(npc);
            if (enteredWithSpectatorTarget)
            {
                int previousTarget = npc.target;
                if (npc.SupportsNPCTargets)
                    npc.TargetClosestUpgraded(faceTarget: false);
                else
                    npc.TargetClosest(faceTarget: false);

                if (Main.netMode == NetmodeID.Server && npc.target != previousTarget)
                    npc.netUpdate = true;
            }

            orig(npc);
        }
        finally
        {
            PopMask();
            currentNPC = previousNPC;
            enteredWithSpectatorTarget = previousSpectatorTarget;
        }
    }

    private static bool IsSpectator(Player player, int slot) => player != null &&
        (player.ghost || SpectatorModeSystem.GetMode(slot) == SpectateMode.Spectator ||
            ReplayPlayback.IsReplayPlayback && slot == ReplayPlayback.RecordClientIndex);

    private static bool HasSpectatorTarget(NPC npc) => npc?.HasPlayerTarget == true &&
        IsSpectator(Main.player[npc.target], npc.target);

    private static void PushMask()
    {
        if (maskDepth++ > 0)
            return;

        for (int slot = 0; slot < Main.maxPlayers; slot++)
        {
            Player player = Main.player[slot];
            if (!IsSpectator(player, slot) || !player.active && !GhostWorldMask.IsMasked(player))
                continue;

            // The authoritative spawn mask may already have cleared active. Capture its
            // current value so this inner scope leaves that outer mask intact.
            maskedPlayers.Add(new PlayerState(player, player.active, player.dead));
            player.active = false;
            player.dead = true;
        }
    }

    private static void PopMask()
    {
        if (maskDepth == 0 || --maskDepth > 0)
            return;

        RestorePlayers();
    }

    private static void RestorePlayers()
    {
        foreach (PlayerState state in maskedPlayers)
        {
            state.Player.active = state.Active;
            state.Player.dead = state.Dead;
        }

        maskedPlayers.Clear();
    }

    private static int SpawnProjectile(
        On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float orig,
        IEntitySource source, float x, float y, float speedX, float speedY, int type, int damage,
        float knockBack, int owner, float ai0, float ai1, float ai2)
    {
        // A ranged attack can already be winding up when its player becomes a ghost.
        // Some vanilla AI (including Cultist Archers) fires without another dead check.
        bool invalidSpectatorTarget = HasSpectatorTarget(currentNPC) ||
            enteredWithSpectatorTarget && currentNPC?.HasValidTarget == false;
        if (currentNPC?.friendly != false || !invalidSpectatorTarget ||
            source?.GetType() != typeof(EntitySource_Parent) ||
            !ReferenceEquals(((EntitySource_Parent)source).Entity, currentNPC) ||
            !ContentSamples.ProjectilesByType.TryGetValue(type, out Projectile sample) || !sample.hostile)
            return orig(source, x, y, speedX, speedY, type, damage, knockBack, owner, ai0, ai1, ai2);

        // Terraria reserves an extra projectile slot beyond its simulation range. Return
        // an inactive, initialized instance there so callers can safely access the result,
        // without spawning, syncing or killing a real projectile.
        int spareSlot = Main.maxProjectiles;
        if (preparingBlockedShot)
            return spareSlot;

        Projectile spare = Main.projectile[spareSlot];
        preparingBlockedShot = true;
        try
        {
            spare.SetDefaults(type);
            spare.whoAmI = spareSlot;
            spare.position = new Vector2(x, y);
            spare.velocity = new Vector2(speedX, speedY);
            spare.owner = owner == -1 ? Main.myPlayer : owner;
            spare.ai[0] = ai0;
            spare.ai[1] = ai1;
            spare.ai[2] = ai2;
            spare.damage = 0;
            spare.friendly = false;
            spare.hostile = false;
        }
        finally
        {
            spare.active = false;
            spare.netUpdate = false;
            preparingBlockedShot = false;
        }

        return spareSlot;
    }

    private static void Reset()
    {
        maskDepth = 0;
        RestorePlayers();
        currentNPC = null;
        enteredWithSpectatorTarget = false;
        preparingBlockedShot = false;
    }
}
