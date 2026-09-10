using System;
using System.Collections.Generic;
using System.Threading;
using Reese.Common.Replay.Events;

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

    public static void ResetReplayState()
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
}
