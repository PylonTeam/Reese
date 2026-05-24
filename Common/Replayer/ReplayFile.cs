using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reese.Common.Replayer;

[Flags]
public enum ReplayFileFlags : byte
{
    None = 0,
    New = 1,
    Watched = 2,
    Favorite = 4,
    All = New | Watched | Favorite
}

// FIXME: Some of the bullshit we do would be better buffered instead of manually counting bytes, in both directions.

public readonly record struct ReplayBaselineEntry(uint Tick, long DataOffset, int DataLength, long ResumeOffset);

public class ReplayFile : IDisposable
{
    public const string Identifier = "Reese";
    private static readonly byte[] IdentifierASCII = Encoding.ASCII.GetBytes(Identifier);
    private static readonly byte[] MetadataMarkerASCII = Encoding.ASCII.GetBytes("RMD1");

    // Baseline blocks are embedded alongside packet blocks as:
    // [uint deltaTick][int -1][int baselineByteLength][baseline bytes].
    // Old readers rejected negative lengths; new normal playback skips this marker.
    private const int BaselineMarkerLength = -1;

    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;
    private readonly List<ReplayBaselineEntry> baselines = [];

    public uint Tick { get; private set; }
    public IReadOnlyList<ReplayBaselineEntry> Baselines => baselines;

    // FIXME: We should just buffer this.
    public int NumberOfPacketDataBytesRemaining { get; private set; }
    public bool ReachedTerminator { get; private set; }
    private bool readingBaselineData;
    private long baselineResumeOffset;

    private ReplayFile()
    {
    }

    private void WriteIdentifier()
    {
        _binaryWriter.Write(IdentifierASCII);
    }

    private static readonly byte[] FlagsMarkerASCII = Encoding.ASCII.GetBytes("RFL1");

    private void ReadPacketDataHeader()
    {
        if (ReachedTerminator)
            return;

        while (!ReachedTerminator)
        {
            // FIXME: This seems like a shitty way to handle EOF? idek
            try
            {
                Tick += _binaryReader.ReadUInt32();
                NumberOfPacketDataBytesRemaining = _binaryReader.ReadInt32();

                if (NumberOfPacketDataBytesRemaining == 0)
                {
                    ReachedTerminator = true;
                    return;
                }

                if (NumberOfPacketDataBytesRemaining == BaselineMarkerLength)
                {
                    int baselineByteLength = _binaryReader.ReadInt32();
                    if (baselineByteLength < 0 ||
                        _binaryReader.BaseStream.Position + baselineByteLength > _binaryReader.BaseStream.Length)
                    {
                        NumberOfPacketDataBytesRemaining = 0;
                        ReachedTerminator = true;
                        return;
                    }

                    _binaryReader.BaseStream.Seek(baselineByteLength, SeekOrigin.Current);
                    NumberOfPacketDataBytesRemaining = 0;
                    continue;
                }

                if (NumberOfPacketDataBytesRemaining < 0)
                {
                    NumberOfPacketDataBytesRemaining = 0;
                    ReachedTerminator = true;
                    return;
                }

                return;
            }
            catch (EndOfStreamException)
            {
                NumberOfPacketDataBytesRemaining = 0;
                ReachedTerminator = true;
            }
        }
    }

    public int ReadPacketData(Span<byte> data)
    {
        var numberOfBytesRead = _binaryReader.Read(data[..Math.Min(data.Length, NumberOfPacketDataBytesRemaining)]);
        NumberOfPacketDataBytesRemaining = Math.Max(0, NumberOfPacketDataBytesRemaining - numberOfBytesRead);

        if (NumberOfPacketDataBytesRemaining == 0)
        {
            if (readingBaselineData)
            {
                _binaryReader.BaseStream.Seek(baselineResumeOffset, SeekOrigin.Begin);
                readingBaselineData = false;
                baselineResumeOffset = 0;
            }

            ReadPacketDataHeader();
        }

        return numberOfBytesRead;
    }

    // FIXME: Ever heard of async? We have the opportunity upstream.
    public void WritePacketData(byte[] data, uint tick)
    {
        if (_binaryWriter == null)
            throw new InvalidOperationException("ReplayFile is not open for writing.");

        if (tick < Tick)
            throw new InvalidOperationException("Cannot write packet data into the past.");

        if (NumberOfPacketDataBytesRemaining > 0 && tick != Tick)
            FlushTick();

        if (NumberOfPacketDataBytesRemaining == 0)
        {
            _binaryWriter.Write(tick - Tick); // delta to THIS block
            _binaryWriter.Write(0);           // placeholder length
            Tick = tick;
        }

        _binaryWriter.Write(data);
        NumberOfPacketDataBytesRemaining += data.Length;
    }

