using Reese.Content;
using Reese.Core.Configs;
using System;
using Terraria.Chat;

namespace Reese.Common.Replay;

[Autoload(Side = ModSide.Client)]
internal sealed class PlaybackEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ModContent.GetInstance<ClientConfig>().ShowWelcomeMessageOnEnterWorld)
            return;

        if (!Playback.IsPlayingReplay(out var replay))
            return;

        Color reeseColor = Color.CornflowerBlue;

        string smallCameraItemTag = $"[i:{ModContent.ItemType<SmallCameraItem>()}]";
        string bigCameraItemTag = $"[i:{ModContent.ItemType<CameraItem>()}]";
        string[] lines = Loc.Get("Replayer.ReplayWelcomeMessage", replay.MetaInfo.Title, replay.MetaInfo.WorldName)
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
