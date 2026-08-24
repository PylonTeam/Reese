using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reese.Common;

public class ReplayFile : IDisposable
{
    public const string Identifier = "Reese";
    private static readonly byte[] IdentifierAscii = Encoding.ASCII.GetBytes(Identifier);
    public const byte Version = 0;

    private object _binaryRw;
    private BinaryReader Reader => _binaryRw as BinaryReader;
    private BinaryWriter Writer => _binaryRw as BinaryWriter;

    // socket bytes go here
    private MemoryStream _dataBuffer;

    // used while reading
    private BlockHeader _bh;
    private MetaBlockHeader[] _mbHeaders;
    private long _foot;

    // used while writing
    private long _head;

    public MetaBlockFooterInfo MetaInfo;
    private MetaBlockFooterBaselines _metaBaselines;
    private MetaBlockFooterTimeline _metaTimeline;

    public long Tick { get; private set; }
    public bool IsBaselining => _bh.Flags.HasFlag(BlockFlag.Baseline);
    public bool IsDataBuffered => _dataBuffer != null && _dataBuffer.Position != _dataBuffer.Length;
    public bool Terminated { get; private set; }

    public enum MetaBlockIdentity : byte
    {
        Info,
        Baselines,
        Timeline
    }

    public record struct MetaBlockHeader(MetaBlockIdentity Identity, int Length)
    {
        public static MetaBlockHeader Read(BinaryReader br) =>
            new((MetaBlockIdentity)br.ReadByte(), br.ReadInt32());

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)Identity);
            bw.Write(Length);
        }
    }

    public record struct MetaBlockFooterInfo(
        byte WhoAmI,
        string WorldName,
        DateTime Start,
        long Duration)
    {
        public void Read(BinaryReader br)
        {
            WhoAmI = br.ReadByte();
            WorldName = br.ReadString();
            Start = DateTime.FromBinary(br.ReadInt64());
            Duration = br.Read7BitEncodedInt64();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(WhoAmI);
            bw.Write(WorldName);
            bw.Write(Start.ToBinary());
            bw.Write7BitEncodedInt64(Duration);
        }
    }

    public record struct MetaBlockFooterBaselines(List<long> Positions)
    {
        public void Read(BinaryReader br)
        {
            var count = br.Read7BitEncodedInt();
            Positions = new List<long>(new long[count]);

            for (var i = 0; i < count; i++)
                Positions[i] = br.ReadInt64();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write7BitEncodedInt(Positions.Count);
            foreach (var p in Positions)
                bw.Write(p);
        }
    }

    public record struct MetaBlockFooterTimeline;

    [Flags]
    public enum BlockFlag : byte
    {
        None = 0,
        Baseline = 1 << 0,
        Terminate = 1 << 1
    }

    public record struct BlockHeader(BlockFlag Flags, int Length, int Delta)
    {
        public void Read(BinaryReader br)
        {
            Flags = (BlockFlag)br.ReadByte();
            Length = br.Read7BitEncodedInt();

            if (Flags.HasFlag(BlockFlag.Terminate))
                Delta = 0;
            else
                Delta = br.Read7BitEncodedInt();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)Flags);
            bw.Write7BitEncodedInt(Length);
            if (!Flags.HasFlag(BlockFlag.Terminate))
                bw.Write7BitEncodedInt(Delta);
        }
    }

    public static ReplayFile Read(Stream stream, bool footerNow)
    {
        var br = new BinaryReader(stream, Encoding.UTF8);
        if (!br.ReadBytes(IdentifierAscii.Length).SequenceEqual(IdentifierAscii))
            throw new InvalidDataException("not a Reese replay");

        var ver = br.ReadByte();
        if (ver > Version)
            throw new InvalidDataException($"Reese replay version too high: has {ver}, but we only know {Version}");

        var replay = new ReplayFile { _binaryRw = br };
        replay.ReadHeader();

        if (footerNow)
        {
            var origin = stream.Position;
            try
            {
                stream.Seek(replay._foot, SeekOrigin.Begin);
                replay.ReadFooter();
            }
            finally
            {
                stream.Seek(origin, SeekOrigin.Begin);
            }
        }

        replay.ReadBlock();

        return replay;
    }

    public static ReplayFile Write(Stream stream)
    {
        var bw = new BinaryWriter(stream, Encoding.UTF8);
        bw.Write(IdentifierAscii);
        bw.Write(Version);

        var replay = new ReplayFile
        {
            _binaryRw = bw,
            _head = stream.Position
        };
        // FIXME: dumb
        replay._metaBaselines.Positions = new List<long>();
        replay.MetaInfo = new(0x69, "lol world", new DateTime(), 0xDEADDEAD);
        replay.WriteHeader([
                new(MetaBlockIdentity.Info, 0),
                new(MetaBlockIdentity.Baselines, 0),
                new(MetaBlockIdentity.Timeline, 0)
            ], 0
        );

        return replay;
    }

    private void ReadHeader()
    {
        var metaBlockCount = Reader.Read7BitEncodedInt();

        _mbHeaders = new MetaBlockHeader[metaBlockCount];
        var identities = new HashSet<MetaBlockIdentity>();

        for (var i = 0; i < metaBlockCount; i++)
        {
            _mbHeaders[i] = MetaBlockHeader.Read(Reader);
            var ident = _mbHeaders[i].Identity;
            if (!identities.Add(ident))
                throw new InvalidDataException($"duplicate meta block {ident.ToString()}");
        }

        _foot = Reader.ReadInt64();
    }

    private void ReadFooter()
    {
        foreach (var hdr in _mbHeaders)
        {
            if (hdr.Length == 0)
                continue;

            var data = Reader.ReadBytes(hdr.Length);
            if (data.Length != hdr.Length)
                throw new EndOfStreamException("incomplete meta block footer");

            using var stream = new MemoryStream(data);
            switch (hdr.Identity)
            {
                case MetaBlockIdentity.Info:
                    MetaInfo.Read(new BinaryReader(stream));
                    break;
                case MetaBlockIdentity.Baselines:
                    _metaBaselines.Read(new BinaryReader(stream));
                    break;
                case MetaBlockIdentity.Timeline:
                    throw new NotImplementedException();
                default:
                    throw new InvalidDataException("unknown meta block identity");
            }
        }
    }

    private void WriteFooter()
    {
        var mbHeaders = new MetaBlockHeader[]
        {
            new(MetaBlockIdentity.Info, 0),
            new(MetaBlockIdentity.Baselines, 0),
            new(MetaBlockIdentity.Timeline, 0)
        };

        var foot = Writer.BaseStream.Position;
        {
            using var stream = new MemoryStream();
            MetaInfo.Write(new BinaryWriter(stream));
            mbHeaders[0].Length = (int)stream.Length;
            stream.WriteTo(Writer.BaseStream);
            Log.Debug($"replay write info meta footer length={(int)stream.Length}");
        }

        {
            using var stream = new MemoryStream();
            _metaBaselines.Write(new BinaryWriter(stream));
            mbHeaders[1].Length = (int)stream.Length;
            stream.WriteTo(Writer.BaseStream);
            Log.Debug($"replay write baselines meta footer length={(int)stream.Length}");
        }

        {
            // using var stream = new MemoryStream();
            // _metaTimeline.Write(new BinaryWriter(stream));
            // mbHeaders[2].Length = (int)stream.Length;
            // stream.WriteTo(_bw.BaseStream);
        }

        Writer.BaseStream.Seek(_head, SeekOrigin.Begin);
        WriteHeader(mbHeaders, foot);
        Writer.Seek(0, SeekOrigin.End);
        Log.Debug("finished all of footer writing");
    }

    private void WriteBufferedBlock(BlockFlag flags, int delta)
    {
        if (_dataBuffer == null)
            throw new InvalidOperationException("cannot write block without buffered data");

        Log.Debug($"writing block from buffered: length={(int)_dataBuffer.Length}, delta={delta}");

        new BlockHeader(flags, (int)_dataBuffer.Length, delta).Write(Writer);
        _dataBuffer.WriteTo(Writer.BaseStream);
        _dataBuffer.Dispose();
        _dataBuffer = null;
    }

    private void ReadBlock()
    {
        if (Terminated)
            throw new InvalidOperationException("cannot read block from terminated replay");

        Log.Debug($"reading another block (ticking {Tick})");
        if (_dataBuffer != null)
            throw new InvalidOperationException("cannot read another block with buffered data pending");

        Tick += _bh.Delta;

        _bh.Read(Reader);
        if (_bh.Length > 0)
        {
            var data = Reader.ReadBytes(_bh.Length);
            if (data.Length != _bh.Length)
                throw new EndOfStreamException("incomplete block");

            _dataBuffer = new MemoryStream(data);
        }

        if (_bh.Flags.HasFlag(BlockFlag.Terminate))
        {
            Terminated = true;
            ReadFooter();
        }
    }

    private void WriteHeader(MetaBlockHeader[] mbHeaders, long foot)
    {
        Writer.Write7BitEncodedInt(mbHeaders.Length);
        foreach (var hdr in mbHeaders)
            hdr.Write(Writer);

        Writer.Write(foot);

        Log.Debug($"wrote {mbHeaders.Length} meta block headers for footer @ {foot}");
    }

    public int ReadData(Span<byte> data)
    {
        var count = _dataBuffer.Read(data);
        if (_dataBuffer.Position == _dataBuffer.Length)
        {
            if (Terminated)
            {
                _dataBuffer.Dispose();
                _dataBuffer = null;
                Tick += _bh.Delta;
                _bh = default;
            }
            else
            {
                _dataBuffer.Dispose();
                _dataBuffer = null;
                ReadBlock();
            }
        }

        return count;
    }

    public void WriteData(byte[] data, long tick)
    {
        if (Terminated)
            throw new InvalidOperationException("cannot write past termination");

        if (Tick > tick)
            throw new InvalidOperationException("cannot tick backwards");

        if (data.Length == 0)
            throw new InvalidOperationException("refusing to write zero-length data");

        if (Tick < tick)
        {
            Flush((int)checked(tick - Tick));
            Tick = tick;
            _dataBuffer = new();
        }
        else
        {
            _dataBuffer ??= new();
        }

        _dataBuffer.Write(data);
    }

    public void WriteBaseline(byte[] data, long tick)
    {
        if (Terminated)
            throw new InvalidOperationException("cannot write past termination");

        if (Tick > tick)
            throw new InvalidOperationException("cannot tick backwards");

        Flush((int)checked(tick - Tick));
        var p = Writer.BaseStream.Position;
        Log.Debug($"writing block from baseline: length={data.Length}, tick {Tick} to {tick}. add baseline index {p}");
        Tick = tick;

        new BlockHeader(BlockFlag.Baseline, data.Length, 0).Write(Writer);
        Writer.Write(data);

        _metaBaselines.Positions.Add(p);
    }

    private void Flush(int delta, bool terminal = false)
    {
        if (Terminated)
            throw new InvalidOperationException("cannot flush to terminated replay");

        if (_dataBuffer != null)
            WriteBufferedBlock(terminal ? BlockFlag.Terminate : BlockFlag.None, delta);
        else if (terminal)
            new BlockHeader(BlockFlag.Terminate, 0, 0).Write(Writer);

        Log.Debug("flush complete!");

        if (!Terminated && terminal)
        {
            Terminated = true;
            MetaInfo.Duration = Tick;
            Log.Debug("replay terminated");
            WriteFooter();
        }
    }

    public void Dispose()
    {
        if (Writer != null)
        {
            if (!Terminated)
                Flush(0, true);

            Writer.Dispose();
            _binaryRw = null;
        }
        else if (Reader != null)
        {
            Reader.Dispose();
            _binaryRw = null;
        }
    }
}