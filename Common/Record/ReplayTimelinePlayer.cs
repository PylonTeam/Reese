using Reese.Common.Replayer;
using Reese.Common.Replay.Events;
using Terraria.DataStructures;
using Terraria.ID;

namespace Reese.Common.Record;

internal sealed class ReplayTimelinePlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Recorder rec = ModContent.GetInstance<Recorder>();
        if (!rec.IsRecording || Player.whoAmI == rec.WhoAmI)
            return;

        TimelineRecorder.RecordPlayerJoined(Player, rec.Tick);
    }

    public override void PlayerDisconnect()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Recorder rec = ModContent.GetInstance<Recorder>();
        if (!rec.IsRecording || Player.whoAmI == rec.WhoAmI)
            return;

        TimelineRecorder.RecordPlayerLeft(Player, rec.Tick);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Recorder rec = ModContent.GetInstance<Recorder>();
        if (!rec.IsRecording || Player.whoAmI == rec.WhoAmI)
            return;

        TimelineRecorder.RecordPlayerDeath(Player, damageSource, rec.Tick);
    }
}
