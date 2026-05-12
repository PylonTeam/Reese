using Reese.Core.Debug;
using System;
using System.IO;
using Terraria;

namespace Reese;

/// <summary>
/// Acts as a bridge between replayer and recorder.
/// Some utility methods.
/// </summary>
public static class ReplayPlayback
{
	public const int RecordClientIndex = 254;

	public static bool IsReplayPlayback { get; private set; }
	public static string CurrentPath { get; private set; }
	public static uint DurationTicks { get; private set; }
    public static uint CurrentTick => ModContent.GetInstance<Replayer>().Ticks;
    public static ReplayMetadata Metadata { get; private set; }

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
        Metadata = ReplayMetadata.FromFile(path);
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

    // Helper for checking for active players, used for deciding when to start and stop recording
    public static bool HasActivePlayers()
    {
        const int RecordClientIndex = ReplayPlayback.RecordClientIndex;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i == RecordClientIndex)
                continue;

            if (Main.player[i]?.active == true)
                return true;
        }

        return false;
    }

    // Helper for deciding filename
    public static int GetNextReplayNumber(string dir, string prefix)
    {
        int next = 1;

        foreach (string path in Directory.EnumerateFiles(dir, $"{prefix}_*.reese", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string suffix = name.Length > prefix.Length + 1 ? name[(prefix.Length + 1)..] : string.Empty;
            if (int.TryParse(suffix, out int number) && number >= next)
                next = number + 1;
        }

        return next;
    }

    // Trigger rebuild when folder changes
    public static event Action OnReplayFolderChanged;

    public static void NotifyFolderChanged()
    {
        Main.QueueMainThreadAction(() => OnReplayFolderChanged?.Invoke());
    }

    #region Seeking
    public static void SeekToTick(uint tick)
    {
        if (!IsReplayPlayback) return;
        Replayer.SeekToTick(tick);
    }
    public static void SeekToStart()
    {
        if (!IsReplayPlayback) return;
        Replayer.SeekToStart();
    }

    public static void SeekToEnd()
    {
        if (!IsReplayPlayback) return;
        Replayer.SeekToEnd();
    }
    #endregion
}
