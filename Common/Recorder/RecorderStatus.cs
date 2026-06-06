using Reese.Common.Replayer;
using Reese.Content;
using Reese.Core.Localization;
using Reese.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;

namespace Reese.Common.Recorder;

/// <summary>
/// Keeps track of recorder state and syncs it from server to clients.
/// Recorder is server-side, so clients only know this through mod packets.
/// </summary>
internal static class RecorderStatus
{
    private const int SyncRateTicks = 60;
    private static uint _nextSyncTick;

    public static bool HasStatus { get; private set; }
    public static bool IsRecording { get; private set; }
    public static string ReplayName { get; private set; } = string.Empty;
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
        ReplayName = string.IsNullOrWhiteSpace(replayPath) ? string.Empty : Path.GetFileNameWithoutExtension(replayPath);
        Tick = 0;
        TotalPacketsSent = 0;
        TotalBytesSent = 0;
        LastPacketTick = 0;
        LastPacketBytes = 0;
        LastPacketMessageId = -1;
        _nextSyncTick = 0;
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

    public static string LastPacketModName { get; private set; } = "None";

    public static void TrackPacket(byte[] data, int offset, int size, uint tick)
    {
        TotalPacketsSent++;
        TotalBytesSent += size;
        LastPacketTick = tick;
        LastPacketBytes = size;

        if (size >= 3)
        {
            LastPacketMessageId = data[offset + 2];

            if (LastPacketMessageId == 250 && size >= 6)
            {
                short modNetId = BitConverter.ToInt16(data, offset + 3);
                Mod callingMod = ModLoader.Mods.FirstOrDefault(m => m.NetID == modNetId);
                LastPacketModName = callingMod?.Name ?? $"Unknown ({modNetId})";
            }
            else
            {
                LastPacketModName = "N/A";
            }
        }
    }

    public static void SyncToClients(bool force = false)
    {
        if (!HasStatus || Main.netMode != NetmodeID.Server)
            return;

        if (!force && Tick < _nextSyncTick)
            return;

        _nextSyncTick = Tick + SyncRateTicks;

        ModPacket packet = ModContent.GetInstance<Reese>().GetPacket();
        packet.Write((byte)ReesePacketType.RecorderStatus);
        packet.Write(HasStatus);
        packet.Write(IsRecording);
        packet.Write(ReplayName ?? string.Empty);
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

    public static void SendStartMessage(int playerId)
    {
        string bigCameraItemTag = $"[i:{ModContent.ItemType<CameraItem>()}]";

        ChatHelper.SendChatMessageToClient(
            NetworkText.FromLiteral($"{bigCameraItemTag} This world is being recorded with Reese!"),
            new Color(90, 255, 100),
            playerId
        );
    }

    /// <summary>
    /// Generates a formatted string of the current recording status.
    /// </summary>
    public static string GetStatusString()
    {
        if (!HasStatus)
            return Loc.Get("Recorder.Status.NotInitialized");

        if (!IsRecording)
            return Loc.Get("Recorder.Status.Inactive", ReplayName, Tick);

        string timeString = TimeSpan.FromSeconds(Tick / 60.0).ToString(@"hh\:mm\:ss");
        double sizeKb = TotalBytesSent / 1024.0;

        return Loc.Get("Recorder.Status.Active", ReplayName, timeString, TotalPacketsSent, sizeKb.ToString("F0"));
    }

    /// <summary>
    /// Outputs the current status to the caller (Console or Server Chat).
    /// </summary>
    public static void PrintStatus(CommandCaller caller)
    {
        caller.Reply(GetStatusString());
    }
}

public static class MessageIDCache
{
    private static readonly Dictionary<int, string> _messageNames = [];

    static MessageIDCache()
    {
        // Use reflection to grab all the constant ints in MessageID
        var fields = typeof(MessageID).GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields.Where(f => f.FieldType == typeof(int) || f.FieldType == typeof(byte)))
        {
            int id = Convert.ToInt32(field.GetValue(null));
            _messageNames[id] = field.Name;
        }
    }

    public static string GetName(int id) => _messageNames.TryGetValue(id, out string name) ? name : $"Unknown ({id})";
}


[Autoload(Side = ModSide.Server)]
internal sealed class RecorderJoinMessagePlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        Log.Info($"Player entered world: {Player.whoAmI}");

        if (Main.netMode != NetmodeID.Server)
            return;

        if (!ModContent.GetInstance<Recorder>().IsRecording)
            return;

        RecorderStatus.SendStartMessage(Player.whoAmI);
    }
}
