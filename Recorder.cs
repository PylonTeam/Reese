using Reese.Core.Debug;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese;

// ImHex
// #include <std/mem>
//
// struct Packet
// {
//     u32 updates_since;
//     u32 length;
//     padding[length];
// };
//
// Packet packets[while($ < std::mem::size())] @ 0x0;

[Autoload(Side = ModSide.Server)]
public class Recorder : ModSystem, ITicker
{
    public uint Ticks { get; private set; }

    private static string _lastReplayPath; // temporary path to store latest replay, useful for quick testing and debugging rn.
    private const uint ForcedPlayerSyncIntervalTicks = 1;
    private const uint ForcedEntitySyncIntervalTicks = 1;
    private const uint ForcedItemSyncIntervalTicks = 30;

    // Reflection for internal tML methods
    private static MethodInfo _modNetSyncMods;
    private static MethodInfo _modNetSendNetIds;
    private static MethodInfo _netMessageSyncOnePlayer;
    private static MethodInfo _netMessageSendNPCHousesAndTravelShop;
    private static MethodInfo _netPlayKickClient;

    public override void Load()
    {
        // Initialize reflection for internal tML methods
        _modNetSyncMods = typeof(ModNet).GetMethod("SyncMods", BindingFlags.NonPublic | BindingFlags.Static);
        _modNetSendNetIds = typeof(ModNet).GetMethod("SendNetIDs", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSyncOnePlayer =
            typeof(NetMessage).GetMethod("SyncOnePlayer", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSendNPCHousesAndTravelShop = typeof(NetMessage).GetMethod("SendNPCHousesAndTravelShop",
            BindingFlags.NonPublic | BindingFlags.Static);
        _netPlayKickClient = typeof(Netplay).GetMethod("KickClient", BindingFlags.NonPublic | BindingFlags.Static);

        // Hook into server initialization and client updates to manage recording state
        On_Netplay.InitializeServer += OnNetplayInitializeServer;
        On_Netplay.UpdateConnectedClients += OnNetplayUpdateConnectedClients;
    }

    public override void Unload()
    {
        On_Netplay.InitializeServer -= OnNetplayInitializeServer;
        On_Netplay.UpdateConnectedClients -= OnNetplayUpdateConnectedClients;
    }

    public void StartMultiplayerRecording(string playerName)
    {
        StartRecording(playerName);
    }

    private void StartRecording(string playerName)
    {
        if (ReplaySession.IsReplayPlayback)
            return;

        if (ReplaySession.IsRecording)
            StopRecording();

        Ticks = 0;
        const string RecordClientName = "Recording";
        string safeWorldName = SanitizeFilePart(Main.worldName);
        string safePlayerName = SanitizeFilePart(playerName);

        var dir = ReeseReplayPaths.GetFolder();
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{safeWorldName}_{safePlayerName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.reese");
        _lastReplayPath = filePath;
        ReplaySession.BeginRecording(filePath);

        var metadata = new ReplayMetadata
        {
            PlayerName = playerName ?? string.Empty,
            WorldName = Main.worldName ?? string.Empty,
            WorldId = Main.worldID,
            ModVersion = Mod?.Version?.ToString() ?? string.Empty,
            TmlVersion = typeof(ModLoader).Assembly.GetName().Version?.ToString() ?? string.Empty,
            TickRate = 60,
        };
        var replayFile = ReplayFile.Write(ReplayFile.OpenWriteShared(filePath), metadata);

        var recordClient = GetOrCreateRecordClient();
        recordClient.Reset();
        recordClient.Name = RecordClientName;
        recordClient.Socket = new RecordSocket(this, recordClient, replayFile);
        Main.player[recordClient.Id].active = false;
        Main.player[recordClient.Id].name = RecordClientName;

        // RemoteClient.Update would set this because Socket.IsConnected() returned true, but we need this now, so
        // fast-track it.
        recordClient.IsActive = true;

        using (NetModeScope.ForPacketSynthesis())
            WriteBaseline(recordClient, RecordClientName);

        // Flush now, so that it comes at update delta 0
        replayFile.FlushTick();
        replayFile.FlushToDisk();
        Log.Info("Replay baseline flushed: " + ReplayInspector.Inspect(filePath).ToLogString());
    }

    private void OnNetplayInitializeServer(On_Netplay.orig_InitializeServer orig)
    {
        orig();
    }

    private void OnNetplayUpdateConnectedClients(On_Netplay.orig_UpdateConnectedClients orig)
    {
        orig();

        if (!ReplaySession.IsRecording)
            return;

        Netplay.HasClients = Netplay.Clients.Any(client => client != null && client.IsConnected() && !ReplaySession.IsRecordClient(client));
    }

    public override void PostUpdateEverything()
    {
        if (!ReplaySession.IsRecording && Main.netMode == NetmodeID.Server)
        {
            var firstPlayer = GetFirstRealActivePlayer();
            if (firstPlayer != null)
                StartMultiplayerRecording(firstPlayer.name);

            return;
        }

        if (!ReplaySession.IsRecording)
            return;

        if (Main.netMode == NetmodeID.Server && !HasRealMultiplayerPlayers())
        {
            StopRecording();
            return;
        }

        Ticks++;
        if (Ticks % 60 == 0)
            Log.Info("Server tick: " + Ticks);

        using (NetModeScope.ForPacketSynthesis())
            ForceSyncReplayClient();
    }

    public override void OnWorldUnload()
    {
        StopRecording();
    }

    public void StopRecording()
    {
        if (!ReplaySession.IsRecording)
            return;

        if (ReplaySession.IsRecording || Main.dedServ)
        {
            foreach (var remoteClient in Netplay.Clients)
            {
                if (remoteClient?.Socket is RecordSocket recordSocket)
                    recordSocket.Close();
            }
        }

        try
        {
            var recordBinPath = ReeseReplayPaths.GetFile();

            if (!string.IsNullOrWhiteSpace(_lastReplayPath) && File.Exists(_lastReplayPath))
            {
                File.Copy(_lastReplayPath, recordBinPath, true);
                Log.Info($"Wrote record.bin: {recordBinPath} (source: {Path.GetFileName(_lastReplayPath)})");
                Log.Info(ReplayInspector.Inspect(_lastReplayPath).ToLogString());
            }
            else
            {
                Log.Warn("record.bin not written (no last replay path / file missing)");
            }
        }
        catch (Exception e)
        {
            Log.Warn("Failed to write record.bin: " + e);
        }

        ReplaySession.End("recording stopped");
    }

    private static void WriteBaseline(RemoteClient recordClient, string recordClientName)
    {
        // Client says hello
        // Server sets State to 1 and syncs mods
        recordClient.State = 1;
        _modNetSyncMods?.Invoke(null, [recordClient.Id]);
        // Client syncs mods to indicate it's done and ready
        // Server sends net IDs and PlayerInfo
        _modNetSendNetIds.Invoke(null, [recordClient.Id]);
        NetMessage.SendData(MessageID.PlayerInfo, recordClient.Id);
        // Client eventually sends RequestWorldData
        // Server sets State to 2 and sends WorldData and syncs invasion
        recordClient.State = 2;
        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        Main.SyncAnInvasion(recordClient.Id);

        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        recordClient.State = 3;

        for (var x = 0; x < Main.maxSectionsX; x++)
        {
            for (var y = 0; y < Main.maxSectionsY; y++)
                NetMessage.SendSection(recordClient.Id, x, y);
        }

        for (var i = 0; i < Main.maxItems; i++)
        {
            var item = Main.item[i];
            if (item.active)
            {
                NetMessage.SendData(MessageID.SyncItem, recordClient.Id, number: i);
                NetMessage.SendData(MessageID.ItemOwner, recordClient.Id, number: i);
            }
        }

        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (npc.active)
                NetMessage.SendData(MessageID.SyncNPC, recordClient.Id, number: i);
        }

        for (var i = 0; i < Main.maxProjectiles; i++)
        {
            if (Main.projectile[i].active)
                NetMessage.SendData(MessageID.SyncProjectile, recordClient.Id, number: i);
        }

        for (var i = 0; i < NPCLoader.NPCCount; i++)
            NetMessage.SendData(MessageID.NPCKillCountDeathTally, recordClient.Id, number: i);

        NetMessage.SendData(MessageID.TileCounts, recordClient.Id);
        NetMessage.SendData(MessageID.MoonlordHorror, recordClient.Id);
        NetMessage.SendData(MessageID.UpdateTowerShieldStrengths, recordClient.Id);
        NetMessage.SendData(MessageID.SyncCavernMonsterType, recordClient.Id);
        NetMessage.SendData(MessageID.InitialSpawn, recordClient.Id);

        Main.BestiaryTracker.OnPlayerJoining(recordClient.Id);
        CreativePowerManager.Instance.SyncThingsToJoiningPlayer(recordClient.Id);
        Main.PylonSystem.OnPlayerJoining(recordClient.Id);

        recordClient.State = 10;
        NetMessage.buffer[recordClient.Id].broadcast = true;

        for (var i = 0; i < Main.maxPlayers; i++)
        {
            if (i != recordClient.Id && Main.player[i]?.active == true)
                SyncPlayerToReplayClient(i, recordClient.Id);
        }

        _netMessageSendNPCHousesAndTravelShop.Invoke(null, [recordClient.Id]);
        NetMessage.SendAnglerQuest(recordClient.Id);
        CreditsRollEvent.SendCreditsRollRemainingTimeToPlayer(recordClient.Id);
        NPC.RevengeManager.SendAllMarkersToPlayer(recordClient.Id);

        NetMessage.SendData(MessageID.AnglerQuest, recordClient.Id, text: NetworkText.FromLiteral(recordClientName), number: Main.anglerQuest);
        NetMessage.SendData(MessageID.FinishedConnectingToServer, recordClient.Id);
    }

    private void ForceSyncReplayClient()
    {
        var recordClient = GetOrCreateRecordClient();
        if (recordClient.Socket is not RecordSocket || recordClient.State < 10)
            return;

        if (Ticks % ForcedPlayerSyncIntervalTicks == 0)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                if (i != recordClient.Id && Main.player[i]?.active == true)
                    SyncPlayerToReplayClient(i, recordClient.Id);
            }
        }

        if (Ticks % ForcedEntitySyncIntervalTicks == 0)
        {
            for (var i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                    NetMessage.SendData(MessageID.SyncNPC, recordClient.Id, number: i);
            }

            for (var i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active)
                    NetMessage.SendData(MessageID.SyncProjectile, recordClient.Id, number: i);
            }
        }

