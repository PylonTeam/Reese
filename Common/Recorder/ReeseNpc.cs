using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Reese.Common.Recorder;

public class ReeseNpc : GlobalNPC
{
    public override void SetDefaults(NPC entity)
    {
        // Only NPCs nearby the player are transmitted -- this forces all NPCs to always be transmitted.
        // FIXME: This should only be done for the replay client, not ALL clients!
        entity.netAlways = true;
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        ModContent.GetInstance<ReplayTimelineTrackerSystem>()?.RecordBossSummoned(npc);
    }

    public override void OnKill(NPC npc)
    {
        ModContent.GetInstance<ReplayTimelineTrackerSystem>()?.RecordBossDefeated(npc);
    }
}
