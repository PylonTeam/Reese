using System;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayEvents;

internal readonly record struct ReplayBossDefinition(string Key, string Name, int NpcId, Func<bool> IsDefeated)
{
    public int HeadNpcId => NpcId switch
    {
        NPCID.Golem => NPCID.GolemHead,
        NPCID.MoonLordCore => NPCID.MoonLordHead,
        _ => NpcId
    };

    public ReplayTimelineEvent CreateEvent(uint tick)
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

        if (WorldGen.crimson)
        {
            evilBossId = NPCID.BrainofCthulhu;
            evilBossKey = "brain-of-cthulhu";
            evilBossName = "Brain of Cthulhu";
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossKey = "eater-of-worlds";
            evilBossName = "Eater of Worlds";
        }

        return
        [
            new("king-slime", "King Slime", NPCID.KingSlime, () => NPC.downedSlimeKing),
            new("eye-of-cthulhu", "Eye of Cthulhu", NPCID.EyeofCthulhu, () => NPC.downedBoss1),
            new(evilBossKey, evilBossName, evilBossId, () => NPC.downedBoss2),
            new("queen-bee", "Queen Bee", NPCID.QueenBee, () => NPC.downedQueenBee),
            new("deerclops", "Deerclops", NPCID.Deerclops, () => NPC.downedDeerclops),
            new("skeletron", "Skeletron", NPCID.SkeletronHead, () => NPC.downedBoss3),
            new("wall-of-flesh", "Wall of Flesh", NPCID.WallofFlesh, () => Main.hardMode),
            new("queen-slime", "Queen Slime", NPCID.QueenSlimeBoss, () => NPC.downedQueenSlime),
            new("destroyer", "The Destroyer", NPCID.TheDestroyer, () => NPC.downedMechBoss1),
            new("twins", "The Twins", NPCID.Retinazer, () => NPC.downedMechBoss2),
            new("skeletron-prime", "Skeletron Prime", NPCID.SkeletronPrime, () => NPC.downedMechBoss3),
            new("plantera", "Plantera", NPCID.Plantera, () => NPC.downedPlantBoss),
            new("golem", "Golem", NPCID.Golem, () => NPC.downedGolemBoss),
            new("duke-fishron", "Duke Fishron", NPCID.DukeFishron, () => NPC.downedFishron),
            new("empress-of-light", "Empress of Light", NPCID.HallowBoss, () => NPC.downedEmpressOfLight),
            new("lunatic-cultist", "Lunatic Cultist", NPCID.CultistBoss, () => NPC.downedAncientCultist),
            new("lunar-pillars", "Lunar Pillars", NPCID.LunarTowerSolar, () => NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust),
            new("moon-lord", "Moon Lord", NPCID.MoonLordCore, () => NPC.downedMoonlord)
        ];
    }
}
