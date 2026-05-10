using Reese.Core.Debug;
using Reese.Common.ReplaySpectate.SpectatorMode;
using Reese.Common.ReplayControls;
using System.IO;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        Main.NewText("Welcome to a Reese replay!");
        Main.NewText("Here's a quick guide on navigating the UI: " + Path.GetFileName(""), Color.LightBlue);
        Main.NewText("The top HUD is used for spectating players", Color.LightBlue);
        Main.NewText("The bottom HUD is used for replay playback controls. Position slider is disabled, for now you can use go to start to restart a replay.", Color.LightBlue);
        Main.NewText("The right-side HUD is used for replay settings, info and more spectating options.", Color.LightBlue);

        Log.Chat("Replay started");
        SpectatorModeSystem.RequestSetLocalMode(SpectateMode.Spectator);
        ModContent.GetInstance<ReplayControlsPanelUISystem>().Open();
    }
}
