using System;
using System.Collections.Generic;
using System.Linq;
using Reese.Common.Replay.ReplayHud;
using Terraria.ID;
using Terraria.Localization;

namespace Reese.Common.Replay.ReplayEvents;

internal readonly record struct ReplayBossDefinition(string Key, string Name, int HeadNpcId, int[] NpcIds, Func<bool> IsDefeated)
{
    public bool Matches(int npcId)
    {
        return NpcIds?.Contains(npcId) == true;
    }

    public ReplayTimelineEvent CreateSummonedEvent(uint tick)
    {
        return new ReplayTimelineEvent(tick, ReplayEventCategory.BossSummoned, Key, Loc.Get("ReplayHud.Events.BossSummoned", Name), ReplayEventIconKind.BossHead, HeadNpcId);
    }

    public ReplayTimelineEvent CreateDefeatedEvent(uint tick)
    {
        return new ReplayTimelineEvent(tick, ReplayEventCategory.BossDefeated, Key, Loc.Get("ReplayHud.Events.BossDefeated", Name), ReplayEventIconKind.BossHead, HeadNpcId);
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
            evilBossName = Lang.GetNPCNameValue(NPCID.BrainofCthulhu);
            evilBossNpcIds = [NPCID.BrainofCthulhu];
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossKey = "eater-of-worlds";
            evilBossName = Lang.GetNPCNameValue(NPCID.EaterofWorldsHead);
            evilBossNpcIds = [NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail];
        }

        return
        [
            Create("king-slime", Lang.GetNPCNameValue(NPCID.KingSlime), NPCID.KingSlime, () => NPC.downedSlimeKing, NPCID.KingSlime),
            Create("eye-of-cthulhu", Lang.GetNPCNameValue(NPCID.EyeofCthulhu), NPCID.EyeofCthulhu, () => NPC.downedBoss1, NPCID.EyeofCthulhu),
            Create(evilBossKey, evilBossName, evilBossId, () => NPC.downedBoss2, evilBossNpcIds),
            Create("queen-bee", Lang.GetNPCNameValue(NPCID.QueenBee), NPCID.QueenBee, () => NPC.downedQueenBee, NPCID.QueenBee),
            Create("deerclops", Lang.GetNPCNameValue(NPCID.Deerclops), NPCID.Deerclops, () => NPC.downedDeerclops, NPCID.Deerclops),
            Create("skeletron", Lang.GetNPCNameValue(NPCID.SkeletronHead), NPCID.SkeletronHead, () => NPC.downedBoss3, NPCID.SkeletronHead),
            Create("wall-of-flesh", Lang.GetNPCNameValue(NPCID.WallofFlesh), NPCID.WallofFlesh, () => Main.hardMode, NPCID.WallofFlesh, NPCID.WallofFleshEye),
            Create("queen-slime", Lang.GetNPCNameValue(NPCID.QueenSlimeBoss), NPCID.QueenSlimeBoss, () => NPC.downedQueenSlime, NPCID.QueenSlimeBoss),
            Create("destroyer", Lang.GetNPCNameValue(NPCID.TheDestroyer), NPCID.TheDestroyer, () => NPC.downedMechBoss1, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail),
            Create("twins", Language.GetTextValue("Enemies.TheTwins"), NPCID.Retinazer, () => NPC.downedMechBoss2, NPCID.Retinazer, NPCID.Spazmatism),
            Create("skeletron-prime", Lang.GetNPCNameValue(NPCID.SkeletronPrime), NPCID.SkeletronPrime, () => NPC.downedMechBoss3, NPCID.SkeletronPrime),
            Create("plantera", Lang.GetNPCNameValue(NPCID.Plantera), NPCID.Plantera, () => NPC.downedPlantBoss, NPCID.Plantera),
            Create("golem", Lang.GetNPCNameValue(NPCID.Golem), NPCID.GolemHead, () => NPC.downedGolemBoss, NPCID.Golem, NPCID.GolemHead),
            Create("duke-fishron", Lang.GetNPCNameValue(NPCID.DukeFishron), NPCID.DukeFishron, () => NPC.downedFishron, NPCID.DukeFishron),
            Create("empress-of-light", Lang.GetNPCNameValue(NPCID.HallowBoss), NPCID.HallowBoss, () => NPC.downedEmpressOfLight, NPCID.HallowBoss),
            Create("lunatic-cultist", Lang.GetNPCNameValue(NPCID.CultistBoss), NPCID.CultistBoss, () => NPC.downedAncientCultist, NPCID.CultistBoss),
            Create("lunar-pillars", Loc.Get("ReplayHud.Events.Boss.LunarPillars"), NPCID.LunarTowerSolar, () => NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust, NPCID.LunarTowerSolar, NPCID.LunarTowerVortex, NPCID.LunarTowerNebula, NPCID.LunarTowerStardust),
            Create("moon-lord", Lang.GetNPCNameValue(NPCID.MoonLordCore), NPCID.MoonLordHead, () => NPC.downedMoonlord, NPCID.MoonLordCore, NPCID.MoonLordHead, NPCID.MoonLordHand)
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
