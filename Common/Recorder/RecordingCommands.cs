using Reese.Common.Replayer;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reese.Common.Recorder;

public sealed class StartRecordingCommand : ModCommand
{
    public override string Command => "startrecording";
    public override string Usage => "startrecording [player name]";
    public override string Description => "Start recording a Reese replay from the server console.";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (ReplaySession.IsReplayPlayback)
        {
            caller.Reply("Cannot start recording while a replay is playing.");
            return;
        }

        if (ReplaySession.IsRecording)
        {
            caller.Reply("Reese is already recording: " + Path.GetFileName(ReplaySession.CurrentPath));
            return;
        }

        if (Main.netMode != NetmodeID.Server)
        {
            caller.Reply("Reese recording commands are server-console only.");
            return;
        }

        string playerName = args.Length == 0 ? GetFirstPlayerName() : string.Join(" ", args);
        if (string.IsNullOrWhiteSpace(playerName))
        {
            caller.Reply("No active player found. Use startrecording [player name] after someone joins.");
            return;
        }

        ModContent.GetInstance<Recorder>().StartRecording(playerName);
        caller.Reply("Started Reese recording: " + Path.GetFileName(ReplaySession.CurrentPath));
    }

    private static string GetFirstPlayerName()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player?.active == true && !string.IsNullOrWhiteSpace(player.name))
                return player.name;
        }

        return null;
    }
}

public sealed class StopRecordingCommand : ModCommand
{
    public override string Command => "stoprecording";
    public override string Usage => "stoprecording";
    public override string Description => "Stop the active Reese replay recording from the server console.";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!ReplaySession.IsRecording)
        {
            caller.Reply("Reese is not recording.");
            return;
        }

        string path = ReplaySession.CurrentPath;
        ModContent.GetInstance<Recorder>().StopRecording();
        caller.Reply("Stopped Reese recording: " + Path.GetFileName(path));
    }
}
