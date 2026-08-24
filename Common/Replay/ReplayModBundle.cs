using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reese.Common.Replayer;

public sealed class ReplayModBundle
{
    public const ushort Version = 1;

    public ReplayModFile[] Mods { get; init; } = [];

    public bool IsEmpty => Mods.Length == 0;

    public static ReplayModBundle CaptureLoadedMods()
    {
        List<ReplayModFile> mods = [];
        string[] candidateNames = ModLoader.Mods
            .Select(x => x?.Name)
            .Where(x => !ShouldIgnoreMod(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Log.Info($"Capturing replay mod bundle for {candidateNames.Length} loaded mods: {string.Join(", ", candidateNames)}");

        foreach (Mod mod in ModLoader.Mods)
        {
            if (ShouldIgnoreMod(mod?.Name))
                continue;

            string path = mod?.File?.path;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Log.Warn($"Could not bundle replay mod {mod?.Name ?? "unknown"} because its .tmod file is missing.");
                continue;
            }

            byte[] payload;
            try
            {
                using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                payload = new byte[stream.Length];
                int total = 0;
                while (total < payload.Length)
                {
                    int read = stream.Read(payload, total, payload.Length - total);
                    if (read <= 0)
                        break;

                    total += read;
                }

                if (total != payload.Length)
                    Array.Resize(ref payload, total);
            }
            catch (Exception e)
            {
                Log.Warn($"Could not read .tmod file for replay mod {mod.Name}: {e}");
                continue;
            }

            byte[] hash = mod.File?.Hash;
            if (hash == null || hash.Length == 0)
            {
                Log.Warn($"Could not bundle replay mod {mod.Name} because its .tmod hash is unavailable.");
                continue;
            }

            mods.Add(new ReplayModFile
            {
                Name = mod.Name,
                DisplayName = string.IsNullOrWhiteSpace(mod.DisplayName) ? mod.Name : mod.DisplayName,
                Version = mod.Version?.ToString() ?? "0.0",
                TModLoaderVersion = mod.TModLoaderVersion?.ToString() ?? string.Empty,
                Hash = hash.ToArray(),
                PayloadLength = payload.LongLength,
                Payload = payload
            });

            Log.Info($"Bundled replay mod {mod.Name} v{mod.Version} ({ReplayModFile.FormatBytes(payload.LongLength)}, hash {ReplayModFile.ShortHash(hash)}) from {path}");
        }

        ReplayModBundle bundle = new()
        {
            Mods =
            [
                .. mods
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            ]
        };

        Log.Info($"Replay mod bundle capture complete: {bundle.Mods.Length} .tmod files, {ReplayModFile.FormatBytes(bundle.Mods.Sum(x => x.PayloadLength))}.");
        return bundle;
    }

    public static bool ShouldIgnoreMod(string modName)
    {
        return string.IsNullOrWhiteSpace(modName) ||
               string.Equals(modName, "ModLoader", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(modName, "Reese", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ReplayModFile
{
    public string Name { get; init; }
    public string DisplayName { get; init; }
    public string Version { get; init; }
    public string TModLoaderVersion { get; init; }
    public byte[] Hash { get; init; } = [];
    public long PayloadOffset { get; init; }
    public long PayloadLength { get; init; }
    public byte[] Payload { get; init; }

    public string FileName => $"{Name}.tmod";

    public System.Version ParsedVersion => TryParseVersion(Version);
    public System.Version ParsedTModLoaderVersion => TryParseVersion(TModLoaderVersion);

    public string HashText => ToHex(Hash);
    public string ShortHashText => ShortHash(Hash);

    public bool HasPayload => Payload != null && Payload.LongLength == PayloadLength;

    private static System.Version TryParseVersion(string value)
    {
        if (System.Version.TryParse(value, out System.Version version))
            return version;

        return new System.Version(0, 0);
    }

    public static string ToHex(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        char[] chars = new char[bytes.Length * 2];
        const string hex = "0123456789abcdef";

        for (int i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = hex[bytes[i] >> 4];
            chars[i * 2 + 1] = hex[bytes[i] & 0xF];
        }

        return new string(chars);
    }

    public static string ShortHash(byte[] bytes)
    {
        string value = ToHex(bytes);
        return value.Length <= 12 ? value : value[..12];
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        int unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    public static byte[] FromHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        value = value.Trim();
        if (value.Length % 2 != 0)
            return [];

        byte[] bytes = new byte[value.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            string pair = value.Substring(i * 2, 2);
            if (!byte.TryParse(pair, System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                return [];
        }

        return bytes;
    }
}
