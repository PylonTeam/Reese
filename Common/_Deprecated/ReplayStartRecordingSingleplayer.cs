//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace Reese.Common._Deprecated;

//[Autoload(Side = ModSide.Both)]
//internal sealed class ReplayRecordingPlayer : ModPlayer
//{
//    public override void OnEnterWorld()
//    {
//        if (ReplaySession.IsReplayPlayback)
//            return;

//        if (Main.netMode == NetmodeID.SinglePlayer)
//            ModContent.GetInstance<Recorder>().StartSinglePlayerRecording(Player.name);
//    }
//}