    public void WriteBaselineData(byte[] data, uint tick)
    {
        if (_binaryWriter == null)
            throw new InvalidOperationException("ReplayFile is not open for writing.");

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (tick < Tick)
            throw new InvalidOperationException("Cannot write baseline data into the past.");

        FlushTick();

        _binaryWriter.Write(tick - Tick);
        _binaryWriter.Write(BaselineMarkerLength);
        _binaryWriter.Write(data.Length);

        long dataOffset = _binaryWriter.BaseStream.Position;
        _binaryWriter.Write(data);
        long resumeOffset = _binaryWriter.BaseStream.Position;

        Tick = tick;
        NumberOfPacketDataBytesRemaining = 0;
        baselines.Add(new ReplayBaselineEntry(tick, dataOffset, data.Length, resumeOffset));
    }

    public void FlushTick()
    {
        if (NumberOfPacketDataBytesRemaining == 0)
            return;

        _binaryWriter.Seek(-NumberOfPacketDataBytesRemaining - sizeof(int), SeekOrigin.Current);
        _binaryWriter.Write(NumberOfPacketDataBytesRemaining);
        _binaryWriter.Seek(0, SeekOrigin.End);

        NumberOfPacketDataBytesRemaining = 0;
    }

    public void Finish(uint finalTick, string worldName, string[] modNames, ReplayFileFlags flags = ReplayFileFlags.None)
    {
        if (_binaryWriter == null)
            return;

        if (finalTick < Tick)
            finalTick = Tick;

        FlushTick();

        _binaryWriter.Write(finalTick - Tick); // delta to recording end
        _binaryWriter.Write(0);                // terminator

        WriteMetadata(worldName, modNames);
        WriteFlags(flags);
        _binaryWriter.Flush();

        Tick = finalTick;
    }

    private void WriteMetadata(string worldName, string[] modNames)
    {
        _binaryWriter.Write(MetadataMarkerASCII);
        _binaryWriter.Write(string.IsNullOrWhiteSpace(worldName) ? "Unknown" : worldName.Trim());

        string[] names = (modNames ?? [])
                     .Where(name => name != "ModLoader") // don't count modloader itself as a mod
                     .ToArray();

        _binaryWriter.Write(names.Length);

        foreach (string name in names)
            _binaryWriter.Write(name ?? string.Empty);
    }

    private void WriteFlags(ReplayFileFlags flags)
    {
        flags = CleanFlags(flags);

        if (flags == ReplayFileFlags.None)
            return;

        WriteFlags(_binaryWriter, flags);
    }

    public static ReplayFile Write(Stream stream)
    {
        var replayFile = new ReplayFile
        {
            _binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true)
        };

        replayFile.WriteIdentifier();

