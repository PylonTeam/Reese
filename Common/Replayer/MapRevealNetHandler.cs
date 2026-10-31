using Reese.Common.Spectator;
using Reese.Core.Net;
using System;
using System.Diagnostics;
using System.IO;
using Terraria.ID;
using Terraria.Net.Sockets;

namespace Reese.Common.Replayer;

internal static class MapRevealNetHandler
{
    internal const int SectionsPerBatch = 4;

    private enum MapRevealPacket : byte
    {
        RequestSections,
        Cancel,
        Reject
    }

    internal static void Receive(BinaryReader reader, int sender)
    {
        if (reader.BaseStream.Length - reader.BaseStream.Position < 1)
            return;

        MapRevealPacket type = (MapRevealPacket)reader.ReadByte();

        switch (type)
        {
            case MapRevealPacket.RequestSections:
                if (Main.netMode != NetmodeID.Server || !HasPayload(reader, 40))
                    return;

                Guid operation = new(reader.ReadBytes(16));
                int worldId = reader.ReadInt32();
                Guid worldGuid = new(reader.ReadBytes(16));
                int firstSection = reader.ReadInt32();
                MapRevealServerSystem.QueueSections(sender, operation, worldId, worldGuid, firstSection);
                break;

            case MapRevealPacket.Cancel:
                if (Main.netMode == NetmodeID.Server && HasPayload(reader, 16))
                    MapRevealServerSystem.Cancel(sender, new Guid(reader.ReadBytes(16)));
                break;

            case MapRevealPacket.Reject:
                if (Main.netMode == NetmodeID.MultiplayerClient && HasPayload(reader, 16))
                    MapRevealHelper.Reject(new Guid(reader.ReadBytes(16)));
                break;
        }
    }

    // HandlePacket can receive a shared buffer with data beyond this packet.
    // Only consume the fixed fields; ModNet owns packet framing.
    private static bool HasPayload(BinaryReader reader, int length) =>
        reader.BaseStream.Length - reader.BaseStream.Position >= length;

    internal static void RequestSections(Guid operation, int worldId, Guid worldGuid, int firstSection)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || ReplayPlayback.IsReplayPlayback)
            return;

        ModPacket packet = CreatePacket(MapRevealPacket.RequestSections, operation);
        packet.Write(worldId);
        packet.Write(worldGuid.ToByteArray());
        packet.Write(firstSection);
        packet.Send();
    }

    internal static void Cancel(Guid operation)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && !ReplayPlayback.IsReplayPlayback)
            CreatePacket(MapRevealPacket.Cancel, operation).Send();
    }

    internal static void Reject(int toClient, Guid operation)
    {
        if (Main.netMode == NetmodeID.Server && toClient >= 0 && toClient < Main.maxPlayers &&
            Netplay.Clients[toClient]?.IsActive == true)
            CreatePacket(MapRevealPacket.Reject, operation).Send(toClient);
    }

    private static ModPacket CreatePacket(MapRevealPacket type, Guid operation)
    {
        ModPacket packet = ModContent.GetInstance<Reese>().GetPacket();
        packet.Write((byte)ReesePacketType.MapReveal);
        packet.Write((byte)type);
        packet.Write(operation.ToByteArray());
        return packet;
    }
}

/// <summary>Sends small terrain batches only to currently authorized ghost spectators.</summary>
public sealed class MapRevealServerSystem : ModSystem
{
    private sealed class SectionBatch
    {
        internal Guid Operation;
        internal Player Player;
        internal ISocket Socket;
        internal int WorldId;
        internal Guid WorldGuid;
        internal int Section;
        internal int End;
    }

    private const int ServerSectionsPerTick = 2;
    private static readonly SectionBatch[] batches = new SectionBatch[Main.maxPlayers];
    private static int nextClient;

    internal static void QueueSections(int sender, Guid operation, int worldId, Guid worldGuid, int firstSection)
    {
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers)
            return;

        int sectionCount = Main.maxSectionsX * Main.maxSectionsY;
        if (!CanSendSections(sender) || operation == Guid.Empty || worldId != Main.worldID ||
            worldGuid != Main.ActiveWorldFileData.UniqueId || firstSection < 0 || firstSection >= sectionCount)
        {
            MapRevealNetHandler.Reject(sender, operation);
            return;
        }

        // Repeated requests cannot grow a connection's pending terrain queue.
        SectionBatch previous = batches[sender];
        if (previous != null && ReferenceEquals(previous.Socket, Netplay.Clients[sender].Socket) &&
            ReferenceEquals(previous.Player, Main.player[sender]))
            return;

        batches[sender] = new SectionBatch
        {
            Operation = operation,
            Player = Main.player[sender],
            Socket = Netplay.Clients[sender].Socket,
            WorldId = worldId,
            WorldGuid = worldGuid,
            Section = firstSection,
            End = Math.Min(firstSection + MapRevealNetHandler.SectionsPerBatch, sectionCount)
        };
    }

    internal static void Cancel(int sender, Guid operation)
    {
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers)
            return;

        SectionBatch batch = batches[sender];
        if (batch?.Operation == operation && ReferenceEquals(batch.Socket, Netplay.Clients[sender]?.Socket) &&
            ReferenceEquals(batch.Player, Main.player[sender]))
            batches[sender] = null;
    }

    private static bool CanSendSections(int slot) =>
        Netplay.Clients[slot]?.IsActive == true && Netplay.Clients[slot].State == 10 &&
        Netplay.Clients[slot].Socket != null && SpectatorMode.IsGhostSpectatingEnabled &&
        SpectatorMode.IsSpectatingForWorldLogic(Main.player[slot]);

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Stopwatch budget = Stopwatch.StartNew();
        int sent = 0;

        for (int checkedClients = 0; checkedClients < Main.maxPlayers &&
            sent < ServerSectionsPerTick && budget.ElapsedMilliseconds < 4; checkedClients++)
        {
            int slot = nextClient;
            nextClient = (nextClient + 1) % Main.maxPlayers;
            SectionBatch batch = batches[slot];

            if (batch == null)
                continue;

            if (!CanSendSections(slot) || !ReferenceEquals(batch.Player, Main.player[slot]) ||
                !ReferenceEquals(batch.Socket, Netplay.Clients[slot].Socket) || batch.WorldId != Main.worldID ||
                batch.WorldGuid != Main.ActiveWorldFileData.UniqueId)
            {
                batches[slot] = null;
                MapRevealNetHandler.Reject(slot, batch.Operation);
                continue;
            }

            int sectionX = batch.Section % Main.maxSectionsX;
            int sectionY = batch.Section / Main.maxSectionsX;
            // The client may have discarded terrain the server previously sent.
            // SendSection otherwise skips it based on this connection's sent flag.
            Netplay.Clients[slot].TileSections[sectionX, sectionY] = false;
            NetMessage.SendSection(slot, sectionX, sectionY);
            batch.Section++;
            sent++;

            if (batch.Section == batch.End)
                batches[slot] = null;
        }
    }

    public override void OnWorldLoad() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();

    private static void Reset()
    {
        Array.Clear(batches);
        nextClient = 0;
    }
}
