using Reese.Common.Replayer;
using Reese.Core.Debug;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace Reese.Common.Recorder;

public sealed class StartRecordingCommand : ModCommand
{
	public override string Command => "startrecording";
	public override string Description => "Start a Reese recording from the server console.";
	public override CommandType Type => CommandType.Console;

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		string playerName = args.Length > 0 ? string.Join(" ", args).Trim() : GetFirstPlayerName();
		bool enabled = Main.dedServ;

		if (ReplaySession.IsReplayPlayback)
		{
			Reply(caller, ok: false, "replay playback active", playerName, enabled, null, 0, 0);
			return;
		}

		if (!enabled)
		{
			Reply(caller, ok: false, "recording only enabled on dedicated servers", playerName, enabled, null, 0, 0);
			return;
		}

		if (ReplaySession.IsRecording)
		{
			Reply(caller, ok: false, "recording already active", playerName, enabled, null, 0, 0);
			return;
		}

		if (string.IsNullOrWhiteSpace(playerName))
		{
			Reply(caller, ok: false, "no active player found", playerName, enabled, null, 0, 0);
			return;
		}

		var recorder = ModContent.GetInstance<Recorder>();
		recorder.StartRecording(playerName);

		string reason = ReplaySession.IsRecording ? "started" : "failed to start";
		Reply(caller, ReplaySession.IsRecording, reason, playerName, enabled, ReplaySession.CurrentPath, GetFileBytes(ReplaySession.CurrentPath), recorder.Ticks);
	}

	private static void Reply(CommandCaller caller, bool ok, string reason, string playerName, bool enabled, string path, long bytes, uint ticks)
	{
		string fileName = string.IsNullOrWhiteSpace(path) ? "<none>" : Path.GetFileName(path);
		string world = string.IsNullOrWhiteSpace(Main.worldName) ? "<none>" : Main.worldName;
		bool isRecording = ReplaySession.IsRecording;

		string message = $"start-recording: ok={ok}, reason={reason}, enabled={enabled}, isRecording={isRecording}, " +
			$"player={playerName ?? "<none>"}, world={world}, file={fileName}, bytes={bytes}, ticks={ticks}";

		if (ok)
			Log.Info(message);
		else
			Log.Warn(message);

		caller.Reply(message);
	}

	private static string GetFirstPlayerName()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			var player = Main.player[i];
			if (player?.active == true && !string.IsNullOrWhiteSpace(player.name))
				return player.name;
		}

		return null;
	}

	private static long GetFileBytes(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			return 0;

		return new FileInfo(path).Length;
	}
}
