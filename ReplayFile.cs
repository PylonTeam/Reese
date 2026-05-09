using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Reese;

public sealed class ReplayFile : IDisposable
{
    public const string Identifier = "Reese";
    private static readonly byte[] IdentifierASCII = Encoding.ASCII.GetBytes(Identifier);
    private const byte Version2Marker = (byte)'2';
    private const int CurrentFormatVersion = 2;
    private const int MetadataByteCount = 32768;

    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;
    private long _metadataOffset = -1;
    private bool _disposed;
    private bool _wroteEndMarker;

    public uint Tick { get; private set; }
    public bool EndOfFile { get; private set; }
    public bool IsWriting => _binaryWriter != null;
    public ReplayMetadata Metadata { get; private set; }

    public int NumberOfPacketDataBytesRemaining { get; private set; }

    private ReplayFile()
    {
    }

    public static FileStream OpenReadShared(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    public static FileStream OpenWriteShared(string path)
    {
        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
    }

    public void WritePacketData(byte[] data, uint tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        Metadata.PacketDataBytes += data.Length;

        var packetStats = ReplayPacketStats.FromPacketData(data);
        Metadata.PacketCount += packetStats.PacketCount;
        Metadata.MalformedPacketDataCount += packetStats.MalformedPacketDataCount;
    }

    private void ReadPacketDataHeader()
    {
        if (EndOfFile)
            return;

        try
        {
            if (_binaryReader.BaseStream.CanSeek && _binaryReader.BaseStream.Position >= _binaryReader.BaseStream.Length)
            {
                EndOfFile = true;
                Tick = Metadata.DurationTicks;
                NumberOfPacketDataBytesRemaining = 0;
                return;
            }

            Tick += _binaryReader.ReadUInt32();
            NumberOfPacketDataBytesRemaining = _binaryReader.ReadInt32();

            if (NumberOfPacketDataBytesRemaining < 0)
                throw new InvalidDataException("Replay block length cannot be negative");

            if (NumberOfPacketDataBytesRemaining == 0)
            {
                EndOfFile = true;
                Tick = Math.Max(Tick, Metadata.DurationTicks);
            }
        }
        catch (EndOfStreamException)
        {
            EndOfFile = true;
            Tick = Metadata.DurationTicks;
            NumberOfPacketDataBytesRemaining = 0;
        }
    }

    public int ReadPacketData(Span<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (EndOfFile)
            return 0;

        var numberOfBytesRead = _binaryReader.Read(data[..Math.Min(data.Length, NumberOfPacketDataBytesRemaining)]);
        NumberOfPacketDataBytesRemaining = Math.Max(0, NumberOfPacketDataBytesRemaining - numberOfBytesRead);

        if (numberOfBytesRead == 0 && NumberOfPacketDataBytesRemaining > 0)
            throw new EndOfStreamException("Replay ended in the middle of a packet block");

        if (NumberOfPacketDataBytesRemaining == 0)
            ReadPacketDataHeader();

        return numberOfBytesRead;
    }

    public void FlushTick()
    {
        FlushTick(Tick);
    }

    public void FlushToDisk()
    {
        _binaryWriter?.Flush();
    }

    private void FlushTick(uint tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (NumberOfPacketDataBytesRemaining > 0)
        {
            _binaryWriter.Seek((-NumberOfPacketDataBytesRemaining) - 8, SeekOrigin.Current);
            _binaryWriter.Write(tick - Tick);
            _binaryWriter.Write(NumberOfPacketDataBytesRemaining);
            _binaryWriter.Seek(0, SeekOrigin.End);

            if (Metadata.BlockCount == 0)
                Metadata.BaselineBytes = NumberOfPacketDataBytesRemaining;

            Metadata.BlockCount++;
            Metadata.DurationTicks = tick;
            Tick = tick;
            NumberOfPacketDataBytesRemaining = 0;
        }
    }

    public static ReplayFile Write(Stream stream, ReplayMetadata metadata = null)
    {
        var replayFile = new ReplayFile
        {
            _binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true),
            Metadata = metadata ?? new ReplayMetadata()
        };

        replayFile.Metadata.FormatVersion = CurrentFormatVersion;
        replayFile.Metadata.CreatedUtc ??= DateTime.UtcNow.ToString("O");
        replayFile.WriteHeader();

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

        replayFile.ReadHeader();
        replayFile.ReadPacketDataHeader();

        return replayFile;
    }

