using Reese.Content;
using System.IO;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;

namespace Reese;

/// <summary>
/// Keeps track of recorder state and syncs it from server to clients.
/// Recorder is server-side, so clients only know this through mod packets.
/// </summary>
internal static class RecorderStatus
{
    private const int SyncRateTicks = 60;

    private static uint nextSyncTick;

    public static bool HasStatus { get; private set; }
    public static bool IsRecording { get; private set; }
    public static string ReplayName { get; private set; } = "";
    public static uint Tick { get; private set; }
    public static long TotalPacketsSent { get; private set; }
    public static long TotalBytesSent { get; private set; }
    public static uint LastPacketTick { get; private set; }
    public static int LastPacketBytes { get; private set; }
    public static int LastPacketMessageId { get; private set; } = -1;

    public static void Start(string replayPath)
    {
        HasStatus = true;
        IsRecording = true;
        ReplayName = string.IsNullOrWhiteSpace(replayPath) ? "" : Path.GetFileNameWithoutExtension(replayPath);
        Tick = 0;
        TotalPacketsSent = 0;
        TotalBytesSent = 0;
        LastPacketTick = 0;
        LastPacketBytes = 0;
        LastPacketMessageId = -1;
        nextSyncTick = 0;
    }

    public static void Stop(uint finalTick)
    {
        HasStatus = true;
        IsRecording = false;
        Tick = finalTick;
    }

    public static void UpdateTick(uint tick)
    {
        Tick = tick;
    }

    public static void TrackPacket(byte[] data, int offset, int size, uint tick)
    {
        TotalPacketsSent++;
        TotalBytesSent += size;
        LastPacketTick = tick;
        LastPacketBytes = size;
        LastPacketMessageId = size >= 3 ? data[offset + 2] : -1;
    }

    public static void SyncToClients(bool force = false)
    {
        if (!HasStatus)
            return;

        if (Main.netMode != NetmodeID.Server)
            return;

        if (!force && Tick < nextSyncTick)
            return;

        nextSyncTick = Tick + SyncRateTicks;

        ModPacket packet = ModContent.GetInstance<Reese>().GetPacket();
        packet.Write((byte)ReesePacketType.RecorderStatus);
        packet.Write(HasStatus);
        packet.Write(IsRecording);
        packet.Write(ReplayName ?? "");
        packet.Write(Tick);
        packet.Write(TotalPacketsSent);
        packet.Write(TotalBytesSent);
        packet.Write(LastPacketTick);
        packet.Write(LastPacketBytes);
        packet.Write(LastPacketMessageId);
        packet.Send(-1, ReplayPlayback.RecordClientIndex);
    }

    public static void Receive(BinaryReader reader)
    {
        HasStatus = reader.ReadBoolean();
        IsRecording = reader.ReadBoolean();
        ReplayName = reader.ReadString();
        Tick = reader.ReadUInt32();
        TotalPacketsSent = reader.ReadInt64();
        TotalBytesSent = reader.ReadInt64();
        LastPacketTick = reader.ReadUInt32();
        LastPacketBytes = reader.ReadInt32();
        LastPacketMessageId = reader.ReadInt32();
    }
}

internal enum ReesePacketType : byte
{
    RecorderStatus
}

[Autoload(Side = ModSide.Server)]
internal sealed class RecorderJoinMessagePlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        if (!ModContent.GetInstance<Recorder>().IsRecording)
            return;

        string smallCameraItemTag = $"[i:{ModContent.ItemType<SmallCameraItem>()}]";
        string bigCameraItemTag = $"[i:{ModContent.ItemType<CameraItem>()}]";

        ChatHelper.SendChatMessageToClient(
            NetworkText.FromLiteral($"{bigCameraItemTag} This world is being recorded with Reese!"),
            new Color(90, 255, 100),
            Player.whoAmI
        );
    }
}