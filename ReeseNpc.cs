using Terraria;
using Terraria.ModLoader;

namespace Reese;

public class ReeseNpc : GlobalNPC
{
    public override void SetDefaults(NPC entity)
    {
        // Only NPCs nearby the player are transmitted -- this forces all NPCs to always be transmitted.
        // FIXME: This should only be done for the replay client, not ALL clients!
        entity.netAlways = true;
    }
}