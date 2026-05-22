using System;
using System.Collections.Generic;
using System.IO;

namespace Reese.Common.Recorder;

public sealed class RecordingFinishedEventArgs : EventArgs
{
    public RecordingFinishedEventArgs(string filePath, string worldName, string[] modNames, uint durationTicks, string reason)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        WorldName = worldName;
        ModNames = modNames is null ? [] : [.. modNames];
        DurationTicks = durationTicks;
        Reason = reason;
    }

    public string FilePath { get; }
    public string FileName { get; }
    public string WorldName { get; }
    public string[] ModNames { get; }
    public uint DurationTicks { get; }
    public string Reason { get; }
}

public static class RecorderEvents
{
    private static readonly List<Action<string, string, string[], uint, string>> recordingFinishedCallbacks = [];

    public static event Action<RecordingFinishedEventArgs> RecordingFinished;

    public static void RegisterRecordingFinishedCallback(Action<string, string, string[], uint, string> callback)
    {
        if (callback is null || recordingFinishedCallbacks.Contains(callback))
            return;

        recordingFinishedCallbacks.Add(callback);
    }

    public static void UnregisterRecordingFinishedCallback(Action<string, string, string[], uint, string> callback)
    {
        if (callback is null)
            return;

        recordingFinishedCallbacks.Remove(callback);
    }

    internal static void ClearSubscribers()
    {
        RecordingFinished = null;
        recordingFinishedCallbacks.Clear();
    }

    internal static void RaiseRecordingFinished(string filePath, string worldName, string[] modNames, uint durationTicks, string reason)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var args = new RecordingFinishedEventArgs(filePath, worldName, modNames, durationTicks, reason);
        InvokeEventSubscribers(args);
        InvokeModCallCallbacks(args);
    }

    private static void InvokeEventSubscribers(RecordingFinishedEventArgs args)
    {
        Action<RecordingFinishedEventArgs> handlers = RecordingFinished;
        if (handlers is null)
            return;

        foreach (Delegate handlerDelegate in handlers.GetInvocationList())
        {
            if (handlerDelegate is not Action<RecordingFinishedEventArgs> handler)
                continue;

            try
            {
                handler(args);
            }
            catch (Exception e)
            {
                Log.Warn($"RecordingFinished subscriber failed: {e}");
            }
        }
    }

    private static void InvokeModCallCallbacks(RecordingFinishedEventArgs args)
    {
        foreach (Action<string, string, string[], uint, string> callback in recordingFinishedCallbacks.ToArray())
        {
            try
            {
                callback(args.FilePath, args.WorldName, [.. args.ModNames], args.DurationTicks, args.Reason);
            }
            catch (Exception e)
            {
                Log.Warn($"RecordingFinished Mod.Call callback failed: {e}");
            }
        }
    }
}
