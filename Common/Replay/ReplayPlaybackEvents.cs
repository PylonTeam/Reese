using System;
using System.Collections.Generic;

namespace Reese.Common.Replayer;

public sealed class ReplayStateResetEventArgs : EventArgs
{
    public ReplayStateResetEventArgs(uint tick, string reason)
    {
        Tick = tick;
        Reason = reason ?? string.Empty;
    }

    public uint Tick { get; }
    public string Reason { get; }
}

public static class ReplayPlaybackEvents
{
    private static readonly List<Action<uint, string>> replayStateResetCallbacks = [];

    public static event Action<ReplayStateResetEventArgs> ReplayStateReset;

    public static void RegisterReplayStateResetCallback(Action<uint, string> callback)
    {
        if (callback is null || replayStateResetCallbacks.Contains(callback))
            return;

        replayStateResetCallbacks.Add(callback);
    }

    public static void UnregisterReplayStateResetCallback(Action<uint, string> callback)
    {
        if (callback is null)
            return;

        replayStateResetCallbacks.Remove(callback);
    }

    internal static void ClearSubscribers()
    {
        ReplayStateReset = null;
        replayStateResetCallbacks.Clear();
    }

    internal static void RaiseReplayStateReset(uint tick, string reason)
    {
        var args = new ReplayStateResetEventArgs(tick, reason);
        InvokeEventSubscribers(args);
        InvokeModCallCallbacks(args);
    }

    private static void InvokeEventSubscribers(ReplayStateResetEventArgs args)
    {
        Action<ReplayStateResetEventArgs> handlers = ReplayStateReset;
        if (handlers is null)
            return;

        foreach (Delegate handlerDelegate in handlers.GetInvocationList())
        {
            if (handlerDelegate is not Action<ReplayStateResetEventArgs> handler)
                continue;

            try
            {
                handler(args);
            }
            catch (Exception e)
            {
                Log.Warn($"ReplayStateReset subscriber failed: {e}");
            }
        }
    }

    private static void InvokeModCallCallbacks(ReplayStateResetEventArgs args)
    {
        foreach (Action<uint, string> callback in replayStateResetCallbacks.ToArray())
        {
            try
            {
                callback(args.Tick, args.Reason);
            }
            catch (Exception e)
            {
                Log.Warn($"ReplayStateReset Mod.Call callback failed: {e}");
            }
        }
    }
}
