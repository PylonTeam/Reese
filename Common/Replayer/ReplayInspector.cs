using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Reese.Common.Replayer;

public static class ReplayInspector
{
    private const string Identifier = ReplayFile.Identifier;
    private static readonly byte[] IdentifierBytes = Encoding.ASCII.GetBytes(Identifier);
    private const byte Version2Marker = (byte)'2';

    public static ReplayInspectionReport InspectMetadata(string path)
    {
        var report = new ReplayInspectionReport
        {
            Path = path,
            FileBytes = new FileInfo(path).Length
        };

        using var stream = ReplayFile.OpenReadShared(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);

        byte[] identifier = reader.ReadBytes(Identifier.Length);
        if (!identifier.SequenceEqual(IdentifierBytes))
        {
            report.Error = "Missing Reese identifier";
            return report;
        }

        int marker = stream.ReadByte();
        if (marker != Version2Marker)
        {
            report.FormatVersion = 1;
            report.IsValid = true;
            return report;
        }

        report.FormatVersion = 2;

        int metadataByteCount = reader.ReadInt32();
        if (metadataByteCount < 0 || metadataByteCount > 1024 * 1024)
        {
            report.Error = $"Invalid metadata length: {metadataByteCount}";
            return report;
        }

        byte[] metadataBytes = reader.ReadBytes(metadataByteCount);
        string metadataJson = Encoding.UTF8.GetString(metadataBytes).TrimEnd('\0', ' ', '\r', '\n', '\t');

        if (!string.IsNullOrWhiteSpace(metadataJson))
            report.Metadata = JsonSerializer.Deserialize<ReplayMetadata>(metadataJson);

        report.DurationTicks = report.Metadata?.DurationTicks ?? 0;
        report.IsValid = report.Error == null;
        return report;
    }

    public static ReplayInspectionReport Inspect(string path)
    {
        var report = new ReplayInspectionReport
        {
            Path = path,
            FileBytes = new FileInfo(path).Length
        };

        using var stream = ReplayFile.OpenReadShared(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);

        byte[] identifier = reader.ReadBytes(Identifier.Length);
        if (!identifier.SequenceEqual(IdentifierBytes))
        {
            report.Error = "Missing Reese identifier";
            return report;
        }

        int marker = stream.ReadByte();
        if (marker == Version2Marker)
        {
            report.FormatVersion = 2;
            int metadataByteCount = reader.ReadInt32();
            if (metadataByteCount < 0 || metadataByteCount > 1024 * 1024)
            {
                report.Error = $"Invalid metadata length: {metadataByteCount}";
                return report;
            }

            byte[] metadataBytes = reader.ReadBytes(metadataByteCount);
            string metadataJson = Encoding.UTF8.GetString(metadataBytes).TrimEnd('\0', ' ', '\r', '\n', '\t');
            if (!string.IsNullOrWhiteSpace(metadataJson))
                report.Metadata = JsonSerializer.Deserialize<ReplayMetadata>(metadataJson);
        }
        else
        {
            report.FormatVersion = 1;
            if (marker >= 0)
                stream.Seek(-1, SeekOrigin.Current);
        }

        var packetBuffer = new List<byte>();

        while (stream.Position < stream.Length)
        {
            if (stream.Length - stream.Position < 8)
            {
                report.TruncatedBlockCount++;
                report.Error = "Trailing bytes are too short for a block header";
                break;
            }

            uint tickDelta = reader.ReadUInt32();
            int length = reader.ReadInt32();
            if (length < 0)
            {
                report.MalformedBlockCount++;
                report.Error = $"Negative block length at byte {stream.Position - 4}: {length}";
                break;
            }

            if (length == 0)
            {
                report.HasCleanEndMarker = true;
                break;
            }

            long payloadStart = stream.Position;
            long payloadEnd = payloadStart + length;
            if (payloadEnd > stream.Length)
            {
                report.TruncatedBlockCount++;
                report.Error = $"Block at byte {payloadStart} overruns file length";
                break;
            }

            report.BlockCount++;
            report.PacketDataBytes += length;
            report.DurationTicks += tickDelta;
            report.MaxBlockBytes = Math.Max(report.MaxBlockBytes, length);

            if (tickDelta == 0)
                report.ZeroDeltaBlockCount++;

            if (report.BlockCount == 1)
                report.BaselineBytes = length;

            byte[] payload = reader.ReadBytes(length);
            AddPacketStats(report, payload, packetBuffer);

            if (stream.Position != payloadEnd)
                stream.Position = payloadEnd;
        }

        report.TrailingPacketBytes = packetBuffer.Count;
        report.IsValid = report.Error == null &&
            report.MalformedBlockCount == 0 &&
            report.TruncatedBlockCount == 0 &&
            report.MalformedPacketDataCount == 0 &&
            report.TrailingPacketBytes == 0;
        return report;
    }

