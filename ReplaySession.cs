using Reese.Core.Debug;
using Terraria;

namespace Reese;

public enum ReplaySessionMode
{
    None,
    Recording,
    Playback
}

public static class ReplaySession
{
    public const int RecordClientIndex = 254;

    public static ReplaySessionMode Mode { get; private set; }
    public static string CurrentPath { get; private set; }

    public static bool IsRecording => Mode == ReplaySessionMode.Recording;
    public static bool IsReplayPlayback => Mode == ReplaySessionMode.Playback;

    public static bool IsRecordClient(int whoAmI) => IsRecording && whoAmI == RecordClientIndex;

    public static bool IsRecordClient(RemoteClient client) => client != null && IsRecordClient(client.Id);

    public static void BeginRecording(string path)
    {
        Mode = ReplaySessionMode.Recording;
        CurrentPath = path;
        Log.Info($"Replay recording started: {path}");
    }

    public static void BeginPlayback(string path)
    {
        Mode = ReplaySessionMode.Playback;
        CurrentPath = path;
        Log.Info($"Replay playback started: {path}");
    }

    public static void End(string reason = null)
    {
        if (Mode != ReplaySessionMode.None)
            Log.Info($"Replay session ended ({Mode}): {reason ?? "no reason supplied"}");

        Mode = ReplaySessionMode.None;
        CurrentPath = null;
    }
}
