using Reese.Common.Replayer;
using Reese.Core.Debug;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reese.Common.Recorder;

[Autoload(Side = ModSide.Both)]
internal sealed class SinglePlayerReplayRecorder : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (ReplaySession.IsReplayPlayback || Main.netMode != NetmodeID.SinglePlayer || Player.whoAmI != Main.myPlayer)
            return;

        Recorder recorder = ModContent.GetInstance<Recorder>();
        if (recorder == null)
        {
            Log.Error("Single-player replay recording could not start because Recorder is not loaded.");
            return;
        }

        recorder.StartSinglePlayerRecording(Player.name);
    }
}
