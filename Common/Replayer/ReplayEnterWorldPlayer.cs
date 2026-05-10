using Reese.Core.Debug;
using System.IO;
using Reese.Common.Replayer.ReplaySpectate.SpectatorMode;
using Reese.Common.Replayer.ReplaySpectate.UI;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayEnterWorldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        string fileName = string.IsNullOrWhiteSpace(ReplaySession.CurrentPath) ? "Unknown replay" : Path.GetFileName(ReplaySession.CurrentPath);

        Main.NewText("Welcome to Reese replay: " + fileName);
        Main.NewText("Here's a quick guide on navigating the UI:");
        Main.NewText("The top HUD is used for spectating players", Color.DodgerBlue);
        Main.NewText("The bottom HUD is used for replay playback controls.", Color.DodgerBlue);
        Main.NewText("The right-side HUD is used for replay settings, info and more spectating options.", Color.DodgerBlue);

        Log.Chat("Replay started");
        SpectatorModeSystem.RequestSetLocalMode(SpectateMode.Spectator);
        ModContent.GetInstance<ReplayUISystem>().ToggleReplayControls();
    }
}
