using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Common.Replayer;

public sealed class ReplayMetadata
{
    public string FullPath { get; init; }
    public string FileName { get; init; }
    public string ReplayName { get; init; }
    public string WorldName { get; init; }
    public uint DurationTicks { get; init; }
    public DateTime DateCreated { get; init; }
    public long SizeBytes { get; init; }
    public string[] ModNames { get; init; }

    /// <summary>
    /// Reads file info
    /// Read replay header if possible
    /// Scan duration if possible
    /// Returns: metadata to display.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static ReplayMetadata FromFile(string path)
    {
        bool fileExists = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        string fileName = fileExists ? Path.GetFileName(path) : "Error";

        if (!fileExists)
            Log.Warn($"Replay file missing: {path}");

        uint durationTicks = 0;
        string worldName = null;
        string[] modNames = null;

        bool hasSummary = fileExists && ReplayFile.TryReadSummary(path, out durationTicks, out worldName, out modNames);

        return new ReplayMetadata
        {
            FullPath = path ?? string.Empty,
            FileName = fileName,
            ReplayName = EmptyToError(Path.GetFileNameWithoutExtension(fileName)),
            WorldName = EmptyToUnknown(worldName),
            DurationTicks = hasSummary ? durationTicks : 0,
            DateCreated = fileExists ? File.GetLastWriteTime(path) : DateTime.MinValue,
            SizeBytes = fileExists ? new FileInfo(path).Length : 0,
            ModNames = modNames
        };
    }

    private static string EmptyToUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
    }


    private static string EmptyToError(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Error" : value.Trim();
    }
}