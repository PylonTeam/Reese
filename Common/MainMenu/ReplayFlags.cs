using System.IO;

namespace Reese.Common.MainMenu;

/// <summary>
/// Keeps track of and displays new/played/favorite flags for a replay.
/// </summary>
internal static class ReplayFlags
{
    public static bool IsNew(string replayPath) => HasFlag(replayPath, "new");
    public static bool HasWatched(string replayPath) => HasFlag(replayPath, "watched");
    public static bool IsFavorite(string replayPath) => HasFlag(replayPath, "favorite");

    public static void MarkNew(string replayPath) => WriteFlag(replayPath, "new");
    public static void MarkWatched(string replayPath)
    {
        DeleteFlag(replayPath, "new");
        WriteFlag(replayPath, "watched");
    }
    public static void ToggleFavorite(string replayPath)
    {
        if (IsFavorite(replayPath))
            DeleteFlag(replayPath, "favorite");
        else
            WriteFlag(replayPath, "favorite");
    }

    public static void Delete(string replayPath)
    {
        DeleteFlag(replayPath, "new");
        DeleteFlag(replayPath, "watched");
        DeleteFlag(replayPath, "favorite");
    }

    public static void Move(string oldReplayPath, string newReplayPath)
    {
        MoveFlag(oldReplayPath, newReplayPath, "new");
        MoveFlag(oldReplayPath, newReplayPath, "watched");
        MoveFlag(oldReplayPath, newReplayPath, "favorite");
    }

    private static bool HasFlag(string replayPath, string flag)
        => !string.IsNullOrWhiteSpace(replayPath) && File.Exists(FlagPath(replayPath, flag));

    private static void WriteFlag(string replayPath, string flag)
    {
        if (!string.IsNullOrWhiteSpace(replayPath))
            File.WriteAllText(FlagPath(replayPath, flag), "");
    }

    private static void DeleteFlag(string replayPath, string flag)
    {
        if (string.IsNullOrWhiteSpace(replayPath))
            return;

        string path = FlagPath(replayPath, flag);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void MoveFlag(string oldReplayPath, string newReplayPath, string flag)
    {
        if (string.IsNullOrWhiteSpace(oldReplayPath) || string.IsNullOrWhiteSpace(newReplayPath))
            return;

        string oldPath = FlagPath(oldReplayPath, flag);
        if (!File.Exists(oldPath))
            return;

        string newPath = FlagPath(newReplayPath, flag);
        if (File.Exists(newPath))
            File.Delete(newPath);

        File.Move(oldPath, newPath);
    }

    private static string FlagPath(string replayPath, string flag)
        => $"{replayPath}.{flag}";
}