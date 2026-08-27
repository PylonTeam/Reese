using Reese.Content;
using Reese.Core.Configs;
using Reese.Core.Localization;
using Reese.Common.Replay.Hud.ReplaySpectate;
using System;
using System.IO;
using Terraria.Chat;
using Terraria.ID;
using Reese.Common.Replayer;

namespace Reese.Common.Replay;

[Autoload(Side = ModSide.Both)]
internal sealed class PlaybackEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
#if DEBUG
        //string debug = $"OnEnterWorld: IsReplayPlayback={Playback.IsPlaying}, RecordClientIndex={ReplayPlayback.RecordClientIndex}, myPlayer={Main.myPlayer}, localActive={Main.LocalPlayer?.active}, ghost={Main.LocalPlayer?.ghost}, PendingReplayPath={Replayer.PendingReplayPathPublic}";
        //ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{debug}"), Main.OurFavoriteColor, Player.whoAmI);
#endif

        if (!Playback.IsPlaying)
            return;

        ReplayPlayback.MarkEnteredReplayWorld();

        string fileName = string.IsNullOrWhiteSpace(ReplayPlayback.CurrentPath)
            ? "Unknown replay"
            : Path.GetFileName(ReplayPlayback.CurrentPath);

        Log.Chat("Replay started: " + fileName);

        if (!ModContent.GetInstance<ClientConfig>().ShowWelcomeMessageOnEnterWorld)
            return;

        Color reeseColor = Color.CornflowerBlue;

        string smallCameraItemTag = $"[i:{ModContent.ItemType<SmallCameraItem>()}]";
        string bigCameraItemTag = $"[i:{ModContent.ItemType<CameraItem>()}]";
        string[] lines = Loc.Get("Replayer.ReplayWelcomeMessage", fileName)
            .Replace("\r\n", "\n")
            .Split('\n');
        int lastLine = Array.FindLastIndex(lines, line => !string.IsNullOrWhiteSpace(line));

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string icon = i == 0 ? bigCameraItemTag : smallCameraItemTag;
            Color color = i == 0 || i == lastLine ? Main.OurFavoriteColor : reeseColor;
            ChatHelper.SendChatMessageToClient(Terraria.Localization.NetworkText.FromLiteral($"{icon} {line}"), color, Player.whoAmI);
        }
    }
}
