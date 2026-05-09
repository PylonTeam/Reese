using Reese.Common.ReplaySpectate.UI.Tabs.World;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.ReplaySpectate.UI.Tabs.World.WorldSections;

internal sealed class WorldBossInfoSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.BossesDefeated;
    public override string HeaderText => "Bosses defeated";
    public override float Height => 276f;

    public override IReadOnlyList<WorldSectionRow> GetRows()
    {
        return
        [
            new("Bosses Defeated:", () => $"Bosses Defeated: {GetBossesDefeatedText()}", () => Ass.Icon_CheckmarkGreen.Value)
        ];
    }

    public override IReadOnlyList<WorldBossEntry> GetBosses()
    {
        return GetBossEntries();
    }

    private static IReadOnlyList<WorldBossEntry> GetBossEntries()
    {
        int evilBossId;
        string evilBossName;

        if (WorldGen.crimson)
        {
            evilBossId = NPCID.BrainofCthulhu;
            evilBossName = "Brain of Cthulhu";
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossName = "Eater of Worlds";
        }

        return
        [
            new(NPCID.KingSlime, "King Slime", NPC.downedSlimeKing),
            new(NPCID.EyeofCthulhu, "Eye of Cthulhu", NPC.downedBoss1),
            new(evilBossId, evilBossName, NPC.downedBoss2),
            new(NPCID.QueenBee, "Queen Bee", NPC.downedQueenBee),
            new(NPCID.Deerclops, "Deerclops", NPC.downedDeerclops),
            new(NPCID.SkeletronHead, "Skeletron", NPC.downedBoss3),
            new(NPCID.WallofFlesh, "Wall of Flesh", Main.hardMode),
            new(NPCID.QueenSlimeBoss, "Queen Slime", NPC.downedQueenSlime),
            new(NPCID.TheDestroyer, "The Destroyer", NPC.downedMechBoss1),
            new(NPCID.Retinazer, "The Twins", NPC.downedMechBoss2),
            new(NPCID.SkeletronPrime, "Skeletron Prime", NPC.downedMechBoss3),
            new(NPCID.Plantera, "Plantera", NPC.downedPlantBoss),
            new(NPCID.Golem, "Golem", NPC.downedGolemBoss),
            new(NPCID.DukeFishron, "Duke Fishron", NPC.downedFishron),
            new(NPCID.HallowBoss, "Empress of Light", NPC.downedEmpressOfLight),
            new(NPCID.CultistBoss, "Lunatic Cultist", NPC.downedAncientCultist),
            new(NPCID.LunarTowerSolar, "Lunar Pillars", NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust),
            new(NPCID.MoonLordCore, "Moon Lord", NPC.downedMoonlord)
        ];
    }

    private static string GetBossesDefeatedText()
    {
        IReadOnlyList<WorldBossEntry> bosses = GetBossEntries();
        int defeated = 0;

        foreach (WorldBossEntry boss in bosses)
            defeated += boss.Downed ? 1 : 0;

        return $"{defeated}/{bosses.Count}";
    }
}
