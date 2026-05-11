using Reese.Content;
using Reese.Core.Configs;
using Reese.Core.Debug;
using System.IO;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Both)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
#if DEBUG
        //string debug = $"OnEnterWorld: IsReplayPlayback={ReplaySession.IsReplayPlayback}, RecordClientIndex={ReplaySession.RecordClientIndex}, myPlayer={Main.myPlayer}, localActive={Main.LocalPlayer?.active}, ghost={Main.LocalPlayer?.ghost}, PendingReplayPath={Replayer.PendingReplayPathPublic}";
        //ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{debug}"), Main.OurFavoriteColor, Player.whoAmI);
#endif

        if (!ReplaySession.IsReplayPlayback)
            return;

        string fileName = string.IsNullOrWhiteSpace(ReplaySession.CurrentPath)
            ? "Unknown replay"
            : Path.GetFileName(ReplaySession.CurrentPath);

        Log.Chat("Replay started: " + fileName);

        if (!ModContent.GetInstance<ClientConfig>().ShowWelcomeMessageOnEnterWorld)
            return;

        Color reeseColor = Color.CornflowerBlue;

        string smallCameraItemTag = $"[i:{ModContent.ItemType<Icon_CameraSmall>()}]";
        string bigCameraItemTag = $"[i:{ModContent.ItemType<Icon_Camera>()}]";

        // Send the message
        //ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{bigCameraItemTag} Welcome to your Reese replay! Now playing: '[c/FFFFFF:{fileName}]'"), Main.OurFavoriteColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{bigCameraItemTag} Welcome to your Reese replay!"), Main.OurFavoriteColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Here's a short guide:"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Top HUD: spectate players"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Bottom HUD: replay playback controls"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Right side HUD: replay settings, info, and spectator options"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} As a ghost you can right click to teleport around the world."), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} To toggle the entire replay HUD, assign a keybind in controls."), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Enjoy!"), Main.OurFavoriteColor, Player.whoAmI);
    }
}
