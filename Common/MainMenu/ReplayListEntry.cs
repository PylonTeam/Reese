using Reese.Core.Debug;
using System;
using System.IO;

namespace Reese.Common.MainMenu;


public readonly struct ReplayListEntry
{
    public readonly string FullPath;
    public readonly string Name;
    public readonly DateTime Date;
    public readonly TimeSpan Duration;
    public readonly uint DurationTicks;
    public readonly long SizeBytes;

    private ReplayListEntry(string fullPath, ReplayDisplayInfo info)
    {
        FullPath = fullPath;
        Name = Path.GetFileNameWithoutExtension(fullPath);
        Date = info.Date;
        Duration = info.Duration;
        DurationTicks = info.DurationTicks;
        SizeBytes = info.FileSizeBytes;
    }

    public static ReplayListEntry FromFile(string fullPath)
    {
        ReplayDisplayInfo info = ReplayDisplayInfo.FromFile(fullPath);
        return new ReplayListEntry(fullPath, info);
    }
}