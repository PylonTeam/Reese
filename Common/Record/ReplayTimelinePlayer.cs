using Reese.Common.Replayer;
using Reese.Common.Replay.ReplayEvents;
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
        if (!(rec.IsRecording && Player.whoAmI == rec.WhoAmI))
            return;

        ReplayTimelineRecorder.RecordPlayerJoined(Player, rec.Ticks);
    }

    public override void PlayerDisconnect()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Recorder rec = ModContent.GetInstance<Recorder>();
        if (!(rec.IsRecording && Player.whoAmI == rec.WhoAmI))
            return;

        ReplayTimelineRecorder.RecordPlayerLeft(Player, rec.Ticks);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Recorder rec = ModContent.GetInstance<Recorder>();
        if (!(rec.IsRecording && Player.whoAmI == rec.WhoAmI))
            return;

        ReplayTimelineRecorder.RecordPlayerDeath(Player, damageSource, rec.Ticks);
    }
}
