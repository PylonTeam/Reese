using Reese.Common.Replayer.ReplaySpectate;
using Reese.Common.Replayer.ReplaySpectate.UI;
using Reese.Content;
using Reese.Core.Configs;
using Reese.Core.Debug;
using System.IO;
using Terraria.Chat;
using Terraria.Localization;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Both)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        SpectatorModeSystem.ForceLocalReplaySpectator();

        if (!ModContent.GetInstance<ClientConfig>().ShowWelcomeMessageOnEnterWorld)
            return;

        string fileName = string.IsNullOrWhiteSpace(ReplaySession.CurrentPath)
            ? "Unknown replay"
            : Path.GetFileName(ReplaySession.CurrentPath);
        Color reeseColor = Color.CornflowerBlue;

        int iconItemType = ModContent.ItemType<Icon_CameraSmall>();
        string cameraItemTag = $"[i:{iconItemType}]";

        int iconItemType2 = ModContent.ItemType<Icon_Camera>();
        string cameraItemTag2 = $"[i:{iconItemType2}]";

        // Send the message
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag2} Welcome to your Reese replay! Now playing: '[c/FFFFFF:{fileName}]'"), Main.OurFavoriteColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag} Here's a short guide:"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag} Top HUD: spectate players"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag} Bottom HUD: replay playback controls"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag} Right side HUD: replay settings, info, and spectator options"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag} Right click as a ghost to teleport."), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{cameraItemTag} Enjoy!"), Main.OurFavoriteColor, Player.whoAmI);

        Log.Chat("Replay started: " + fileName);
        ModContent.GetInstance<ReplayUISystem>().ToggleReplayControls();
    }
}