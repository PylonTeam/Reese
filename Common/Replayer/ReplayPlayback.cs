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
		CurrentPath = null;
		DurationTicks = 0;

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
    public static void SeekToTick(uint tick)
    {
        if (!IsReplayPlayback)
            return;

        Replayer replayer = ModContent.GetInstance<Replayer>();

        if (DurationTicks > 0)
            tick = Math.Min(tick, DurationTicks);

        if (tick == replayer.Ticks)
            return;

        if (tick < replayer.Ticks)
        {
            if (ModContent.GetInstance<ClientConfig>()?.EnableBackwardsSeeking != true)
                return;

            Replayer.ReplaySocket socket = CurrentReplaySocket;

            if (socket?.ResetToStart() != true)
            {
                Log.Chat("Unable to seek backwards: stream reset failed.");
                return;
            }

            replayer.SetTicks(0);
            ResetReplayStateForStart();
            SpectatorTargetSystem.ResetForReplayStart();
        }

        replayer.SetTicks(tick);

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        Replayer.ReplaySocket.ResetTimeoutTimer();
        Log.Chat($"Seeking to tick {tick}...");
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

        replayer.SetTicks(0);
        ResetReplayStateForStart();
        SpectatorTargetSystem.ResetForReplayStart();
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;

        Replayer.ReplaySocket.ResetTimeoutTimer();
        Log.Chat("Replay restarted from the beginning.");
    }

    public static void SeekToEnd()
    {
        SeekToTick(DurationTicks);
    }

    private static Replayer.ReplaySocket CurrentReplaySocket => Netplay.Connection?.Socket as Replayer.ReplaySocket;

    private static void ResetReplayStateForStart()
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
    }
    #endregion
}
