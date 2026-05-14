using Reese.Common.Replayer;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Reese.Common.MainMenu;

/// <summary>
/// Keeps track of and displays new/played/favorite flags for a replay.
/// </summary>
internal static class ReplayFlags
{
    private const byte New = 1;
    private const byte Watched = 2;
    private const byte Favorite = 4;
    private const byte AllFlags = New | Watched | Favorite;

    private static readonly byte[] IdentifierASCII = Encoding.ASCII.GetBytes(ReplayFile.Identifier);
    private static readonly byte[] MetadataMarkerASCII = Encoding.ASCII.GetBytes("RMD1");
    private static readonly byte[] FlagsMarkerASCII = Encoding.ASCII.GetBytes("RFL1");

    public static bool IsNew(string replayPath) => HasFlag(replayPath, New);
    public static bool HasWatched(string replayPath) => HasFlag(replayPath, Watched);
    public static bool IsFavorite(string replayPath) => HasFlag(replayPath, Favorite);

    public static void MarkNew(string replayPath) => SetFlag(replayPath, New, true);

    public static void MarkWatched(string replayPath)
    {
        WriteFlags(replayPath, (byte)((ReadFlags(replayPath) & ~New) | Watched));
    }

    public static void ToggleFavorite(string replayPath)
    {
        SetFlag(replayPath, Favorite, !IsFavorite(replayPath));
    }

    public static void Delete(string replayPath)
    {
    }

    public static void Move(string oldReplayPath, string newReplayPath)
    {
    }

    private static bool HasFlag(string replayPath, byte flag) => (ReadFlags(replayPath) & flag) != 0;

    private static void SetFlag(string replayPath, byte flag, bool enabled)
    {
        byte flags = ReadFlags(replayPath);
        WriteFlags(replayPath, enabled ? (byte)(flags | flag) : (byte)(flags & ~flag));
    }

    private static byte ReadFlags(string replayPath)
    {
        if (string.IsNullOrWhiteSpace(replayPath) || !File.Exists(replayPath))
            return 0;

        try
        {
            using var stream = File.Open(replayPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            if (!ReadMarker(reader, IdentifierASCII) || !SkipPacketData(reader) || !ReadMarker(reader, MetadataMarkerASCII))
                return 0;

            _ = reader.ReadString();
            int modCount = reader.ReadInt32();
            if (modCount < 0 || modCount > 4096)
                return 0;

            for (int i = 0; i < modCount; i++)
                _ = reader.ReadString();

            byte flags = 0;
            while (stream.Position + FlagsMarkerASCII.Length + sizeof(byte) <= stream.Length)
            {
                if (!ReadMarker(reader, FlagsMarkerASCII))
                    return flags;

                flags = (byte)(reader.ReadByte() & AllFlags);
            }

            return flags;
        }
        catch
        {
            return 0;
        }
    }

    private static void WriteFlags(string replayPath, byte flags)
    {
        if (string.IsNullOrWhiteSpace(replayPath) || !File.Exists(replayPath))
            return;

        if (!ReplayFile.TryReadSummary(replayPath, out _, out _, out _))
            return;

        try
        {
            DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(replayPath);

            using var stream = File.Open(replayPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.End);

            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(FlagsMarkerASCII);
            writer.Write((byte)(flags & AllFlags));
            writer.Flush();

            File.SetLastWriteTimeUtc(replayPath, lastWriteTimeUtc);
        }
        catch
        {
        }
    }

    private static bool SkipPacketData(BinaryReader reader)
    {
        Stream stream = reader.BaseStream;

        while (stream.Position + sizeof(uint) + sizeof(int) <= stream.Length)
        {
            _ = reader.ReadUInt32();
            int length = reader.ReadInt32();

            if (length < 0 || stream.Position + length > stream.Length)
                return false;

            if (length == 0)
                return true;

            stream.Seek(length, SeekOrigin.Current);
        }

        return false;
    }

    private static bool ReadMarker(BinaryReader reader, byte[] marker)
    {
        return reader.ReadBytes(marker.Length).SequenceEqual(marker);
    }
}
