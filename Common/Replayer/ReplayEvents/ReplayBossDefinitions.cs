using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayEvents;

internal readonly record struct ReplayBossDefinition(string Key, string Name, int HeadNpcId, int[] NpcIds, Func<bool> IsDefeated)
{
    public bool Matches(int npcId)
    {
        return NpcIds?.Contains(npcId) == true;
    }

    public ReplayTimelineEvent CreateSummonedEvent(uint tick)
    {
        return new ReplayTimelineEvent(tick, ReplayEventCategory.BossSummoned, Key, $"{Name} summoned", ReplayEventIconKind.BossHead, HeadNpcId);
    }

    public ReplayTimelineEvent CreateDefeatedEvent(uint tick)
    {
        return new ReplayTimelineEvent(tick, ReplayEventCategory.BossDefeated, Key, $"{Name} defeated", ReplayEventIconKind.BossHead, HeadNpcId);
    }
}

internal static class ReplayBossDefinitions
{
    public static IReadOnlyList<ReplayBossDefinition> GetDefinitions()
    {
        int evilBossId;
        string evilBossKey;
        string evilBossName;
        int[] evilBossNpcIds;

        if (WorldGen.crimson)
        {
            evilBossId = NPCID.BrainofCthulhu;
            evilBossKey = "brain-of-cthulhu";
            evilBossName = "Brain of Cthulhu";
            evilBossNpcIds = [NPCID.BrainofCthulhu];
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossKey = "eater-of-worlds";
            evilBossName = "Eater of Worlds";
            evilBossNpcIds = [NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail];
        }

        return
        [
            Create("king-slime", "King Slime", NPCID.KingSlime, () => NPC.downedSlimeKing, NPCID.KingSlime),
            Create("eye-of-cthulhu", "Eye of Cthulhu", NPCID.EyeofCthulhu, () => NPC.downedBoss1, NPCID.EyeofCthulhu),
            Create(evilBossKey, evilBossName, evilBossId, () => NPC.downedBoss2, evilBossNpcIds),
            Create("queen-bee", "Queen Bee", NPCID.QueenBee, () => NPC.downedQueenBee, NPCID.QueenBee),
            Create("deerclops", "Deerclops", NPCID.Deerclops, () => NPC.downedDeerclops, NPCID.Deerclops),
            Create("skeletron", "Skeletron", NPCID.SkeletronHead, () => NPC.downedBoss3, NPCID.SkeletronHead),
            Create("wall-of-flesh", "Wall of Flesh", NPCID.WallofFlesh, () => Main.hardMode, NPCID.WallofFlesh, NPCID.WallofFleshEye),
            Create("queen-slime", "Queen Slime", NPCID.QueenSlimeBoss, () => NPC.downedQueenSlime, NPCID.QueenSlimeBoss),
            Create("destroyer", "The Destroyer", NPCID.TheDestroyer, () => NPC.downedMechBoss1, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail),
            Create("twins", "The Twins", NPCID.Retinazer, () => NPC.downedMechBoss2, NPCID.Retinazer, NPCID.Spazmatism),
            Create("skeletron-prime", "Skeletron Prime", NPCID.SkeletronPrime, () => NPC.downedMechBoss3, NPCID.SkeletronPrime),
            Create("plantera", "Plantera", NPCID.Plantera, () => NPC.downedPlantBoss, NPCID.Plantera),
            Create("golem", "Golem", NPCID.GolemHead, () => NPC.downedGolemBoss, NPCID.Golem, NPCID.GolemHead),
            Create("duke-fishron", "Duke Fishron", NPCID.DukeFishron, () => NPC.downedFishron, NPCID.DukeFishron),
            Create("empress-of-light", "Empress of Light", NPCID.HallowBoss, () => NPC.downedEmpressOfLight, NPCID.HallowBoss),
            Create("lunatic-cultist", "Lunatic Cultist", NPCID.CultistBoss, () => NPC.downedAncientCultist, NPCID.CultistBoss),
            Create("lunar-pillars", "Lunar Pillars", NPCID.LunarTowerSolar, () => NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust, NPCID.LunarTowerSolar, NPCID.LunarTowerVortex, NPCID.LunarTowerNebula, NPCID.LunarTowerStardust),
            Create("moon-lord", "Moon Lord", NPCID.MoonLordHead, () => NPC.downedMoonlord, NPCID.MoonLordCore, NPCID.MoonLordHead, NPCID.MoonLordHand)
        ];
    }

    public static bool TryGetDefinition(int npcId, out ReplayBossDefinition definition)
    {
        foreach (ReplayBossDefinition boss in GetDefinitions())
        {
            if (!boss.Matches(npcId))
                continue;

            definition = boss;
            return true;
        }

        definition = default;
        return false;
    }

    public static bool HasActiveInstance(ReplayBossDefinition definition, int excludedNpcIndex = -1)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (i == excludedNpcIndex)
                continue;

            NPC npc = Main.npc[i];
            if (npc?.active == true && definition.Matches(npc.type))
                return true;
        }

        return false;
    }

    private static ReplayBossDefinition Create(string key, string name, int headNpcId, Func<bool> isDefeated, params int[] npcIds)
    {
        return new ReplayBossDefinition(key, name, headNpcId, npcIds.Distinct().ToArray(), isDefeated);
    }
}
