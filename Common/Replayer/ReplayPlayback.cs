using System;
using System.IO;
using Terraria;
using Terraria.Localization;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Core.Configs;

namespace Reese.Common.Replayer;

/// <summary>
/// Acts as a bridge between replayer and recorder.
/// Some utility methods.
/// </summary>
public static class ReplayPlayback
{
    public const int RecordClientIndex = 254;

	public static bool IsReplayPlayback { get; private set; }
	public static bool HasEnteredReplayWorld { get; private set; }
	public static string CurrentPath { get; private set; }
	public static uint DurationTicks { get; private set; }
    public static uint CurrentTick => ModContent.GetInstance<Replayer>().Ticks;
    public static ReplayMetadata Metadata { get; private set; }
    private static bool hasReappliedStartAfterWorldEntry;

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
		HasEnteredReplayWorld = false;
        hasReappliedStartAfterWorldEntry = false;
        Metadata = ReplayMetadata.FromFile(path);
        DurationTicks = Metadata?.DurationTicks ?? 0;
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);
        Log.Info($"Replay playback started: {path}");
	}

    public static void MarkEnteredReplayWorld()
    {
        if (!IsReplayPlayback || HasEnteredReplayWorld)
            return;

        HasEnteredReplayWorld = true;
        Log.Info($"Replay world entered; bootstrap tick advancement stopping at replay tick {CurrentTick}.");
    }

	public static void End(string reason = null, bool quitPlayer=false)
	{
		if (IsReplayPlayback)
			Log.Info($"Replay playback ended: {reason ?? "no reason supplied"}");

        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);
		IsReplayPlayback = false;
		HasEnteredReplayWorld = false;
        hasReappliedStartAfterWorldEntry = false;
		CurrentPath = null;
		DurationTicks = 0;
        CancelSeek();

        if (quitPlayer)
        {
            Log.Info("Leaving replay world because replay has ended.");
            WorldGen.JustQuit();
        }
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

        foreach (string path in Directory.EnumerateFiles(dir, $"{prefix}_*.reese", SearchOption.AllDirectories))
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

    private static bool replayFolderDirty;

    public static void NotifyFolderChanged()
    {
        replayFolderDirty = true;
        Main.QueueMainThreadAction(() => OnReplayFolderChanged?.Invoke());
    }

    public static bool ConsumeReplayFolderDirty()
    {
        bool wasDirty = replayFolderDirty;
        replayFolderDirty = false;
        return wasDirty;
    }

    #region Seeking
    public static bool IsSeeking { get; private set; }
    public static uint SeekTargetTick { get; private set; }

    public static void SeekToTick(uint tick)
    {
        BeginSeekToTick(tick);
    }

    public static void BeginSeekToTick(uint targetTick)
    {
        if (!IsReplayPlayback)
            return;

        Replayer replayer = ModContent.GetInstance<Replayer>();

        if (DurationTicks > 0)
            targetTick = Math.Min(targetTick, DurationTicks);

        if (targetTick == replayer.Ticks)
        {
            CancelSeek();
            return;
        }

        if (targetTick < replayer.Ticks && ModContent.GetInstance<ClientConfig>()?.EnableBackwardsSeeking != true)
            return;

        Replayer.ReplaySocket socket = CurrentReplaySocket;
        if (socket == null)
        {
            Log.Chat("Unable to seek: replay socket is unavailable.");
            return;
        }

        uint currentTick = replayer.Ticks;
        ReplayBaselineEntry? baseline = socket.GetNearestBaselineBefore(targetTick);
        bool canContinueFromCurrent = targetTick > currentTick;
        bool useCurrentState = canContinueFromCurrent &&
                               (!baseline.HasValue || targetTick - currentTick <= targetTick - baseline.Value.Tick);

        uint startTick;
        string startDescription;

        if (useCurrentState)
        {
            startTick = currentTick;
            startDescription = "current state";
        }
        else if (baseline.HasValue)
        {
            ReplayBaselineEntry entry = baseline.Value;
            if (!socket.SeekToBaseline(entry))
            {
                Log.Chat($"Unable to seek to baseline at tick {entry.Tick}; falling back to replay start.");
                if (!ResetReplayToStart(socket, replayer))
                    return;

                startTick = 0;
                startDescription = "start";
            }
            else
            {
                replayer.SetTicks(entry.Tick);
                ResetReplayStateForBaseline(entry.Tick, "baseline");
                startTick = entry.Tick;
                startDescription = $"baseline tick {entry.Tick}";
            }
        }
        else
        {
            if (!ResetReplayToStart(socket, replayer))
                return;

            startTick = 0;
            startDescription = "start";
        }

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        IsSeeking = true;
        SeekTargetTick = targetTick;
        Replayer.ReplaySocket.ResetTimeoutTimer();
        Log.Chat($"Seeking to tick {targetTick} from {startDescription}...");
        Log.Info($"Seeking to tick {targetTick} from {startDescription}; start tick {startTick}.");
    }

    public static void SeekToStart()
    {
        if (!IsReplayPlayback)
            return;

        Replayer replayer = ModContent.GetInstance<Replayer>();
        Replayer.ReplaySocket socket = CurrentReplaySocket;

        if (socket?.ResetToStart() != true)
        {
            Log.Chat("Unable to restart replay: stream reset failed.");
            return;
        }

        CancelSeek();
        replayer.SetTicks(0);
        ResetReplayStateForBaseline(0, "start");
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        Replayer.ReplaySocket.ResetTimeoutTimer();
        Log.Chat("Replay restarted from the beginning.");
    }

    public static void ReapplyStartAfterWorldEntry()
    {
        if (!IsReplayPlayback || hasReappliedStartAfterWorldEntry)
            return;

        Replayer replayer = ModContent.GetInstance<Replayer>();
        Replayer.ReplaySocket socket = CurrentReplaySocket;

        if (socket?.ResetToStart() != true)
        {
            Log.Warn("Unable to reapply replay start after world entry: stream reset failed.");
            return;
        }

        hasReappliedStartAfterWorldEntry = true;
        CancelSeek();
        replayer.SetTicks(0);
        ResetReplayStateForBaseline(0, "world-entry-start-reapply");
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        Replayer.ReplaySocket.ResetTimeoutTimer();
        Log.Info("Reapplied replay start after world entry.");
    }

    public static void SeekToEnd()
    {
        SeekToTick(DurationTicks);
    }

    public static void NotifyPlaybackTickAdvanced(uint currentTick)
    {
        TryCompleteSeek(currentTick);
    }

    public static bool TryCompleteSeek(uint currentTick)
    {
        if (!IsSeeking)
            return false;

        if (currentTick < SeekTargetTick)
            return false;

        Replayer.ReplaySocket socket = CurrentReplaySocket;
        if (socket != null && socket.HasPendingDataAtOrBefore(SeekTargetTick))
            return false;

        uint targetTick = SeekTargetTick;
        IsSeeking = false;
        SeekTargetTick = 0;
        Replayer.ReplaySocket.ResetTimeoutTimer();
        Log.Chat($"Seek complete at tick {currentTick}.");
        Log.Info($"Seek to tick {targetTick} completed at replay tick {currentTick}.");
        return true;
    }

    public static void CancelSeek()
    {
        IsSeeking = false;
        SeekTargetTick = 0;
    }

    private static Replayer.ReplaySocket CurrentReplaySocket => Netplay.Connection?.Socket as Replayer.ReplaySocket;

    private static bool ResetReplayToStart(Replayer.ReplaySocket socket, Replayer replayer)
    {
        if (socket?.ResetToStart() != true)
        {
            Log.Chat("Unable to seek: stream reset failed.");
            return false;
        }

        replayer.SetTicks(0);
        ResetReplayStateForBaseline(0, "start");
        return true;
    }

    private static void ResetReplayStateForBaseline(uint tick = 0, string reason = "unknown")
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != Main.myPlayer && Main.player[i] != null)
                Main.player[i].active = false;
        }

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i] != null)
                Main.npc[i].active = false;
        }

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (Main.projectile[i] != null)
                Main.projectile[i].active = false;
        }

        for (int i = 0; i < Main.maxItems; i++)
        {
            if (Main.item[i] != null)
                Main.item[i].active = false;
        }

        SpectatorTargetSystem.ResetForReplayStart();
        ReplayPlaybackEvents.RaiseReplayStateReset(tick, reason);
    }
    #endregion
}
