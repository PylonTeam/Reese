using System;
using System.Collections.Generic;

namespace Reese.Common.Recorder;

public sealed class ReplaySnapshotEventArgs : EventArgs
{
    public ReplaySnapshotEventArgs(int clientId, uint tick, bool initial)
    {
        ClientId = clientId;
        Tick = tick;
        Initial = initial;
    }

    public int ClientId { get; }
    public uint Tick { get; }
    public bool Initial { get; }
    public bool IsBaseline => !Initial;
}

public static class ReplaySnapshotEvents
{
    private static readonly List<Action<int, uint, bool>> replaySnapshotWriterCallbacks = [];

    public static event Action<ReplaySnapshotEventArgs> ReplaySnapshotWriting;

    public static void RegisterReplaySnapshotWriter(Action<int, uint, bool> callback)
    {
        if (callback is null || replaySnapshotWriterCallbacks.Contains(callback))
            return;

        replaySnapshotWriterCallbacks.Add(callback);
    }

    public static void UnregisterReplaySnapshotWriter(Action<int, uint, bool> callback)
    {
        if (callback is null)
            return;

        replaySnapshotWriterCallbacks.Remove(callback);
    }

    internal static void ClearSubscribers()
    {
        ReplaySnapshotWriting = null;
        replaySnapshotWriterCallbacks.Clear();
    }

    internal static void RaiseReplaySnapshotWriting(int clientId, uint tick, bool initial)
    {
        var args = new ReplaySnapshotEventArgs(clientId, tick, initial);
        InvokeEventSubscribers(args);
        InvokeModCallCallbacks(args);
    }

    private static void InvokeEventSubscribers(ReplaySnapshotEventArgs args)
    {
        Action<ReplaySnapshotEventArgs> handlers = ReplaySnapshotWriting;
        if (handlers is null)
            return;

        foreach (Delegate handlerDelegate in handlers.GetInvocationList())
        {
            if (handlerDelegate is not Action<ReplaySnapshotEventArgs> handler)
                continue;

            try
            {
                handler(args);
            }
            catch (Exception e)
            {
                Log.Warn($"ReplaySnapshotWriting subscriber failed: {e}");
            }
        }
    }

    private static void InvokeModCallCallbacks(ReplaySnapshotEventArgs args)
    {
        foreach (Action<int, uint, bool> callback in replaySnapshotWriterCallbacks.ToArray())
        {
            try
            {
                callback(args.ClientId, args.Tick, args.Initial);
            }
            catch (Exception e)
            {
                Log.Warn($"ReplaySnapshotWriting Mod.Call callback failed: {e}");
            }
        }
    }
}