    public static ReplayInspectionReport InspectLatestReplay()
    {
        string dir = ReplayPaths.GetFolder();
        Directory.CreateDirectory(dir);

        string path = Directory.GetFiles(dir, "*.reese", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return path == null
            ? new ReplayInspectionReport { Error = $"No .reese files found in {dir}" }
            : Inspect(path);
    }

    private static void AddPacketStats(ReplayInspectionReport report, ReadOnlySpan<byte> payload, List<byte> packetBuffer)
    {
        packetBuffer.AddRange(payload.ToArray());

        while (packetBuffer.Count >= 3)
        {
            int packetLength = packetBuffer[0] | (packetBuffer[1] << 8);
            if (packetLength < 3)
            {
                report.MalformedPacketDataCount++;
                packetBuffer.RemoveAt(0);
                continue;
            }

            if (packetBuffer.Count < packetLength)
                break;

            int messageId = packetBuffer[2];
            report.MessageCounts.TryGetValue(messageId, out int existing);
            report.MessageCounts[messageId] = existing + 1;
            report.PacketCount++;
            packetBuffer.RemoveRange(0, packetLength);
        }
    }

}

public sealed class ReplayInspectionReport
{
    public string Path { get; set; }
    public long FileBytes { get; set; }
    public int FormatVersion { get; set; }
    public ReplayMetadata Metadata { get; set; }
    public bool IsValid { get; set; }
    public string Error { get; set; }
    public int BlockCount { get; set; }
    public int PacketCount { get; set; }
    public long PacketDataBytes { get; set; }
    public uint DurationTicks { get; set; }
    public int ZeroDeltaBlockCount { get; set; }
    public int BaselineBytes { get; set; }
    public int MaxBlockBytes { get; set; }
    public int MalformedPacketDataCount { get; set; }
    public int TrailingPacketBytes { get; set; }
    public int MalformedBlockCount { get; set; }
    public int TruncatedBlockCount { get; set; }
    public bool HasCleanEndMarker { get; set; }
    public Dictionary<int, int> MessageCounts { get; } = new();

    public string ToLogString()
    {
        string fileName = string.IsNullOrWhiteSpace(Path) ? "<none>" : System.IO.Path.GetFileName(Path);
        string topMessages = MessageCounts.Count == 0
            ? "none"
            : string.Join(", ", MessageCounts.OrderByDescending(x => x.Value).Take(8).Select(x => $"{x.Key}:{x.Value}"));

        string player = string.IsNullOrWhiteSpace(Metadata?.PlayerName) ? "?" : Metadata.PlayerName;
        string world = string.IsNullOrWhiteSpace(Metadata?.WorldName) ? "?" : Metadata.WorldName;

        return $"Replay report {fileName}: valid={IsValid}, format=v{FormatVersion}, player={player}, world={world}, bytes={FileBytes}, " +
            $"blocks={BlockCount}, packets={PacketCount}, packetBytes={PacketDataBytes}, ticks={DurationTicks}, " +
            $"seconds={DurationTicks / 60d:0.##}, baselineBytes={BaselineBytes}, maxBlockBytes={MaxBlockBytes}, " +
            $"zeroDeltaBlocks={ZeroDeltaBlockCount}, malformedPackets={MalformedPacketDataCount}, " +
            $"trailingPacketBytes={TrailingPacketBytes}, malformedBlocks={MalformedBlockCount}, " +
            $"truncatedBlocks={TruncatedBlockCount}, eof={HasCleanEndMarker}, topMessages={topMessages}, " +
            $"error={Error ?? "none"}";
    }
}

public sealed class InspectReplayCommand : ModCommand
{
    public override string Command => "inspect";
    public override string Description => "Inspect a current Reese replay file and log packet/tick diagnostics.";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        try
        {
            string path = ResolvePath(args);
            ReplayInspectionReport report = path == null ? ReplayInspector.InspectLatestReplay() : ReplayInspector.Inspect(path);
            string message = report.ToLogString();

            if (ReplaySession.IsRecording)
                message = GetRecordingStatusLine() + Environment.NewLine + message;

            Log.Info(message);
            caller.Reply(message);
        }
        catch (Exception e)
        {
            Log.Warn("Replay inspect failed: " + e);
            caller.Reply("Replay inspect failed: " + e.Message);
        }
    }

    private static string GetRecordingStatusLine()
    {
        string path = ReplaySession.CurrentPath;
        string fileName = string.IsNullOrWhiteSpace(path) ? "<none>" : Path.GetFileName(path);
        long fileBytes = !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? new FileInfo(path).Length : 0;
        return $"Recording status: isRecording={ReplaySession.IsRecording}, playerFile={fileName}, currentBytes={fileBytes}";
    }

    private static string ResolvePath(string[] args)
    {
        if (args.Length == 0)
            return null;

        string raw = string.Join(" ", args).Trim('"');
        if (Path.IsPathRooted(raw))
            return raw;

        string replayFolderPath = Path.Combine(ReplayPaths.GetFolder(), raw);
        if (File.Exists(replayFolderPath))
            return replayFolderPath;

        if (!raw.EndsWith(".reese", StringComparison.OrdinalIgnoreCase))
        {
            string replayFilePath = Path.Combine(ReplayPaths.GetFolder(), raw + ".reese");
            if (File.Exists(replayFilePath))
                return replayFilePath;
        }

        return raw;
    }
}
