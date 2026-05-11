using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Reese;

// FIXME: Some of the bullshit we do would be better buffered instead of manually counting bytes, in both directions.

public class ReplayFile : IDisposable
{
    public const string Identifier = "Reese";
    private const byte Version2Marker = (byte)'2';
    private static readonly byte[] IdentifierASCII = Encoding.ASCII.GetBytes(Identifier);

    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;

    public uint Tick { get; private set; }

    /// <summary>Populated for v2 replays after the header is read.</summary>
    public ReplayMetadata Metadata { get; private set; } = new ReplayMetadata();

    // FIXME: We should just buffer this.
    public int NumberOfPacketDataBytesRemaining { get; private set; }

    private ReplayFile()
    {
    }

    private void WriteIdentifier()
    {
        _binaryWriter.Write(IdentifierASCII);
    }

    // FIXME: Ever heard of async? We have the opportunity upstream.
    public void WritePacketData(byte[] data, uint tick)
    {
        // Can't go backwards
        if (Tick > tick)
            throw new Exception("Cannot write packet data into the past");

        if (Tick < tick)
            FlushTick(tick);

        if (NumberOfPacketDataBytesRemaining == 0)
        {
            _binaryWriter.Write(0);
            _binaryWriter.Write(0);
        }

        _binaryWriter.Write(data);
        NumberOfPacketDataBytesRemaining += data.Length;
    }

    private void ReadPacketDataHeader()
    {
        // FIXME: This seems like a shitty way to handle EOF? idek
        try
        {
            Tick += _binaryReader.ReadUInt32();
            NumberOfPacketDataBytesRemaining = _binaryReader.ReadInt32();
            if (NumberOfPacketDataBytesRemaining < 0)
                throw new InvalidDataException("Replay block length cannot be negative");
        }
        catch (EndOfStreamException)
        {
            Tick = 0;
            NumberOfPacketDataBytesRemaining = 0;
        }
    }

    /// <summary>
    /// V2 replays store JSON metadata after the magic string; legacy files go straight to tick/length blocks.
    /// </summary>
    private void ReadFormatHeaderAfterIdentifier()
    {
        long packetStart = _binaryReader.BaseStream.Position;
        int marker = _binaryReader.BaseStream.ReadByte();
        if (marker == Version2Marker)
        {
            try
            {
                int metadataByteCount = _binaryReader.ReadInt32();
                if (metadataByteCount < 0 || metadataByteCount > 1024 * 1024)
                    throw new InvalidDataException($"Invalid Reese metadata block length: {metadataByteCount}");

                byte[] metadataBytes = _binaryReader.ReadBytes(metadataByteCount);
                Metadata = ReplayMetadata.FromJsonBytes(metadataBytes);
                Metadata.FormatVersion = Math.Max(Metadata.FormatVersion, 2);
                return;
            }
            catch (Exception e) when (e is EndOfStreamException or InvalidDataException)
            {
                _binaryReader.BaseStream.Seek(packetStart, SeekOrigin.Begin);
                Metadata = new ReplayMetadata();
                return;
            }
        }

        if (marker >= 0)
            _binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);

        Metadata = new ReplayMetadata();
    }

    public int ReadPacketData(Span<byte> data)
    {
        var numberOfBytesRead = _binaryReader.Read(data[..Math.Min(data.Length, NumberOfPacketDataBytesRemaining)]);
        NumberOfPacketDataBytesRemaining = Math.Max(0, NumberOfPacketDataBytesRemaining - numberOfBytesRead);

        if (NumberOfPacketDataBytesRemaining == 0)
            ReadPacketDataHeader();

        return numberOfBytesRead;
    }

    public void FlushTick()
    {
        FlushTick(Tick);
    }

    private void FlushTick(uint tick)
    {
        if (NumberOfPacketDataBytesRemaining > 0)
        {
            _binaryWriter.Seek((-NumberOfPacketDataBytesRemaining) - 8, SeekOrigin.Current);
            _binaryWriter.Write(tick - Tick);
            _binaryWriter.Write(NumberOfPacketDataBytesRemaining);
            _binaryWriter.Seek(0, SeekOrigin.End);

            Tick = tick;
            NumberOfPacketDataBytesRemaining = 0;
        }
    }

    public static FileStream OpenReadShared(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    /// <summary>Read embedded v2 metadata for UI (browser list) without loading the full packet stream.</summary>
    public static bool TryReadMetadata(string path, out ReplayMetadata metadata)
    {
        metadata = new ReplayMetadata();

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            using FileStream stream = OpenReadShared(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

            byte[] identifier = reader.ReadBytes(Identifier.Length);
            if (!identifier.SequenceEqual(IdentifierASCII))
                return false;

            long packetStart = stream.Position;
            int marker = stream.ReadByte();
            if (marker == Version2Marker)
            {
                int metadataByteCount = reader.ReadInt32();
                if (metadataByteCount < 0 || metadataByteCount > 1024 * 1024)
                    return false;

                byte[] raw = reader.ReadBytes(metadataByteCount);
                metadata = ReplayMetadata.FromJsonBytes(raw);
                return true;
            }

            if (marker >= 0)
                stream.Seek(-1, SeekOrigin.Current);

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool ResetRead()
    {
        if (_binaryReader == null || !_binaryReader.BaseStream.CanSeek)
            return false;

        _binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
        Tick = 0;
        NumberOfPacketDataBytesRemaining = 0;

        byte[] identifier = _binaryReader.ReadBytes(Identifier.Length);
        if (!identifier.SequenceEqual(IdentifierASCII))
            return false;

        ReadFormatHeaderAfterIdentifier();
        ReadPacketDataHeader();
        return true;
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

        replayFile.ReadFormatHeaderAfterIdentifier();
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
}
