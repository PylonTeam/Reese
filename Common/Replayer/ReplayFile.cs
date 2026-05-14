using System;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

namespace Reese.Common.Replayer;

// FIXME: Some of the bullshit we do would be better buffered instead of manually counting bytes, in both directions.

public class ReplayFile : IDisposable
{
    public const string Identifier = "Reese";
    private static readonly byte[] IdentifierASCII = Encoding.ASCII.GetBytes(Identifier);

    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;

    public uint Tick { get; private set; }

    // FIXME: We should just buffer this.
    public int NumberOfPacketDataBytesRemaining { get; private set; }
    public bool ReachedTerminator { get; private set; }

    private ReplayFile()
    {
    }

    private void WriteIdentifier()
    {
        _binaryWriter.Write(IdentifierASCII);
    }

    private static readonly byte[] MetadataMarkerASCII = Encoding.ASCII.GetBytes("RMD1");

    private void ReadPacketDataHeader()
    {
        if (ReachedTerminator)
            return;

        // FIXME: This seems like a shitty way to handle EOF? idek
        try
        {
            Tick += _binaryReader.ReadUInt32();
            NumberOfPacketDataBytesRemaining = _binaryReader.ReadInt32();

            if (NumberOfPacketDataBytesRemaining == 0)
                ReachedTerminator = true;
        }
        catch (EndOfStreamException)
        {
            NumberOfPacketDataBytesRemaining = 0;
            ReachedTerminator = true;
        }
    }

    public int ReadPacketData(Span<byte> data)
    {
        var numberOfBytesRead = _binaryReader.Read(data[..Math.Min(data.Length, NumberOfPacketDataBytesRemaining)]);
        NumberOfPacketDataBytesRemaining = Math.Max(0, NumberOfPacketDataBytesRemaining - numberOfBytesRead);

        if (NumberOfPacketDataBytesRemaining == 0)
            ReadPacketDataHeader();

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

    public void FlushTick()
    {
        if (NumberOfPacketDataBytesRemaining == 0)
            return;

        _binaryWriter.Seek(-NumberOfPacketDataBytesRemaining - sizeof(int), SeekOrigin.Current);
        _binaryWriter.Write(NumberOfPacketDataBytesRemaining);
        _binaryWriter.Seek(0, SeekOrigin.End);

        NumberOfPacketDataBytesRemaining = 0;
    }

    public void Finish(uint finalTick, string worldName, string[] modNames)
    {
        if (_binaryWriter == null)
            return;

        if (finalTick < Tick)
            finalTick = Tick;

        FlushTick();

        _binaryWriter.Write(finalTick - Tick); // delta to recording end
        _binaryWriter.Write(0);                // terminator

        WriteMetadata(worldName, modNames);
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
        ReadPacketDataHeader(); // Prime the first header so ReadPacketData has data ready to go
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

                if (length < 0)
                    return false;

                totalTicks += delta;
                if (totalTicks > uint.MaxValue)
                    return false;

                if (length == 0)
                {
                    foundTerminator = true;
                    break;
                }

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
        durationTicks = 0;
        worldName = null;
        modNames = null;

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

                if (length < 0)
                    return false;

                totalTicks += delta;
                if (totalTicks > uint.MaxValue)
                    return false;

                if (length == 0)
                {
                    durationTicks = chunkCount > 0 ? (uint)totalTicks : 0;
                    ReadMetadataTrailer(reader, out worldName, out modNames);
                    return chunkCount > 0;
                }

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
            return false;
        }
    }

    private static bool ReadMetadataTrailer(BinaryReader reader, out string worldName, out string[] modNames)
    {
        worldName = null;
        modNames = null;

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

        return true;
    }

    #endregion
}
