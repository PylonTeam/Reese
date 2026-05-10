using Reese.Common.Replayer;
using System.IO;
using Terraria.ID;

namespace Reese.Common.Recorder;

public sealed class ReeseCommand : ModCommand
{
    public override string Command => "reese";
    public override string Usage => "reese start [player name] | reese stop";
    public override string Description => "Start or stop Reese replay recording from the server console.";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
        {
            ReplyHelp(caller);
            return;
        }

        string action = args[0].Trim().ToLowerInvariant();

        if (action == "start")
        {
            StartRecording(caller, args);
            return;
        }

        if (action == "stop")
        {
            StopRecording(caller);
            return;
        }

        caller.Reply("Unknown Reese command: " + args[0]);
        ReplyHelp(caller);
    }

    private static void StartRecording(CommandCaller caller, string[] args)
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

        string playerName = args.Length <= 1 ? GetFirstPlayerName() : string.Join(" ", args[1..]);
        if (string.IsNullOrWhiteSpace(playerName))
        {
            caller.Reply("No active player found. Use reese start [player name] after someone joins.");
            return;
        }

        ModContent.GetInstance<Recorder>().StartRecording(playerName);
        caller.Reply("Started Reese recording: " + Path.GetFileName(ReplaySession.CurrentPath));
    }

    private static void StopRecording(CommandCaller caller)
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

    private static void ReplyHelp(CommandCaller caller)
    {
        caller.Reply("/reese start - starts a new recording");
        caller.Reply("/reese stop - stops the existing recording");
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