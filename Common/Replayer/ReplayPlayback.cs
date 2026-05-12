using Reese.Core.Debug;
using Reese;
using Terraria;

namespace Reese.Common.Replayer;

public static class ReplayPlayback
{
	public const int RecordClientIndex = 254;

	public static bool IsReplayPlayback { get; private set; }
	public static string CurrentPath { get; private set; }
	public static uint DurationTicks { get; private set; }

    public static bool IsPlayerReplayClient(Player player)
    {
        return IsReplayPlayback &&
               player?.active == true &&
               player.whoAmI == RecordClientIndex;
    }

    public static void BeginPlayback(string path)
	{
		IsReplayPlayback = true;
		CurrentPath = path;
		DurationTicks = TryGetDurationTicks(path);
		Log.Info($"Replay playback started: {path}");
	}

	public static void End(string reason = null)
	{
		if (IsReplayPlayback)
			Log.Info($"Replay playback ended: {reason ?? "no reason supplied"}");

		IsReplayPlayback = false;
		CurrentPath = null;
		DurationTicks = 0;
	}

	private static uint TryGetDurationTicks(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return 0;

		if (!ReplayFile.TryReadDurationTicks(path, out uint durationTicks))
			return 0;

		return durationTicks;
	}
}
