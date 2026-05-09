using Reese.Common.Replayer;
using Reese.Core.Net;
using System.Collections.Generic;
using System.IO;
using Terraria.ID;

namespace Reese.Common.ReplaySpectate.SpectatorMode;

internal static class SpectatorModeNetHandler
{
    private enum SpectatorPacket : byte
    {
        SetMode, // Client asks server to change one player’s mode
        SyncModes // Server sends the complete player mode list
    }

    public static void Receive(BinaryReader reader, int sender)
    {
        SpectatorPacket packet = (SpectatorPacket)reader.ReadByte();

        switch (packet)
        {
            case SpectatorPacket.SetMode:
                RequireBytes(reader, 5, packet);
                ReceiveSetMode(reader, sender);
                break;

            case SpectatorPacket.SyncModes:
                RequireBytes(reader, 4, packet);
                ReceiveSyncModes(reader);
                break;

            default:
                throw new IOException($"Unknown spectator packet: {packet} ({(byte)packet})");
        }
    }

    private static void RequireBytes(BinaryReader reader, int bytes, SpectatorPacket packet)
    {
        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;

        if (remaining < bytes)
            throw new IOException($"Malformed spectator packet {packet}: expected at least {bytes} payload bytes, got {remaining}.");
    }

    public static void SendRequestSetMode(int slot, SpectateMode mode)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = CreatePacket(SpectatorPacket.SetMode);
        packet.Write(slot);
        packet.Write((byte)mode);
        packet.Send();
    }

    public static void SendSyncModes(int toClient = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = CreatePacket(SpectatorPacket.SyncModes);
        packet.Write(SpectatorModeSystem.Modes.Count);

        foreach ((int slot, SpectateMode mode) in SpectatorModeSystem.Modes)
        {
            packet.Write(slot);
            packet.Write((byte)mode);
        }

        packet.Send(toClient);
    }

    private static void ReceiveSetMode(BinaryReader reader, int sender)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        int slot = reader.ReadInt32();
        SpectateMode mode = (SpectateMode)reader.ReadByte();

        if (slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        if (slot != sender)
            return;

        SpectatorModeSystem.SetModeServer(slot, mode);
    }

    private static void ReceiveSyncModes(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (ReplaySession.IsReplayPlayback)
        {
            SpectatorModeSystem.ForceLocalReplaySpectator();
            return;
        }

        int count = reader.ReadInt32();
        HashSet<int> receivedSlots = [];

        for (int i = 0; i < count; i++)
        {
            int slot = reader.ReadInt32();
            SpectateMode mode = (SpectateMode)reader.ReadByte();

            if (slot is < 0 or >= Main.maxPlayers)
                continue;

            if (mode is not SpectateMode.Player and not SpectateMode.Spectator)
                continue;

            receivedSlots.Add(slot);
            SpectatorModeSystem.SetModeLocal(slot, mode);
        }

        List<int> staleSlots = [];

        foreach (int slot in SpectatorModeSystem.Modes.Keys)
            if (!receivedSlots.Contains(slot))
                staleSlots.Add(slot);

        foreach (int slot in staleSlots)
            SpectatorModeSystem.Modes.Remove(slot);
    }

    private static ModPacket CreatePacket(SpectatorPacket packetType)
    {
        ModPacket packet = ModContent.GetInstance<Reese>().GetPacket();
        packet.Write((byte)ReesePacketIdentifier.RequestToggleSpectateMode);
        packet.Write((byte)packetType);
        return packet;
    }   

}
