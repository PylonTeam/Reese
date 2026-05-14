using Reese.Content;
using Reese.Core.Configs;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
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
        //string debug = $"OnEnterWorld: IsReplayPlayback={ReplayPlayback.IsReplayPlayback}, RecordClientIndex={ReplayPlayback.RecordClientIndex}, myPlayer={Main.myPlayer}, localActive={Main.LocalPlayer?.active}, ghost={Main.LocalPlayer?.ghost}, PendingReplayPath={Replayer.PendingReplayPathPublic}";
        //ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{debug}"), Main.OurFavoriteColor, Player.whoAmI);
#endif

        if (!ReplayPlayback.IsReplayPlayback)
            return;

        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);

        string fileName = string.IsNullOrWhiteSpace(ReplayPlayback.CurrentPath)
            ? "Unknown replay"
            : Path.GetFileName(ReplayPlayback.CurrentPath);

        Log.Chat("Replay started: " + fileName);

        if (!ModContent.GetInstance<ClientConfig>().ShowWelcomeMessageOnEnterWorld)
            return;

        Color reeseColor = Color.CornflowerBlue;

        string smallCameraItemTag = $"[i:{ModContent.ItemType<SmallCameraItem>()}]";
        string bigCameraItemTag = $"[i:{ModContent.ItemType<CameraItem>()}]";

        // Send the message
        //ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{bigCameraItemTag} Welcome to your Reese replay! Now playing: '[c/FFFFFF:{fileName}]'"), Main.OurFavoriteColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{bigCameraItemTag} Welcome to your Reese replay!"), Main.OurFavoriteColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Quick guide:"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Top HUD: spectate players/NPCs"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Bottom HUD: playback controls"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Right side HUD: settings"), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Right click to teleport around the world."), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} To toggle the entire replay HUD, assign a keybind in controls."), reeseColor, Player.whoAmI);
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral($"{smallCameraItemTag} Enjoy!"), Main.OurFavoriteColor, Player.whoAmI);
    }
}
