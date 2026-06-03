using Reese.Common.Replayer.ReplayEvents;
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
    private static readonly byte[] HeaderMarkerASCII = Encoding.ASCII.GetBytes("RHD1");
    private static readonly byte[] MetadataMarkerASCII = Encoding.ASCII.GetBytes("RMD1");
    private static readonly byte[] SummaryMarkerASCII = Encoding.ASCII.GetBytes("RSM1");
    private static readonly byte[] FlagsMarkerASCII = Encoding.ASCII.GetBytes("RFL1");
    private static readonly byte[] EventsMarkerASCII = Encoding.ASCII.GetBytes("REV1");

    // Baseline blocks are embedded alongside packet blocks as:
    // [uint deltaTick][int -1][int baselineByteLength][baseline bytes].
    // Old readers rejected negative lengths; new normal playback skips this marker.
    private const int BaselineMarkerLength = -1;
    private const int BlockHeaderByteLength = sizeof(uint) + sizeof(int);
    private const int MinimumTerrariaPacketLength = 3;
    private const int MaxReasonablePacketBlockLength = 64 * 1024 * 1024;
    private const int MaxReasonableBaselineBlockLength = 512 * 1024 * 1024;
    private const uint SuspiciousDeltaTicks = 10 * 60 * 60;
    private const int RequiredRecoveryBlocks = 3;
    private const long MaxRecoveryScanBytes = 128L * 1024 * 1024;
    private const int MaxCatalogTailScanBytes = 4 * 1024 * 1024;
    private const int MaxMetadataStringByteLength = 16 * 1024;
    private const int MaxReplayEventStringByteLength = 4 * 1024;
    private const int MaxReplayEventCount = 65536;
    private const ushort MinimumReplayEventsVersion = 1;
    private const ushort ReplayEventsVersion = 2;
    private const int PlayerHeadSnapshotByteLength = (22 + 7) * sizeof(int);

    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;
    private readonly List<ReplayBaselineEntry> baselines = [];
    private long dataStartOffset = IdentifierASCII.Length;
    private long headerDurationOffset = -1;

    public uint Tick { get; private set; }
    public IReadOnlyList<ReplayBaselineEntry> Baselines => baselines;

    // FIXME: We should just buffer this.
    public int NumberOfPacketDataBytesRemaining { get; private set; }
    public bool ReachedTerminator { get; private set; }
    private bool readingBaselineData;
    private long baselineResumeOffset;

    private readonly record struct ReplayBlockHeader(
        uint DeltaTick,
        int Length,
        int PayloadLength,
        long HeaderOffset,
        long DataOffset,
        bool IsBaseline,
        bool IsTerminator)
    {
        public long ResumeOffset => DataOffset + PayloadLength;
    }

    private ReplayFile()
    {
    }

    private void WriteIdentifier()
    {
        _binaryWriter.Write(IdentifierASCII);
    }

    private void WriteHeaderPlaceholder()
    {
        _binaryWriter.Write(HeaderMarkerASCII);
        headerDurationOffset = _binaryWriter.BaseStream.Position;
        _binaryWriter.Write(0u);
        _binaryWriter.Write(~0u);
        dataStartOffset = _binaryWriter.BaseStream.Position;
    }

    private void WriteHeaderDuration(uint durationTicks)
    {
        if (headerDurationOffset < 0)
            return;

        Stream stream = _binaryWriter.BaseStream;
        long position = stream.Position;

        stream.Seek(headerDurationOffset, SeekOrigin.Begin);
        _binaryWriter.Write(durationTicks);
        _binaryWriter.Write(~durationTicks);
        stream.Seek(position, SeekOrigin.Begin);
    }

    private void ReadPacketDataHeader()
    {
        if (ReachedTerminator)
            return;

        Stream stream = _binaryReader.BaseStream;

        while (!ReachedTerminator)
        {
            long headerOffset = stream.Position;

            if (!TryReadBlockHeader(_binaryReader, headerOffset, out ReplayBlockHeader header))
            {
                NumberOfPacketDataBytesRemaining = 0;
                ReachedTerminator = true;
                return;
            }

            if (!IsReplayBlockHeaderValid(_binaryReader, header, Tick, validatePacketFrames: false))
            {
                if (!TryRecoverFromInvalidHeader(_binaryReader, headerOffset, Tick, out header))
                {
                    NumberOfPacketDataBytesRemaining = 0;
                    ReachedTerminator = true;
                    return;
                }
            }

            Tick += header.DeltaTick;

            if (header.IsTerminator)
            {
                NumberOfPacketDataBytesRemaining = 0;
                ReachedTerminator = true;
                stream.Seek(header.DataOffset, SeekOrigin.Begin);
                return;
            }

            if (header.IsBaseline)
            {
                stream.Seek(header.ResumeOffset, SeekOrigin.Begin);
                NumberOfPacketDataBytesRemaining = 0;
                continue;
            }

            stream.Seek(header.DataOffset, SeekOrigin.Begin);
            NumberOfPacketDataBytesRemaining = header.PayloadLength;
            return;
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

    public void Finish(uint finalTick, string worldName, string[] modNames, ReplayFileFlags flags = ReplayFileFlags.None, IReadOnlyList<ReplayTimelineEvent> timelineEvents = null)
    {
        if (_binaryWriter == null)
            return;

        if (finalTick < Tick)
            finalTick = Tick;

        FlushTick();

        _binaryWriter.Write(finalTick - Tick); // delta to recording end
        _binaryWriter.Write(0);                // terminator

        WriteHeaderDuration(finalTick);
        WriteMetadata(finalTick, worldName, modNames);
        WriteFlags(flags);
        WriteEvents(timelineEvents);
        _binaryWriter.Flush();

        Tick = finalTick;
    }

    private void WriteMetadata(uint durationTicks, string worldName, string[] modNames)
    {
        _binaryWriter.Write(MetadataMarkerASCII);
        _binaryWriter.Write(string.IsNullOrWhiteSpace(worldName) ? "Unknown" : worldName.Trim());

        string[] names = (modNames ?? [])
                     .Where(name => name != "ModLoader") // don't count modloader itself as a mod
                     .ToArray();

        _binaryWriter.Write(names.Length);

        foreach (string name in names)
            _binaryWriter.Write(name ?? string.Empty);

        WriteSummary(durationTicks);
    }

    private void WriteSummary(uint durationTicks)
    {
        _binaryWriter.Write(SummaryMarkerASCII);
        _binaryWriter.Write(durationTicks);
    }

    private void WriteFlags(ReplayFileFlags flags)
    {
        flags = CleanFlags(flags);

        if (flags == ReplayFileFlags.None)
            return;

        WriteFlags(_binaryWriter, flags);
    }

    private void WriteEvents(IReadOnlyList<ReplayTimelineEvent> timelineEvents)
    {
        if (timelineEvents == null || timelineEvents.Count == 0)
            return;

        ReplayTimelineEvent[] sortedEvents =
        [
            .. timelineEvents
                .Where(e => !string.IsNullOrWhiteSpace(e.Text))
                .OrderBy(e => e.Tick)
                .ThenBy(e => e.Category)
                .ThenBy(e => e.Key)
                .Take(MaxReplayEventCount)
        ];

        if (sortedEvents.Length == 0)
            return;

        _binaryWriter.Write(EventsMarkerASCII);
        _binaryWriter.Write(ReplayEventsVersion);
        _binaryWriter.Write(sortedEvents.Length);

        foreach (ReplayTimelineEvent timelineEvent in sortedEvents)
        {
            _binaryWriter.Write(timelineEvent.Tick);
            _binaryWriter.Write((byte)timelineEvent.Category);
            _binaryWriter.Write((byte)timelineEvent.IconKind);
            _binaryWriter.Write(timelineEvent.IconId);
            _binaryWriter.Write(timelineEvent.Key ?? string.Empty);
            _binaryWriter.Write(timelineEvent.Text ?? string.Empty);
            WritePlayerHeadSnapshot(_binaryWriter, timelineEvent.PlayerHead);
        }
    }

    private static void WritePlayerHeadSnapshot(BinaryWriter writer, ReplayPlayerHeadSnapshot? snapshot)
    {
        writer.Write(snapshot.HasValue);

        if (!snapshot.HasValue)
            return;

        ReplayPlayerHeadSnapshot value = snapshot.Value;
        writer.Write(value.PlayerIndex);
        writer.Write(value.Team);
        writer.Write(value.Hair);
        writer.Write(value.HairDye);
        writer.Write(value.SkinVariant);
        writer.Write(value.Head);
        writer.Write(value.Body);
        writer.Write(value.Legs);
        writer.Write(value.CHead);
        writer.Write(value.CBody);
        writer.Write(value.CLegs);
        writer.Write(value.Face);
        writer.Write(value.Neck);
        writer.Write(value.Front);
        writer.Write(value.Back);
        writer.Write(value.Waist);
        writer.Write(value.Shield);
        writer.Write(value.Shoe);
        writer.Write(value.Balloon);
        writer.Write(value.Beard);
        writer.Write(value.HandOn);
        writer.Write(value.HandOff);
        writer.Write(value.HairColor);
        writer.Write(value.SkinColor);
        writer.Write(value.EyeColor);
        writer.Write(value.ShirtColor);
        writer.Write(value.UnderShirtColor);
        writer.Write(value.PantsColor);
        writer.Write(value.ShoeColor);
    }

    public static ReplayFile Write(Stream stream)
    {
        var replayFile = new ReplayFile
        {
            _binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true)
        };

        replayFile.WriteIdentifier();
        replayFile.WriteHeaderPlaceholder();

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

        TryReadHeaderDuration(replayFile._binaryReader, out _, out long dataStartOffset);
        replayFile.dataStartOffset = dataStartOffset;

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

        _binaryReader.BaseStream.Seek(dataStartOffset, SeekOrigin.Begin);
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
            while (stream.Position + BlockHeaderByteLength <= stream.Length)
            {
                long headerOffset = stream.Position;
                if (!TryReadBlockHeader(_binaryReader, headerOffset, out ReplayBlockHeader header))
                    break;

                if (!IsReplayBlockHeaderValid(_binaryReader, header, absoluteTick, validatePacketFrames: false) &&
                    !TryRecoverFromInvalidHeader(_binaryReader, headerOffset, absoluteTick, out header))
                    break;

                absoluteTick += header.DeltaTick;

                if (header.IsTerminator)
                    break;

                if (header.IsBaseline)
                {
                    baselines.Add(new ReplayBaselineEntry(absoluteTick, header.DataOffset, header.PayloadLength, header.ResumeOffset));
                    stream.Seek(header.ResumeOffset, SeekOrigin.Begin);
                    continue;
                }

                stream.Seek(header.ResumeOffset, SeekOrigin.Begin);
            }
        }
        finally
        {
            stream.Seek(initialPosition, SeekOrigin.Begin);
            Log.Info($"Replay file baseline index built: {baselines.Count} baselines found.");
        }
    }

    private static bool TryReadBlockHeader(BinaryReader reader, long headerOffset, out ReplayBlockHeader header)
    {
        header = default;

        Stream stream = reader.BaseStream;
        if (headerOffset < IdentifierASCII.Length || headerOffset + BlockHeaderByteLength > stream.Length)
            return false;

        stream.Seek(headerOffset, SeekOrigin.Begin);

        try
        {
            uint deltaTick = reader.ReadUInt32();
            int length = reader.ReadInt32();
            long dataOffset = stream.Position;

            if (length == 0)
            {
                header = new ReplayBlockHeader(deltaTick, length, 0, headerOffset, dataOffset, IsBaseline: false, IsTerminator: true);
                return true;
            }

            if (length == BaselineMarkerLength)
            {
                if (stream.Position + sizeof(int) > stream.Length)
                    return false;

                int baselineByteLength = reader.ReadInt32();
                header = new ReplayBlockHeader(deltaTick, length, baselineByteLength, headerOffset, stream.Position, IsBaseline: true, IsTerminator: false);
                return true;
            }

            header = new ReplayBlockHeader(deltaTick, length, length, headerOffset, dataOffset, IsBaseline: false, IsTerminator: false);
            return true;
        }
        catch (EndOfStreamException)
        {
            header = default;
            return false;
        }
    }

    private static bool IsReplayBlockHeaderValid(BinaryReader reader, ReplayBlockHeader header, uint currentTick, bool validatePacketFrames)
    {
        Stream stream = reader.BaseStream;
        if (currentTick + (long)header.DeltaTick > uint.MaxValue)
            return false;

        if (header.IsTerminator)
            return IsTerminatorFollowedByMetadataOrEnd(reader, header.DataOffset);

        if (header.PayloadLength < 0)
            return false;

        if (header.DataOffset > stream.Length || header.PayloadLength > stream.Length - header.DataOffset)
            return false;

        int maxBlockLength = header.IsBaseline ? MaxReasonableBaselineBlockLength : MaxReasonablePacketBlockLength;
        if (header.PayloadLength > maxBlockLength)
            return false;

        if ((validatePacketFrames || header.DeltaTick >= SuspiciousDeltaTicks) &&
            !TryValidateTerrariaPacketFrames(reader, header.DataOffset, header.PayloadLength))
            return false;

        return true;
    }

    private static bool TryRecoverFromInvalidHeader(BinaryReader reader, long badHeaderOffset, uint currentTick, out ReplayBlockHeader recoveredHeader)
    {
        Stream stream = reader.BaseStream;
        long originalPosition = stream.Position;

        if (TryFindRecoveryHeader(reader, badHeaderOffset, currentTick, requireDeltaOne: true, out recoveredHeader) ||
            TryFindRecoveryHeader(reader, badHeaderOffset, currentTick, requireDeltaOne: false, out recoveredHeader))
        {
            long skippedBytes = recoveredHeader.HeaderOffset - badHeaderOffset;
            Log.Warn($"Recovered damaged Reese replay stream at offset {badHeaderOffset} by skipping {skippedBytes} bytes.");
            stream.Seek(recoveredHeader.DataOffset, SeekOrigin.Begin);
            return true;
        }

        recoveredHeader = default;
        stream.Seek(originalPosition, SeekOrigin.Begin);
        return false;
    }

    private static bool TryFindRecoveryHeader(BinaryReader reader, long badHeaderOffset, uint currentTick, bool requireDeltaOne, out ReplayBlockHeader recoveredHeader)
    {
        Stream stream = reader.BaseStream;
        long startOffset = badHeaderOffset + 1;
        long endOffset = Math.Min(stream.Length - BlockHeaderByteLength, badHeaderOffset + MaxRecoveryScanBytes);

        for (long candidateOffset = startOffset; candidateOffset <= endOffset; candidateOffset++)
        {
            if (!TryReadBlockHeader(reader, candidateOffset, out ReplayBlockHeader candidate))
                continue;

            if (requireDeltaOne && candidate.DeltaTick != 1)
                continue;

            if (!HasValidRecoveryRun(reader, candidate, currentTick))
                continue;

            recoveredHeader = candidate;
            return true;
        }

        recoveredHeader = default;
        return false;
    }

    private static bool HasValidRecoveryRun(BinaryReader reader, ReplayBlockHeader firstHeader, uint currentTick)
    {
        ReplayBlockHeader header = firstHeader;
        uint tick = currentTick;
        int validDataBlocks = 0;

        while (true)
        {
            if (!IsReplayBlockHeaderValid(reader, header, tick, validatePacketFrames: true))
                return false;

            tick += header.DeltaTick;

            if (header.IsTerminator)
                return validDataBlocks > 0;

            validDataBlocks++;
            if (validDataBlocks >= RequiredRecoveryBlocks)
                return true;

            if (!TryReadBlockHeader(reader, header.ResumeOffset, out header))
                return false;
        }
    }

    private static bool TryValidateTerrariaPacketFrames(BinaryReader reader, long dataOffset, int byteLength)
    {
        Stream stream = reader.BaseStream;
        long originalPosition = stream.Position;

        try
        {
            if (byteLength < 0 || dataOffset > stream.Length || byteLength > stream.Length - dataOffset)
                return false;

            stream.Seek(dataOffset, SeekOrigin.Begin);
            int remaining = byteLength;

            while (remaining > 0)
            {
                if (remaining < MinimumTerrariaPacketLength)
                    return false;

                int low = stream.ReadByte();
                int high = stream.ReadByte();
                if (low < 0 || high < 0)
                    return false;

                int packetLength = low | (high << 8);
                if (packetLength < MinimumTerrariaPacketLength || packetLength > remaining)
                    return false;

                stream.Seek(packetLength - sizeof(ushort), SeekOrigin.Current);
                remaining -= packetLength;
            }

            return true;
        }
        finally
        {
            stream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }

    private static bool IsTerminatorFollowedByMetadataOrEnd(BinaryReader reader, long terminatorDataOffset)
    {
        Stream stream = reader.BaseStream;
        if (terminatorDataOffset == stream.Length)
            return true;

        if (terminatorDataOffset + MetadataMarkerASCII.Length > stream.Length)
            return false;

        long originalPosition = stream.Position;
        try
        {
            stream.Seek(terminatorDataOffset, SeekOrigin.Begin);
            return reader.ReadBytes(MetadataMarkerASCII.Length).SequenceEqual(MetadataMarkerASCII);
        }
        finally
        {
            stream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }

    private static bool TryReadHeaderDuration(BinaryReader reader, out uint durationTicks, out long dataStartOffset)
    {
        durationTicks = 0;

        Stream stream = reader.BaseStream;
        long headerStartOffset = stream.Position;
        dataStartOffset = headerStartOffset;

        if (stream.Position + HeaderMarkerASCII.Length + sizeof(uint) * 2 > stream.Length)
            return false;

        byte[] marker = reader.ReadBytes(HeaderMarkerASCII.Length);
        if (!marker.SequenceEqual(HeaderMarkerASCII))
        {
            stream.Seek(headerStartOffset, SeekOrigin.Begin);
            return false;
        }

        uint value = reader.ReadUInt32();
        uint complement = reader.ReadUInt32();
        if (complement != ~value)
        {
            stream.Seek(headerStartOffset, SeekOrigin.Begin);
            return false;
        }

        durationTicks = value;
        dataStartOffset = stream.Position;
        return true;
    }

    #region Metadata reading
    public static bool TryReadDurationTicks(string path, out uint durationTicks)
    {
        durationTicks = 0;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        if (TryReadDurationTicksFromHeader(path, out durationTicks) && durationTicks > 0)
            return true;

        if (TryReadCatalogInfoFromTail(path, out durationTicks, out _, out _, out _))
            return true;

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

        bool hasHeaderDuration = TryReadDurationTicksFromHeader(path, out uint headerDurationTicks);

        if (TryReadCatalogInfoFromTail(path, out durationTicks, out worldName, out modNames, out flags))
        {
            if (hasHeaderDuration && headerDurationTicks > 0)
                durationTicks = headerDurationTicks;

            return true;
        }

        if (hasHeaderDuration && headerDurationTicks > 0)
        {
            durationTicks = headerDurationTicks;
            return true;
        }

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            if (!identifier.SequenceEqual(IdentifierASCII))
                return false;

            long totalTicks = 0;
            int chunkCount = 0;

            while (stream.Position + BlockHeaderByteLength <= stream.Length)
            {
                long headerOffset = stream.Position;
                if (!TryReadBlockHeader(reader, headerOffset, out ReplayBlockHeader header))
                    return false;

                if (!IsReplayBlockHeaderValid(reader, header, (uint)totalTicks, validatePacketFrames: false) &&
                    !TryRecoverFromInvalidHeader(reader, headerOffset, (uint)totalTicks, out header))
                    return false;

                totalTicks += header.DeltaTick;
                if (totalTicks > uint.MaxValue)
                    return false;

                if (header.IsTerminator)
                {
                    durationTicks = chunkCount > 0 ? (uint)totalTicks : 0;
                    TryReadMetadataAndFlags(reader, out worldName, out modNames, out flags, out uint metadataDurationTicks, out bool hasMetadataDuration);
                    if (durationTicks == 0 && hasMetadataDuration)
                        durationTicks = metadataDurationTicks;

                    return chunkCount > 0;
                }

                if (header.IsBaseline)
                {
                    stream.Seek(header.ResumeOffset, SeekOrigin.Begin);
                    continue;
                }

                stream.Seek(header.ResumeOffset, SeekOrigin.Begin);
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

    public static bool TryReadEvents(string path, out ReplayTimelineEvent[] timelineEvents)
    {
        timelineEvents = [];

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        return TryReadEventsFromTail(path, out timelineEvents);
    }

    private static bool TryReadDurationTicksFromHeader(string path, out uint durationTicks)
    {
        durationTicks = 0;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            return identifier.SequenceEqual(IdentifierASCII) &&
                   TryReadHeaderDuration(reader, out durationTicks, out _);
        }
        catch
        {
            durationTicks = 0;
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

    private static bool TryReadCatalogInfoFromTail(string path, out uint durationTicks, out string worldName, out string[] modNames, out ReplayFileFlags flags)
    {
        durationTicks = 0;
        worldName = null;
        modNames = null;
        flags = ReplayFileFlags.None;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            if (!identifier.SequenceEqual(IdentifierASCII))
                return false;

            int tailLength = (int)Math.Min(MaxCatalogTailScanBytes, Math.Max(0, stream.Length - IdentifierASCII.Length));
            if (tailLength <= MetadataMarkerASCII.Length)
                return false;

            long tailStart = stream.Length - tailLength;
            stream.Seek(tailStart, SeekOrigin.Begin);
            byte[] tail = reader.ReadBytes(tailLength);

            int searchIndex = tail.Length - MetadataMarkerASCII.Length;
            while ((searchIndex = LastIndexOf(tail, MetadataMarkerASCII, searchIndex)) >= 0)
            {
                if (!LooksLikeMetadataAfterTerminator(tail, searchIndex))
                {
                    searchIndex--;
                    continue;
                }

                stream.Seek(tailStart + searchIndex, SeekOrigin.Begin);

                if (TryReadMetadataAndFlags(reader, out string candidateWorldName, out string[] candidateModNames, out ReplayFileFlags candidateFlags, out uint candidateDurationTicks, out bool hasDuration) &&
                    hasDuration &&
                    candidateDurationTicks > 0)
                {
                    durationTicks = candidateDurationTicks;
                    worldName = candidateWorldName;
                    modNames = candidateModNames;
                    flags = candidateFlags;
                    return true;
                }

                searchIndex--;
            }
        }
        catch
        {
            durationTicks = 0;
            worldName = null;
            modNames = null;
            flags = ReplayFileFlags.None;
        }

        return false;
    }

    private static bool TryReadEventsFromTail(string path, out ReplayTimelineEvent[] timelineEvents)
    {
        timelineEvents = [];

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            if (!identifier.SequenceEqual(IdentifierASCII))
                return false;

            int tailLength = (int)Math.Min(MaxCatalogTailScanBytes, Math.Max(0, stream.Length - IdentifierASCII.Length));
            if (tailLength <= MetadataMarkerASCII.Length)
                return false;

            long tailStart = stream.Length - tailLength;
            stream.Seek(tailStart, SeekOrigin.Begin);
            byte[] tail = reader.ReadBytes(tailLength);

            int searchIndex = tail.Length - MetadataMarkerASCII.Length;
            while ((searchIndex = LastIndexOf(tail, MetadataMarkerASCII, searchIndex)) >= 0)
            {
                if (!LooksLikeMetadataAfterTerminator(tail, searchIndex))
                {
                    searchIndex--;
                    continue;
                }

                stream.Seek(tailStart + searchIndex, SeekOrigin.Begin);

                if (TryReadTimelineEventsFromMetadata(reader, out timelineEvents))
                    return true;

                searchIndex--;
            }
        }
        catch
        {
            timelineEvents = [];
        }

        return false;
    }

    private static bool TryReadMetadataAndFlags(BinaryReader reader, out string worldName, out string[] modNames, out ReplayFileFlags flags)
    {
        return TryReadMetadataAndFlags(reader, out worldName, out modNames, out flags, out _, out _);
    }

    private static bool TryReadMetadataAndFlags(BinaryReader reader, out string worldName, out string[] modNames, out ReplayFileFlags flags, out uint durationTicks, out bool hasDuration)
    {
        try
        {
            return ReadMetadataAndFlags(reader, out worldName, out modNames, out flags, out durationTicks, out hasDuration);
        }
        catch
        {
            worldName = null;
            modNames = null;
            flags = ReplayFileFlags.None;
            durationTicks = 0;
            hasDuration = false;
            return false;
        }
    }

    private static bool ReadMetadataAndFlags(BinaryReader reader, out string worldName, out string[] modNames, out ReplayFileFlags flags, out uint durationTicks, out bool hasDuration)
    {
        worldName = null;
        modNames = null;
        flags = ReplayFileFlags.None;
        durationTicks = 0;
        hasDuration = false;

        Stream stream = reader.BaseStream;
        if (stream.Position + MetadataMarkerASCII.Length > stream.Length)
            return false;

        byte[] marker = reader.ReadBytes(MetadataMarkerASCII.Length);
        if (!marker.SequenceEqual(MetadataMarkerASCII))
            return false;

        return TryReadMetadataBodyAndTrailers(reader, out worldName, out modNames, out flags, out durationTicks, out hasDuration);
    }

    private static bool TryReadMetadataBodyAndTrailers(BinaryReader reader, out string worldName, out string[] modNames, out ReplayFileFlags flags, out uint durationTicks, out bool hasDuration)
    {
        worldName = null;
        modNames = null;
        flags = ReplayFileFlags.None;
        durationTicks = 0;
        hasDuration = false;

        Stream stream = reader.BaseStream;

        if (!TryReadBoundedString(reader, out worldName))
            return false;

        if (stream.Position + sizeof(int) > stream.Length)
            return false;

        int modCount = reader.ReadInt32();
        if (modCount < 0 || modCount > 4096)
            return false;

        modNames = new string[modCount];
        for (int i = 0; i < modCount; i++)
        {
            if (!TryReadBoundedString(reader, out modNames[i]))
                return false;
        }

        return TryReadCatalogTrailers(reader, out flags, out durationTicks, out hasDuration);
    }

    private static bool TryReadCatalogTrailers(BinaryReader reader, out ReplayFileFlags flags, out uint durationTicks, out bool hasDuration)
    {
        flags = ReplayFileFlags.None;
        durationTicks = 0;
        hasDuration = false;

        Stream stream = reader.BaseStream;
        while (stream.Position + MetadataMarkerASCII.Length <= stream.Length)
        {
            long markerOffset = stream.Position;
            byte[] marker = reader.ReadBytes(FlagsMarkerASCII.Length);

            if (marker.SequenceEqual(SummaryMarkerASCII))
            {
                if (stream.Position + sizeof(uint) > stream.Length)
                    return false;

                durationTicks = reader.ReadUInt32();
                hasDuration = true;
                continue;
            }

            if (marker.SequenceEqual(FlagsMarkerASCII))
            {
                if (stream.Position + sizeof(byte) > stream.Length)
                    return false;

                flags = CleanFlags((ReplayFileFlags)reader.ReadByte());
                continue;
            }

            if (marker.SequenceEqual(EventsMarkerASCII))
            {
                if (!TryReadReplayEventsBody(reader, out _, store: false))
                    return false;

                continue;
            }

            stream.Seek(markerOffset, SeekOrigin.Begin);
            return true;
        }

        return true;
    }

    private static bool TryReadTimelineEventsFromMetadata(BinaryReader reader, out ReplayTimelineEvent[] timelineEvents)
    {
        timelineEvents = [];

        Stream stream = reader.BaseStream;
        if (stream.Position + MetadataMarkerASCII.Length > stream.Length)
            return false;

        byte[] marker = reader.ReadBytes(MetadataMarkerASCII.Length);
        if (!marker.SequenceEqual(MetadataMarkerASCII))
            return false;

        if (!TrySkipMetadataBody(reader))
            return false;

        while (stream.Position + EventsMarkerASCII.Length <= stream.Length)
        {
            long markerOffset = stream.Position;
            marker = reader.ReadBytes(EventsMarkerASCII.Length);

            if (marker.SequenceEqual(SummaryMarkerASCII))
            {
                if (stream.Position + sizeof(uint) > stream.Length)
                    return false;

                stream.Seek(sizeof(uint), SeekOrigin.Current);
                continue;
            }

            if (marker.SequenceEqual(FlagsMarkerASCII))
            {
                if (stream.Position + sizeof(byte) > stream.Length)
                    return false;

                stream.Seek(sizeof(byte), SeekOrigin.Current);
                continue;
            }

            if (marker.SequenceEqual(EventsMarkerASCII))
                return TryReadReplayEventsBody(reader, out timelineEvents, store: true);

            stream.Seek(markerOffset, SeekOrigin.Begin);
            return true;
        }

        return true;
    }

    private static bool TrySkipMetadataBody(BinaryReader reader)
    {
        Stream stream = reader.BaseStream;

        if (!TryReadBoundedString(reader, out _))
            return false;

        if (stream.Position + sizeof(int) > stream.Length)
            return false;

        int modCount = reader.ReadInt32();
        if (modCount < 0 || modCount > 4096)
            return false;

        for (int i = 0; i < modCount; i++)
        {
            if (!TryReadBoundedString(reader, out _))
                return false;
        }

        return true;
    }

    private static bool TryReadReplayEventsBody(BinaryReader reader, out ReplayTimelineEvent[] timelineEvents, bool store)
    {
        timelineEvents = [];

        Stream stream = reader.BaseStream;
        if (stream.Position + sizeof(ushort) + sizeof(int) > stream.Length)
            return false;

        ushort version = reader.ReadUInt16();
        if (version < MinimumReplayEventsVersion || version > ReplayEventsVersion)
            return false;

        int count = reader.ReadInt32();
        if (count < 0 || count > MaxReplayEventCount)
            return false;

        List<ReplayTimelineEvent> events = store ? new List<ReplayTimelineEvent>(count) : null;

        for (int i = 0; i < count; i++)
        {
            if (stream.Position + sizeof(uint) + sizeof(byte) * 2 + sizeof(int) > stream.Length)
                return false;

            uint tick = reader.ReadUInt32();
            ReplayEventCategory category = (ReplayEventCategory)reader.ReadByte();
            ReplayEventIconKind iconKind = (ReplayEventIconKind)reader.ReadByte();
            int iconId = reader.ReadInt32();

            if (!TryReadBoundedString(reader, out string key, MaxReplayEventStringByteLength) ||
                !TryReadBoundedString(reader, out string text, MaxReplayEventStringByteLength))
                return false;

            ReplayPlayerHeadSnapshot? playerHead = null;
            if (version >= 2)
            {
                if (stream.Position + 1 > stream.Length)
                    return false;

                bool hasPlayerHead = reader.ReadBoolean();
                if (hasPlayerHead)
                {
                    if (!TryReadPlayerHeadSnapshot(reader, out ReplayPlayerHeadSnapshot snapshot))
                        return false;

                    playerHead = snapshot;
                }
            }

            if (store)
            {
                key = string.IsNullOrWhiteSpace(key) ? $"{category}:{tick}:{i}" : key.Trim();
                text = string.IsNullOrWhiteSpace(text) ? category.ToString() : text.Trim();
                events.Add(new ReplayTimelineEvent(tick, category, key, text, iconKind, iconId, playerHead));
            }
        }

        if (store)
        {
            timelineEvents =
            [
                .. events
                    .OrderBy(e => e.Tick)
                    .ThenBy(e => e.Category)
                    .ThenBy(e => e.Key)
            ];
        }

        return true;
    }

    private static bool TryReadPlayerHeadSnapshot(BinaryReader reader, out ReplayPlayerHeadSnapshot snapshot)
    {
        snapshot = default;

        if (reader.BaseStream.Position + PlayerHeadSnapshotByteLength > reader.BaseStream.Length)
            return false;

        snapshot = new ReplayPlayerHeadSnapshot(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32());

        return true;
    }

    private static bool TryReadBoundedString(BinaryReader reader, out string value)
    {
        return TryReadBoundedString(reader, out value, MaxMetadataStringByteLength);
    }

    private static bool TryReadBoundedString(BinaryReader reader, out string value, int maxByteLength)
    {
        value = null;

        if (!TryRead7BitEncodedInt(reader, out int byteLength))
            return false;

        Stream stream = reader.BaseStream;
        if (byteLength < 0 ||
            byteLength > maxByteLength ||
            byteLength > stream.Length - stream.Position)
            return false;

        byte[] bytes = reader.ReadBytes(byteLength);
        if (bytes.Length != byteLength)
            return false;

        value = Encoding.UTF8.GetString(bytes);
        return true;
    }

    private static bool TryRead7BitEncodedInt(BinaryReader reader, out int value)
    {
        value = 0;
        int shift = 0;

        for (int i = 0; i < 5; i++)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
                return false;

            byte b = reader.ReadByte();
            value |= (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return value >= 0;

            shift += 7;
        }

        value = 0;
        return false;
    }

    private static int LastIndexOf(byte[] buffer, byte[] marker, int startIndex)
    {
        if (buffer == null || marker == null || marker.Length == 0 || buffer.Length < marker.Length)
            return -1;

        int start = Math.Min(startIndex, buffer.Length - marker.Length);

        for (int i = start; i >= 0; i--)
        {
            bool matches = true;

            for (int j = 0; j < marker.Length; j++)
            {
                if (buffer[i + j] == marker[j])
                    continue;

                matches = false;
                break;
            }

            if (matches)
                return i;
        }

        return -1;
    }

    private static bool LooksLikeMetadataAfterTerminator(byte[] buffer, int metadataMarkerIndex)
    {
        if (metadataMarkerIndex < sizeof(int))
            return false;

        return BitConverter.ToInt32(buffer, metadataMarkerIndex - sizeof(int)) == 0;
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
