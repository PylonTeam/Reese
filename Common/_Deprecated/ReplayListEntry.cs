//using Reese.Core.Debug;
//using System;
//using System.IO;

//namespace Reese.Common.MainMenu;

//public readonly struct ReplayMetadata
//{
//    public readonly string FullPath;
//    public readonly ReplayMetadata Info;
//    public readonly string Name;
//    public readonly string WorldName;
//    public readonly DateTime Date;
//    public readonly string DateText;
//    public readonly TimeSpan Duration;
//    public readonly string DurationText;
//    public readonly uint DurationTicks;
//    public readonly long SizeBytes;
//    public readonly string SizeText;
//    public readonly bool IsFavorite;

//    private ReplayMetadata(string fullPath, ReplayMetadata info, bool isFavorite)
//    {
//        info ??= ReplayMetadata.FromFile(fullPath);

//        FullPath = string.IsNullOrWhiteSpace(fullPath) ? info.FullPath : fullPath;
//        Info = info;
//        Name = EmptyToError(info.FileName);
//        WorldName = EmptyToError(info.WorldName);
//        Date = info.Date;
//        DateText = info.DateText;
//        Duration = info.Duration;
//        DurationText = info.DurationText;
//        DurationTicks = info.DurationTicks;
//        SizeBytes = info.FileSizeBytes;
//        SizeText = info.FileSizeText;
//        IsFavorite = isFavorite;
//    }

//    public static ReplayMetadata FromFile(string fullPath)
//    {
//        ReplayMetadata info = ReplayMetadata.FromFile(fullPath);
//        return new ReplayMetadata(fullPath, info, ReplayFavorites.IsFavorite(fullPath));
//    }

//    public ReplayMetadata WithCurrentFlags()
//    {
//        return new ReplayMetadata(FullPath, Info, ReplayFavorites.IsFavorite(FullPath));
//    }

//    private static string EmptyToError(string value)
//    {
//        return string.IsNullOrWhiteSpace(value) ? "Error" : value.Trim();
//    }
//}