        if (Ticks % ForcedItemSyncIntervalTicks == 0)
        {
            for (var i = 0; i < Main.maxItems; i++)
            {
                if (Main.item[i].active)
                {
                    NetMessage.SendData(MessageID.SyncItem, recordClient.Id, number: i);
                    NetMessage.SendData(MessageID.ItemOwner, recordClient.Id, number: i);
                }
            }
        }
    }

    private static Player GetFirstRealActivePlayer()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != ReplaySession.RecordClientIndex && Main.player[i]?.active == true)
                return Main.player[i];
        }

        return null;
    }

    private static void SyncPlayerToReplayClient(int playerIndex, int toWho, int fromWho = -1)
    {
        var player = Main.player[playerIndex];
        if (player == null)
        {
            Log.Warn($"Skipping replay player sync for missing player slot {playerIndex}");
            NetMessage.SendData(MessageID.PlayerActive, toWho, fromWho, number: playerIndex);
            return;
        }

        float active = player.active ? 1f : 0f;
        NetMessage.SendData(MessageID.PlayerActive, toWho, fromWho, number: playerIndex, number2: active);

        if (!player.active)
            return;

        NetMessage.SendData(MessageID.SyncPlayer, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.PlayerControls, toWho, fromWho, number: playerIndex);

        if (player.statLife <= 0)
            NetMessage.SendData(MessageID.DeadPlayer, toWho, fromWho, number: playerIndex);

        NetMessage.SendData(MessageID.PlayerLifeMana, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.TogglePVP, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.PlayerTeam, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.PlayerMana, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.PlayerBuffs, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.SyncPlayerChestIndex, toWho, fromWho, number: playerIndex, number2: player.chest);
        NetMessage.SendData(MessageID.SyncProjectileTrackers, toWho, fromWho, number: playerIndex);
        NetMessage.SendData(MessageID.SyncLoadout, toWho, fromWho, number: playerIndex, number2: player.CurrentLoadoutIndex);

        SyncPlayerItemArray(playerIndex, toWho, fromWho, player.inventory, PlayerItemSlotID.Inventory0);
        SyncPlayerItemArray(playerIndex, toWho, fromWho, player.armor, PlayerItemSlotID.Armor0);
        SyncPlayerItemArray(playerIndex, toWho, fromWho, player.dye, PlayerItemSlotID.Dye0);
        SyncPlayerItemArray(playerIndex, toWho, fromWho, player.miscEquips, PlayerItemSlotID.Misc0);
        SyncPlayerItemArray(playerIndex, toWho, fromWho, player.miscDyes, PlayerItemSlotID.MiscDye0);

        if (player.Loadouts != null)
        {
            SyncLoadoutItemArray(playerIndex, toWho, fromWho, 0, PlayerItemSlotID.Loadout1_Armor_0, PlayerItemSlotID.Loadout1_Dye_0);
            SyncLoadoutItemArray(playerIndex, toWho, fromWho, 1, PlayerItemSlotID.Loadout2_Armor_0, PlayerItemSlotID.Loadout2_Dye_0);
            SyncLoadoutItemArray(playerIndex, toWho, fromWho, 2, PlayerItemSlotID.Loadout3_Armor_0, PlayerItemSlotID.Loadout3_Dye_0);
        }

        PlayerLoader.SyncPlayer(player, toWho, fromWho, false);
    }

    private static void SyncLoadoutItemArray(int playerIndex, int toWho, int fromWho, int loadoutIndex, int armorStartSlot, int dyeStartSlot)
    {
        var loadouts = Main.player[playerIndex]?.Loadouts;
        if (loadouts == null || loadoutIndex < 0 || loadoutIndex >= loadouts.Length || loadouts[loadoutIndex] == null)
            return;

        SyncPlayerItemArray(playerIndex, toWho, fromWho, loadouts[loadoutIndex].Armor, armorStartSlot);
        SyncPlayerItemArray(playerIndex, toWho, fromWho, loadouts[loadoutIndex].Dye, dyeStartSlot);
    }

    private static void SyncPlayerItemArray(int playerIndex, int toWho, int fromWho, Item[] items, int startSlot)
    {
        if (items == null)
            return;

        for (var i = 0; i < items.Length; i++)
            NetMessage.SendData(MessageID.SyncEquipment, toWho, fromWho, number: playerIndex, number2: startSlot + i, number3: items[i]?.prefix ?? 0);
    }

    private static RemoteClient GetOrCreateRecordClient()
    {
        if (Netplay.Clients == null || Netplay.Clients.Length <= ReplaySession.RecordClientIndex)
            throw new InvalidOperationException("Netplay client slots are not initialized");

        var recordClient = Netplay.Clients[ReplaySession.RecordClientIndex];
        if (recordClient != null)
        {
            EnsureMessageBufferSlot(ReplaySession.RecordClientIndex);
            return recordClient;
        }

        recordClient = new RemoteClient
        {
            Id = ReplaySession.RecordClientIndex
        };
        Netplay.Clients[ReplaySession.RecordClientIndex] = recordClient;
        EnsureMessageBufferSlot(ReplaySession.RecordClientIndex);

        return recordClient;
    }

    private static void EnsureMessageBufferSlot(int whoAmI)
    {
        if (NetMessage.buffer == null || NetMessage.buffer.Length <= whoAmI)
            throw new InvalidOperationException("NetMessage buffer slots are not initialized");

        NetMessage.buffer[whoAmI] ??= new MessageBuffer();
    }

    private static bool HasRealMultiplayerPlayers()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != ReplaySession.RecordClientIndex && Main.player[i].active)
                return true;
        }

        return Netplay.Clients.Any(client => client != null &&
            client.Id != ReplaySession.RecordClientIndex &&
            client.IsConnected() &&
            client.State >= 10);
    }

    private static string SanitizeFilePart(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Length <= 48 ? value : value[..48];
    }

    private class RecordRemoteAddress : RemoteAddress
    {
        public override string GetIdentifier() => "Recording";
        public override string GetFriendlyName() => "Recording";
        public override bool IsLocalHost() => true;

        public override string ToString() => GetFriendlyName();
    }

    private class RecordSocket(ITicker ticker, RemoteClient remoteClient, ReplayFile replayFile) : ISocket
    {
        private readonly RecordRemoteAddress _remoteAddress = new();
        private bool _closed;

        public void Close()
        {
            if (_closed)
                return;

            _closed = true;
            SendQueuedPackets();
            Log.Info("Closing record socket");
            replayFile.Dispose();
            remoteClient.Reset();
        }

        public bool IsConnected() => !_closed;

        public void Connect(RemoteAddress address) =>
            throw new InvalidOperationException("The recording socket cannot connect");

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
        {
            // FIXME: Actually do this async
            replayFile.WritePacketData(data[offset..(offset + size)], ticker.Ticks);

            callback(state);
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback,
            object state = null) => throw new InvalidOperationException("The recording socket cannot receive");

        public bool IsDataAvailable() => false;

        public void SendQueuedPackets()
        {
            remoteClient.TimeOutTimer = 0;
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The recording socket cannot listen");

        public void StopListening() => throw new InvalidOperationException("The recording socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;
    }

    private readonly struct NetModeScope : IDisposable
    {
        private readonly int _previousNetMode;
        private readonly bool _changed;

        private NetModeScope(int previousNetMode, bool changed)
        {
            _previousNetMode = previousNetMode;
            _changed = changed;
        }

        public static NetModeScope ForPacketSynthesis()
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
                return new NetModeScope(Main.netMode, false);

            int previousNetMode = Main.netMode;
            Main.netMode = NetmodeID.Server;
            return new NetModeScope(previousNetMode, true);
        }

        public void Dispose()
        {
            if (_changed)
                Main.netMode = _previousNetMode;
        }
    }
}
