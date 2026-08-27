using System;
using System.Collections.Generic;
using System.Threading;
using Reese.Common.MainMenu;
using Reese.Common.Replay.Events;
using Reese.Common.Replay.Hud.ReplaySpectate;
using Reese.Common.Replay;

namespace Reese.Common.Replayer;

/// <summary>
/// Acts as a bridge between replayer and recorder.
/// Some utility methods.
/// </summary>
public static class ReplayPlayback
{
    public static string CurrentPath { get; private set; }
    public static IReadOnlyList<TimelineEvent> TimelineEvents { get; private set; } = Array.Empty<TimelineEvent>();
    private static int launchGeneration;
    private static int activeLaunchGeneration;
    internal static Func<bool> IsLaunchCancelled;

    internal static bool LaunchCancelled()
    {
        try
        {
            return IsLaunchCancelled?.Invoke() == true;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    internal static int BeginLaunchAttempt()
    {
        int generation = Interlocked.Increment(ref launchGeneration);
        Volatile.Write(ref activeLaunchGeneration, generation);
        return generation;
    }

    internal static void CancelLaunchAttempt(int generation)
    {
        if (generation == 0 || Volatile.Read(ref activeLaunchGeneration) == generation)
            Volatile.Write(ref activeLaunchGeneration, 0);
    }

    internal static void CompleteLaunchAttempt(int generation)
    {
        if (Volatile.Read(ref activeLaunchGeneration) == generation)
            Volatile.Write(ref activeLaunchGeneration, 0);
    }

    internal static bool IsReplayLaunchActive => Volatile.Read(ref activeLaunchGeneration) != 0;

    public static void BeginPlayback(string path)
    {
        CurrentPath = path;
        SpectatorTargetSystem.ResetForReplayStart();
        // Metadata = ReplayMetadata.FromFile(path);
        // DurationTicks = Metadata?.DurationTicks ?? 0;
        // TimelineEvents = Metadata?.Events ?? Array.Empty<ReplayTimelineEvent>();
        // ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(1f);
        Log.Info($"Replay playback started: {path}");
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
        // TODO: seek
        return;
    }

    public static void SeekToStart()
    {
        // TODO: seek
    }

    public static void SeekToEnd()
    {
        // FIXME: seek to end
        // SeekToTick(DurationTicks);
    }

    public static void CancelSeek()
    {
        IsSeeking = false;
        SeekTargetTick = 0;
    }

    private static void ResetReplayStateForSeek(uint tick = 0, string reason = "unknown")
    {
        ResetReplayState();
        SpectatorTargetSystem.PreserveTargetForSeek();
    }

    private static void ResetReplayStateForReplayStart(uint tick = 0, string reason = "unknown")
    {
        ResetReplayState();
        SpectatorTargetSystem.ResetForReplayStart();
    }

    private static void ResetReplayState()
    {
        Player local = Main.LocalPlayer;
        bool wasGhost = local?.ghost == true;
        bool wasDead = local?.dead == true;
        int selectedItem = local?.selectedItem ?? 0;
        bool playerInventory = Main.playerInventory;

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

        if (local != null)
        {
            local.ghost = wasGhost;
            local.dead = wasDead;
            local.selectedItem = selectedItem;
        }

        Main.playerInventory = playerInventory;
    }
    #endregion
}