        return replayFile;
    }

    public static ReplayFile Read(Stream stream)
    {
        var replayFile = new ReplayFile
        {
            _binaryReader = new BinaryReader(stream, Encoding.UTF8, true)
        };

        var identifier = replayFile._binaryReader.ReadBytes(Identifier.Length);
        if (!identifier.SequenceEqual(IdentifierASCII))
            throw new InvalidDataException("Not a Reese file");

        replayFile.BuildBaselineIndex();

        // Do this once right now, so we have some data to deal in once it comes time to.
        // FIXME: This honestly smells like implementation detail from upstream (them checking on how many bytes we have)
        //        but honestly it might be okay to assume that if we have data we should say we do. yeah i agree hard with that.
        replayFile.ReadPacketDataHeader();

        return replayFile;
    }

    public void Dispose()
    {
        _binaryWriter?.Dispose();
        _binaryReader?.Dispose();

        // We told both binary streams to leaveOpen, so let's close it once ourselves now.
        if (_binaryReader != null)
            _binaryReader.BaseStream.Dispose();
        else if (_binaryWriter != null)
            _binaryWriter.BaseStream.Dispose();
    }

    public void Reset()
    {
        if (_binaryReader == null)
            throw new InvalidOperationException("ReplayFile is not open for reading.");

        _binaryReader.BaseStream.Seek(IdentifierASCII.Length, SeekOrigin.Begin);
        Tick = 0;
        NumberOfPacketDataBytesRemaining = 0;
        ReachedTerminator = false;
        readingBaselineData = false;
        baselineResumeOffset = 0;
        ReadPacketDataHeader(); // Prime the first header so ReadPacketData has data ready to go
    }

    public ReplayBaselineEntry? GetNearestBaselineBefore(uint targetTick)
    {
        ReplayBaselineEntry? nearest = null;

        foreach (ReplayBaselineEntry baseline in baselines)
        {
            if (baseline.Tick > targetTick)
                break;

            nearest = baseline;
        }

        return nearest;
    }

    public bool SeekToBaseline(ReplayBaselineEntry baseline)
    {
        if (_binaryReader == null)
            throw new InvalidOperationException("ReplayFile is not open for reading.");

        if (baseline.DataOffset < IdentifierASCII.Length ||
            baseline.DataLength < 0 ||
            baseline.DataOffset + baseline.DataLength > _binaryReader.BaseStream.Length)
            return false;

        _binaryReader.BaseStream.Seek(baseline.DataOffset, SeekOrigin.Begin);
        Tick = baseline.Tick;
        NumberOfPacketDataBytesRemaining = baseline.DataLength;
        ReachedTerminator = false;
        readingBaselineData = true;
        baselineResumeOffset = baseline.ResumeOffset;

        if (NumberOfPacketDataBytesRemaining == 0)
        {
            readingBaselineData = false;
            _binaryReader.BaseStream.Seek(baselineResumeOffset, SeekOrigin.Begin);
            baselineResumeOffset = 0;
            ReadPacketDataHeader();
        }

        return true;
    }

    public bool HasPendingDataAtOrBefore(uint targetTick)
    {
        if (ReachedTerminator)
            return false;

        return Tick <= targetTick && (readingBaselineData || NumberOfPacketDataBytesRemaining > 0);
    }

    private void BuildBaselineIndex()
    {
        baselines.Clear();

        Stream stream = _binaryReader.BaseStream;
        long initialPosition = stream.Position;
        uint absoluteTick = 0;

        try
        {
            while (stream.Position + sizeof(uint) + sizeof(int) <= stream.Length)
            {
                uint delta = _binaryReader.ReadUInt32();
                int length = _binaryReader.ReadInt32();
                absoluteTick += delta;

                if (length == 0)
                    break;

                if (length == BaselineMarkerLength)
                {
                    if (stream.Position + sizeof(int) > stream.Length)
                        break;

                    int baselineByteLength = _binaryReader.ReadInt32();
                    long dataOffset = stream.Position;

                    if (baselineByteLength < 0 || dataOffset + baselineByteLength > stream.Length)
                        break;

                    long resumeOffset = dataOffset + baselineByteLength;
                    baselines.Add(new ReplayBaselineEntry(absoluteTick, dataOffset, baselineByteLength, resumeOffset));
                    stream.Seek(baselineByteLength, SeekOrigin.Current);
                    continue;
                }

                if (length < 0 || stream.Position + length > stream.Length)
                    break;

                stream.Seek(length, SeekOrigin.Current);
            }
        }
        finally
        {
            stream.Seek(initialPosition, SeekOrigin.Begin);
            Log.Info($"Replay file baseline index built: {baselines.Count} baselines found.");
        }
    }

    #region Metadata reading
    public static bool TryReadDurationTicks(string path, out uint durationTicks)
    {
        durationTicks = 0;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            if (!identifier.SequenceEqual(IdentifierASCII))
                return false;

            long totalTicks = 0;
            int chunkCount = 0;
            bool foundTerminator = false;

            while (stream.Position + 8 <= stream.Length)
            {
                uint delta = reader.ReadUInt32();
                int length = reader.ReadInt32();

                totalTicks += delta;
                if (totalTicks > uint.MaxValue)
                    return false;

                if (length == 0)
                {
                    foundTerminator = true;
                    break;
                }

                if (length == BaselineMarkerLength)
                {
                    if (stream.Position + sizeof(int) > stream.Length)
                        return false;

                    int baselineByteLength = reader.ReadInt32();
                    if (baselineByteLength < 0 || stream.Position + baselineByteLength > stream.Length)
                        return false;

                    stream.Seek(baselineByteLength, SeekOrigin.Current);
                    continue;
                }

                if (length < 0)
                    return false;

                if (stream.Position + length > stream.Length)
                    return false;

                stream.Seek(length, SeekOrigin.Current);
                chunkCount++;
            }

            if (!foundTerminator || chunkCount == 0)
                return false;

            durationTicks = (uint)totalTicks;
            return true;
        }
        catch
        {
            durationTicks = 0;
            return false;
        }
    }
    public static bool TryReadSummary(string path, out uint durationTicks, out string worldName, out string[] modNames)
    {
        return TryReadCatalogInfo(path, out durationTicks, out worldName, out modNames, out _);
    }

    public static bool TryReadCatalogInfo(string path, out uint durationTicks, out string worldName, out string[] modNames, out ReplayFileFlags flags)
    {
        durationTicks = 0;
        worldName = null;
        modNames = null;
        flags = ReplayFileFlags.None;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            if (!identifier.SequenceEqual(IdentifierASCII))
                return false;

            long totalTicks = 0;
            int chunkCount = 0;

            while (stream.Position + 8 <= stream.Length)
            {
                uint delta = reader.ReadUInt32();
                int length = reader.ReadInt32();

                totalTicks += delta;
                if (totalTicks > uint.MaxValue)
                    return false;

                if (length == 0)
                {
                    durationTicks = chunkCount > 0 ? (uint)totalTicks : 0;
                    TryReadMetadataAndFlags(reader, out worldName, out modNames, out flags);
                    return chunkCount > 0;
                }

                if (length == BaselineMarkerLength)
                {
                    if (stream.Position + sizeof(int) > stream.Length)
                        return false;

                    int baselineByteLength = reader.ReadInt32();
                    if (baselineByteLength < 0 || stream.Position + baselineByteLength > stream.Length)
                        return false;

                    stream.Seek(baselineByteLength, SeekOrigin.Current);
                    continue;
                }

                if (length < 0)
                    return false;

                if (stream.Position + length > stream.Length)
                    return false;

                stream.Seek(length, SeekOrigin.Current);
                chunkCount++;
            }

            return false;
        }
        catch
        {
            durationTicks = 0;
            worldName = null;
            modNames = null;
            flags = ReplayFileFlags.None;
            return false;
        }
    }

    public static bool TryAppendFlags(string path, ReplayFileFlags flags, bool preserveLastWriteTime = true, bool validateFile = true)
    {
        flags = CleanFlags(flags);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        if (validateFile && !TryReadCatalogInfo(path, out _, out _, out _, out _))
            return false;

        try
        {
            DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);

            using var stream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.End);

            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            WriteFlags(writer, flags);
            writer.Flush();

            if (preserveLastWriteTime)
                File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadMetadataAndFlags(BinaryReader reader, out string worldName, out string[] modNames, out ReplayFileFlags flags)
    {
        try
        {
            return ReadMetadataAndFlags(reader, out worldName, out modNames, out flags);
        }
        catch
        {
            worldName = null;
            modNames = null;
            flags = ReplayFileFlags.None;
            return false;
        }
    }

    private static bool ReadMetadataAndFlags(BinaryReader reader, out string worldName, out string[] modNames, out ReplayFileFlags flags)
    {
        worldName = null;
        modNames = null;
        flags = ReplayFileFlags.None;

        Stream stream = reader.BaseStream;
        if (stream.Position + MetadataMarkerASCII.Length > stream.Length)
            return false;

        byte[] marker = reader.ReadBytes(MetadataMarkerASCII.Length);
        if (!marker.SequenceEqual(MetadataMarkerASCII))
            return false;

        worldName = reader.ReadString();

        int modCount = reader.ReadInt32();
        if (modCount < 0 || modCount > 4096)
            return false;

        modNames = new string[modCount];
        for (int i = 0; i < modCount; i++)
            modNames[i] = reader.ReadString();

        ReadFlagTrailers(reader, out flags);
        return true;
    }

    private static void ReadFlagTrailers(BinaryReader reader, out ReplayFileFlags flags)
    {
        flags = ReplayFileFlags.None;

        Stream stream = reader.BaseStream;
        while (stream.Position + FlagsMarkerASCII.Length + sizeof(byte) <= stream.Length)
        {
            byte[] marker = reader.ReadBytes(FlagsMarkerASCII.Length);
            if (!marker.SequenceEqual(FlagsMarkerASCII))
                return;

            flags = CleanFlags((ReplayFileFlags)reader.ReadByte());
        }
    }

    private static void WriteFlags(BinaryWriter writer, ReplayFileFlags flags)
    {
        writer.Write(FlagsMarkerASCII);
        writer.Write((byte)CleanFlags(flags));
    }

    private static ReplayFileFlags CleanFlags(ReplayFileFlags flags)
    {
        return flags & ReplayFileFlags.All;
    }

    #endregion
}
