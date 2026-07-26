using Reese.Common.Spectator;

namespace Reese.Common.Replayer.GhostHooks;

/// <summary>
/// Stops ghosts from picking up world items, and — more importantly — from reserving them.
/// <para/>
/// Player.Update bails into Player.Ghost and returns before it reaches GrabItems, so a ghost never
/// literally collects an item. The visible bug is Item.FindOwner (Item.cs:51483-51503), which runs on the
/// server, scans every player with only an <c>active</c> check, and assigns
/// <c>playerIndexTheItemIsReservedFor</c> to whichever eligible player is closest. A ghost hovering over
/// loot wins that reservation and the item becomes unlootable for the real player standing next to it,
/// with the server broadcasting the reservation over MessageID 21/22.
/// <para/>
/// Both that loop and GrabItems gate on <c>ItemLoader.CanPickup</c>, so denying it here closes the
/// reservation, the magnet pull, and the pickup itself — including hearts, mana stars and coins.
/// </summary>
internal sealed class GhostDisablePickupItem : GlobalItem
{
    public override bool CanPickup(Item item, Player player)
    {
        return !SpectatorMode.IsSpectatingForWorldLogic(player);
    }
}
