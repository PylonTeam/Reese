using Reese.Core.Debug;
using System;
using System.IO;

namespace Reese.Common.MainMenu;

public readonly struct ReplayListEntry
{
    public readonly string FullPath;
    public readonly ReplayDisplayInfo Info;
    public readonly string Name;
    public readonly DateTime Date;
    public readonly TimeSpan Duration;
    public readonly uint DurationTicks;
    public readonly long SizeBytes;
    public readonly bool IsFavorite;

    private ReplayListEntry(string fullPath, ReplayDisplayInfo info, bool isFavorite)
    {
        FullPath = fullPath;
        Info = info;
        Name = Path.GetFileNameWithoutExtension(fullPath);
        Date = info.Date;
        Duration = info.Duration;
        DurationTicks = info.DurationTicks;
        SizeBytes = info.FileSizeBytes;
        IsFavorite = isFavorite;
    }

    public static ReplayListEntry FromFile(string fullPath)
    {
        return new ReplayListEntry(fullPath, ReplayDisplayInfo.FromFile(fullPath), ReplayFavorites.IsFavorite(fullPath));
    }

    public ReplayListEntry WithCurrentFlags()
    {
        return new ReplayListEntry(FullPath, Info, ReplayFavorites.IsFavorite(FullPath));
    }
}