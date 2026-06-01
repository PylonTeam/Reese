using Reese.Common.Replayer;
using Reese.Common.Replayer.ReplayEvents;
using Terraria.DataStructures;
using Terraria.ID;

namespace Reese.Common.Recorder;

internal sealed class ReplayTimelinePlayer : ModPlayer
{
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        Recorder recorder = ModContent.GetInstance<Recorder>();
        if (!recorder.IsRecording || Player.whoAmI == ReplayPlayback.RecordClientIndex)
            return;

        ReplayTimelineRecorder.RecordPlayerDeath(Player, recorder.Ticks);
    }
}
