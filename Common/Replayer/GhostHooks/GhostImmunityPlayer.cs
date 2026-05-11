using System;
using Terraria.DataStructures;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Makes ghosts invincible
/// </summary>
internal sealed class GhostImmunityPlayer : ModPlayer
{
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        return Player.ghost;
    }

    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
    {
        return !Player.ghost;
    }

    public override bool CanBeHitByProjectile(Projectile proj)
    {
        return !Player.ghost;
    }

    public override bool CanHitPvp(Item item, Player target)
    {
        return target?.ghost != true;
    }

    public override bool CanHitPvpWithProj(Projectile proj, Player target)
    {
        return target?.ghost != true;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        if (!Player.ghost)
            return true;

        Player.statLife = Math.Max(1, Player.statLifeMax2);
        playSound = false;
        genDust = false;
        return false;
    }
}