    private void WriteHeader()
    {
        _binaryWriter.Write(IdentifierASCII);
        _binaryWriter.Write(Version2Marker);
        _binaryWriter.Write(MetadataByteCount);
        _metadataOffset = _binaryWriter.BaseStream.Position;
        _binaryWriter.Write(new byte[MetadataByteCount]);
        WriteMetadataHeader();
    }

    private void ReadHeader()
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
                string metadataJson = Encoding.UTF8.GetString(metadataBytes).TrimEnd('\0', ' ', '\r', '\n', '\t');
                Metadata = string.IsNullOrWhiteSpace(metadataJson)
                    ? ReplayMetadata.Legacy()
                    : JsonSerializer.Deserialize<ReplayMetadata>(metadataJson) ?? ReplayMetadata.Legacy();
                Metadata.FormatVersion = Math.Max(Metadata.FormatVersion, CurrentFormatVersion);
                return;
            }
            catch (Exception e) when (e is EndOfStreamException or InvalidDataException or JsonException)
            {
                _binaryReader.BaseStream.Seek(packetStart, SeekOrigin.Begin);
                Metadata = ReplayMetadata.Legacy();
                return;
            }
        }

        if (marker >= 0)
            _binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);

        Metadata = ReplayMetadata.Legacy();
    }

    private void WriteMetadataHeader()
    {
        if (_metadataOffset < 0)
            return;

        long restorePosition = _binaryWriter.BaseStream.Position;
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(Metadata, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        if (jsonBytes.Length > MetadataByteCount)
            throw new InvalidDataException($"Replay metadata is too large ({jsonBytes.Length} bytes)");

        _binaryWriter.BaseStream.Seek(_metadataOffset, SeekOrigin.Begin);
        _binaryWriter.Write(jsonBytes);
        _binaryWriter.Write(new byte[MetadataByteCount - jsonBytes.Length]);
        _binaryWriter.BaseStream.Seek(restorePosition, SeekOrigin.Begin);
    }

    private void WriteEndMarker()
    {
        if (_wroteEndMarker)
            return;

        _binaryWriter.Write(0u);
        _binaryWriter.Write(0);
        _wroteEndMarker = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_binaryWriter != null)
        {
            FlushTick();
            Metadata.Finalized = true;
            Metadata.DurationTicks = Tick;
            Metadata.EndReason ??= "Closed";
            WriteEndMarker();
            WriteMetadataHeader();
        }

        Stream baseStream = _binaryReader?.BaseStream ?? _binaryWriter?.BaseStream;

        _binaryWriter?.Dispose();
        _binaryReader?.Dispose();
        baseStream?.Dispose();

        _disposed = true;
    }
}

public sealed class ReplayMetadata
{
    public int FormatVersion { get; set; } = 2;
    public string CreatedUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string PlayerName { get; set; } = string.Empty;
    public string WorldName { get; set; } = string.Empty;
    public int WorldId { get; set; }
    public string ModVersion { get; set; } = string.Empty;
    public string TmlVersion { get; set; } = string.Empty;
    public int TickRate { get; set; } = 60;
    public uint DurationTicks { get; set; }
    public int BlockCount { get; set; }
    public int PacketCount { get; set; }
    public long PacketDataBytes { get; set; }
    public int BaselineBytes { get; set; }
    public int MalformedPacketDataCount { get; set; }
    public bool Finalized { get; set; }
    public string EndReason { get; set; }

    public static ReplayMetadata Legacy() => new()
    {
        FormatVersion = 1,
        CreatedUtc = string.Empty,
        EndReason = "Legacy/no explicit EOF"
    };
}

public readonly struct ReplayPacketStats
{
    public int PacketCount { get; init; }
    public int MalformedPacketDataCount { get; init; }
    public IReadOnlyDictionary<int, int> MessageCounts { get; init; }

    public static ReplayPacketStats FromPacketData(ReadOnlySpan<byte> data)
    {
        var counts = new Dictionary<int, int>();
        int packetCount = 0;
        int malformed = 0;
        int offset = 0;

        while (offset < data.Length)
        {
            if (offset + 3 > data.Length)
                break;

            int packetLength = data[offset] | (data[offset + 1] << 8);
            if (packetLength < 3)
            {
                malformed++;
                break;
            }

            if (offset + packetLength > data.Length)
                break;

            int messageId = data[offset + 2];
            counts.TryGetValue(messageId, out int count);
            counts[messageId] = count + 1;
            packetCount++;
            offset += packetLength;
        }

        return new ReplayPacketStats
        {
            PacketCount = packetCount,
            MalformedPacketDataCount = malformed,
            MessageCounts = counts
        };
    }
}
