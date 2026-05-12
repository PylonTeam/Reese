using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese;

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

    public static ReplayMetadata FromFile(string path)
    {
        bool fileExists = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        string fileName = fileExists ? Path.GetFileName(path) : "Error";

        if (!fileExists)
            Log.Warn($"Replay file missing: {path}");

        uint durationTicks = 0;
        bool hasDuration = fileExists && ReplayFile.TryReadDurationTicks(path, out durationTicks);

        return new ReplayMetadata
        {
            FullPath = path ?? string.Empty,
            FileName = fileName,
            ReplayName = EmptyToError(Path.GetFileNameWithoutExtension(fileName)),
            WorldName = "Unknown",
            DurationTicks = hasDuration ? durationTicks : 0,
            DateCreated = fileExists ? File.GetLastWriteTime(path) : DateTime.MinValue,
            SizeBytes = fileExists ? new FileInfo(path).Length : 0,
            ModNames = fileExists ? ReadModNames(path) : null
        };
    }

    private static string[] ReadModNames(string path)
    {
        // Return null when metadata could not be read.
        // Return [] only when metadata was read and there are genuinely no mods.
        return null;
    }

    private static string EmptyToError(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Error" : value.Trim();
    }
}