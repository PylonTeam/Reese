using Terraria;
using Terraria.ModLoader;

namespace Reese.Common.Recorder;

public class ReeseProjectile : GlobalProjectile
{
    public override void SetDefaults(Projectile entity)
    {
        // Only projectiles nearby the player are transmitted -- this forces all NPCs to always be transmitted.
        // FIXME: This should only be done for the replay client, not ALL clients!
        entity.netImportant = true;
    }
}