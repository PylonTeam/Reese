using Reese.Common.Replayer;
using Reese.Core.Debug;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace Reese.Common.Recorder;

public sealed class StopRecordingCommand : ModCommand
{
	public override string Command => "stoprecording";
	public override string Description => "Stop the active Reese recording.";
	public override CommandType Type => CommandType.Console;

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		bool enabled = Main.dedServ;
		bool wasRecording = ReplaySession.IsRecording;

		if (!wasRecording)
		{
			Reply(caller, ok: false, "not recording", enabled, wasRecording, ReplaySession.IsRecording, null, 0, 0);
			return;
		}

		var recorder = ModContent.GetInstance<Recorder>();
		string path = ReplaySession.CurrentPath;
		long bytes = GetFileBytes(path);
		uint ticks = recorder.Ticks;

		recorder.StopRecording();

		string reason = ReplaySession.IsRecording ? "stop failed" : "stopped";
		Reply(caller, !ReplaySession.IsRecording, reason, enabled, wasRecording, ReplaySession.IsRecording, path, bytes, ticks);
	}

	private static void Reply(CommandCaller caller, bool ok, string reason, bool enabled, bool wasRecording, bool isRecording, string path, long bytes, uint ticks)
	{
		string fileName = string.IsNullOrWhiteSpace(path) ? "<none>" : Path.GetFileName(path);
		string world = string.IsNullOrWhiteSpace(Main.worldName) ? "<none>" : Main.worldName;

		string message = $"stop-recording: ok={ok}, reason={reason}, enabled={enabled}, wasRecording={wasRecording}, " +
			$"isRecording={isRecording}, world={world}, file={fileName}, bytes={bytes}, ticks={ticks}";

		if (ok)
			Log.Info(message);
		else
			Log.Warn(message);

		caller.Reply(message);
	}

	private static long GetFileBytes(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			return 0;

		return new FileInfo(path).Length;
	}
}
