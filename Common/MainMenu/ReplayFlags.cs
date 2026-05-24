using Reese.Common.Replayer;

namespace Reese.Common.MainMenu;

/// <summary>
/// Keeps track of and displays new/played/favorite flags for a replay.
/// </summary>
internal static class ReplayFlags
{
    public static bool IsNew(string replayPath) => HasFlag(replayPath, ReplayFileFlags.New);
    public static bool HasWatched(string replayPath) => HasFlag(replayPath, ReplayFileFlags.Watched);
    public static bool IsFavorite(string replayPath) => HasFlag(replayPath, ReplayFileFlags.Favorite);

    public static ReplayFileFlags MarkNew(string replayPath)
    {
        return SetFlag(replayPath, ReplayFileFlags.New, true);
    }

    public static ReplayFileFlags MarkWatched(string replayPath)
    {
        if (!TryRead(replayPath, out ReplayFileFlags flags))
            return ReplayFileFlags.None;

        ReplayFileFlags updatedFlags = (flags & ~ReplayFileFlags.New) | ReplayFileFlags.Watched;
        TryWrite(replayPath, updatedFlags, validateFile: false);
        return Clean(updatedFlags);
    }

    public static bool TryToggleFavorite(string replayPath, out ReplayFileFlags updatedFlags)
    {
        if (!TryRead(replayPath, out ReplayFileFlags flags))
        {
            updatedFlags = ReplayFileFlags.None;
            return false;
        }

        updatedFlags = flags.HasFlag(ReplayFileFlags.Favorite)
            ? flags & ~ReplayFileFlags.Favorite
            : flags | ReplayFileFlags.Favorite;

        updatedFlags = Clean(updatedFlags);
        return TryWrite(replayPath, updatedFlags, validateFile: false);
    }

    public static ReplayFileFlags ToggleFavorite(string replayPath)
    {
        TryToggleFavorite(replayPath, out ReplayFileFlags updatedFlags);
        return updatedFlags;
    }

    public static void Delete(string replayPath)
    {
    }

    public static void Move(string oldReplayPath, string newReplayPath)
    {
    }

    private static bool HasFlag(string replayPath, ReplayFileFlags flag)
    {
        return Read(replayPath).HasFlag(flag);
    }

    private static ReplayFileFlags SetFlag(string replayPath, ReplayFileFlags flag, bool enabled)
    {
        if (!TryRead(replayPath, out ReplayFileFlags flags))
            return ReplayFileFlags.None;

        ReplayFileFlags updatedFlags = enabled ? flags | flag : flags & ~flag;
        TryWrite(replayPath, updatedFlags, validateFile: false);
        return Clean(updatedFlags);
    }

    private static ReplayFileFlags Read(string replayPath)
    {
        return TryRead(replayPath, out ReplayFileFlags flags) ? flags : ReplayFileFlags.None;
    }

    private static bool TryRead(string replayPath, out ReplayFileFlags flags)
    {
        return ReplayFile.TryReadCatalogInfo(replayPath, out _, out _, out _, out flags);
    }

    private static bool TryWrite(string replayPath, ReplayFileFlags flags, bool validateFile)
    {
        return ReplayFile.TryAppendFlags(replayPath, Clean(flags), preserveLastWriteTime: true, validateFile: validateFile);
    }

    private static ReplayFileFlags Clean(ReplayFileFlags flags)
    {
        return flags & ReplayFileFlags.All;
    }
}
