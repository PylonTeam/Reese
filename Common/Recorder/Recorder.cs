using MonoMod.Cil;
using Reese.Common.Replayer;
using Reese.Core.Configs;
using Reese.Core.Stats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese.Common.Recorder;

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
    private const uint BaselineIntervalTicks = 1800; // Every 30 seconds at 60 TPS. Adjust as needed for performance vs. seek speed tradeoff.

    // FIXME: Become delegate
    public uint Ticks { get; private set; }
    public bool IsRecording => isRecording;
    private bool isRecording;
    private string currentReplayPath;
    private uint nextBaselineTick;

    // Reflection fields
    private static MethodInfo _modNetSyncMods;
    private static MethodInfo _modNetSendNetIds;
    private static MethodInfo _netMessageSyncOnePlayer;
    private static MethodInfo _netMessageSendNPCHousesAndTravelShop;
    //private static MethodInfo _netPlayKickClient;

    public override void Load()
    {
        _modNetSyncMods = typeof(ModNet).GetMethod("SyncMods", BindingFlags.NonPublic | BindingFlags.Static);
        _modNetSendNetIds = typeof(ModNet).GetMethod("SendNetIDs", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSyncOnePlayer =
            typeof(NetMessage).GetMethod("SyncOnePlayer", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSendNPCHousesAndTravelShop = typeof(NetMessage).GetMethod("SendNPCHousesAndTravelShop",
            BindingFlags.NonPublic | BindingFlags.Static);
        //_netPlayKickClient = typeof(Netplay).GetMethod("KickClient", BindingFlags.NonPublic | BindingFlags.Static);

        //On_Netplay.InitializeServer += OnNetplayInitializeServer;

        // Don't "run one update" on the dedicated server when starting, before entering the main loop.
        // I think TML wants to remove this anyway? Or is going to soon?
        // NOTE: Yes, that's correct! This is no longer needed as the code I removed was actually removed from the game.
        // FIXME: Remove this commented code
        // IL_Main.DedServ_PostModLoad += EditMainDedServ_PostModLoad;

        // FIXME: REMOVE THIS BULLSHIT TEST
        // On_Netplay.UpdateConnectedClients += orig =>
        // {
        //     orig();
        //     var trueCount = Netplay.Clients.Count(client => client.IsConnected());
        //     // remove the recording client from consideration i guess???
        //     Netplay.HasClients = trueCount - 1 > 0;
        // };

        // FIXME: This should only be done for the replay client, not ALL clients!
        // Always broadcast DamageNPC regardless of distance to the client's player.
        IL_NetMessage.SendData += EditNetMessageSendData;
    }

    public override void Unload()
    {
        //On_Netplay.InitializeServer -= OnNetplayInitializeServer;
        IL_NetMessage.SendData -= EditNetMessageSendData;
    }

    public void StartRecording()
    {
        StartRecordingInner();
    }
    private void StartRecordingInner()
    {
        if (isRecording)
            return;

        // FIXME: Will this break an existing recording that we try to end? prob need to do it later.
        Ticks = 0;
        nextBaselineTick = BaselineIntervalTicks;
        const int RecordClientIndex = ReplayPlayback.RecordClientIndex;
        const string RecordClientName = "Recording";

        string dir = ReplayPaths.GetFolder();
        Directory.CreateDirectory(dir);
        const string ReplayFilePrefix = "Reese";
        currentReplayPath = Path.Combine(dir, $"{ReplayFilePrefix}_{ReplayPlayback.GetNextReplayNumber(dir, ReplayFilePrefix):0000}.reese");
        RecorderStatus.Start(currentReplayPath);

        Console.WriteLine($"[Reese] Client {RecordClientIndex} started recording to: \"{Path.GetFileName(currentReplayPath)}\"");

        var replayFile = ReplayFile.Write(File.Open(currentReplayPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));

        var recordClient = Netplay.Clients[RecordClientIndex];
        // Not really needed, because we probably just did it above, but why not.
        recordClient.Reset();
        recordClient.Name = RecordClientName;
        // FIXME: File name too long? file path too long? do we care is that our problem??
        recordClient.Socket = new RecordSocket(this, recordClient, replayFile);

        // RemoteClient.Update would set this because Socket.IsConnected() returned true, but we need this now, so
        // fast-track it.
        recordClient.IsActive = true;

        // Client says hello
        // Server sets State to 1 and syncs mods
        recordClient.State = 1;
        _modNetSyncMods.Invoke(null, [recordClient.Id]);
        // Client syncs mods to indicate it's done and ready
        // Server sends net IDs and PlayerInfo
        _modNetSendNetIds.Invoke(null, [recordClient.Id]);
        NetMessage.SendData(MessageID.PlayerInfo, recordClient.Id);
        // Client eventually sends RequestWorldData
        // Server sets State to 2 and sends WorldData and syncs invasion
        recordClient.State = 2;
        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        Main.SyncAnInvasion(recordClient.Id);
        SendReplayWorldSnapshot(recordClient, initial: true, syncInvasion: false);

        // Flush now, so that it comes at update delta 0
        replayFile.FlushTick();
        isRecording = true;
        RecorderStatus.SyncToClients(force: true);

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != RecordClientIndex && Main.player[i]?.active == true)
                RecorderStatus.SendStartMessage(i);
        }
    }

    private void SendReplayWorldSnapshot(RemoteClient recordClient, bool initial, bool syncInvasion = true)
    {
        // Client waits for world clear and state bullshit, eventually sends SpawnTileData.
        // Server sends WorldData (again yes), calculates portal bullshit(???), StatusTextSize (who cares),
        // set State to 3, TileSection for world spawn and maybe player spawn and maybe portal sections, SyncItem and
        // ItemOwner for all active items, SyncNPC for all active NPCs, SyncProjectile for all applicable projectiles,
        // NPCKillCountDeathTally for all NPC types, TileCounts, MoonlordHorror, UpdateTowerShieldStrengths,
        // SyncCavernMonsterType, InitialSpawn.
        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        if (syncInvasion)
            Main.SyncAnInvasion(recordClient.Id);

        if (initial)
        {
            // NOTE: Not sending status text, who cares
            recordClient.State = 3;
        }

        // Let's send ALL tile sections for the ENTIRE world.
        // Likely handles world spawn, player spawn, and portal bullshit.
        int expectedSectionCount = Main.maxSectionsX * Main.maxSectionsY;
        RecordSocket baselineRecordSocket = !initial ? recordClient.Socket as RecordSocket : null;
        int tileSectionsBefore = baselineRecordSocket?.GetBaselineMessageCount(MessageID.TileSection) ?? 0;

        if (!initial)
        {
            int clearedSectionArrays = ClearFakeClientSentSections(recordClient);
            Log.Info($"Recording baseline tile sections for fake client {recordClient.Id}: expected {expectedSectionCount} sections; cleared {clearedSectionArrays} sent-section arrays.");
        }

        for (var x = 0; x < Main.maxSectionsX; x++)
        {
            for (var y = 0; y < Main.maxSectionsY; y++)
                NetMessage.SendSection(recordClient.Id, x, y);
        }

        if (!initial && baselineRecordSocket != null)
        {
            int tileSectionsAfter = baselineRecordSocket.GetBaselineMessageCount(MessageID.TileSection);
            Log.Info($"Baseline tile section loop emitted {tileSectionsAfter - tileSectionsBefore}/{expectedSectionCount} TileSection packets.");
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

        // NOTE: Applicable projectiles is a subset of all projectiles, but in our mod, that subset is ALWAYS equal to
        // all projectiles anyhow.
        for (var i = 0; i < 1000; i++)
        {
            if (Main.projectile[i].active)
                NetMessage.SendData(MessageID.SyncProjectile, recordClient.Id, number: i);
        }

        for (var i = 0; i < NPCLoader.NPCCount; i++)
            NetMessage.SendData(MessageID.NPCKillCountDeathTally, recordClient.Id, number: i);

        NetMessage.SendData(MessageID.TileCounts, recordClient.Id);

        if (initial)
            NetMessage.SendData(MessageID.MoonlordHorror);
        else
            NetMessage.SendData(MessageID.MoonlordHorror, recordClient.Id);

        NetMessage.SendData(MessageID.UpdateTowerShieldStrengths, recordClient.Id);
        NetMessage.SendData(MessageID.SyncCavernMonsterType, recordClient.Id);

        if (initial)
        {
            NetMessage.SendData(MessageID.InitialSpawn, recordClient.Id);
            Main.BestiaryTracker.OnPlayerJoining(recordClient.Id);
            CreativePowerManager.Instance.SyncThingsToJoiningPlayer(recordClient.Id);
            Main.PylonSystem.OnPlayerJoining(recordClient.Id);

            // Client sends a PlayerSpawn from Player.Spawn. Server calls Player.Spawn, sets State to 10, enables
            // broadcast, NetMessage.SyncConnectedPlayer, maybe SetCountsAsHostForGameplay, AnglerQuest,
            // FinishedConnectingToServer, NetMessage.greetPlayer(whoAmI).
            // NOTE: We really don't want a player for this client, so we start to deviate here.
            recordClient.State = 10;
            NetMessage.buffer[recordClient.Id].broadcast = true;
        }

        SendReplayPlayerSnapshot(recordClient, initial);
    }

    private static int ClearFakeClientSentSections(RemoteClient recordClient)
    {
        int cleared = 0;
        FieldInfo[] fields = typeof(RemoteClient).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (!field.FieldType.IsArray ||
                field.FieldType.GetElementType() != typeof(bool) ||
                !field.Name.Contains("section", StringComparison.OrdinalIgnoreCase))
                continue;

            if (field.GetValue(recordClient) is not Array sections)
                continue;

            Array.Clear(sections, 0, sections.Length);
            cleared++;
        }

        return cleared;
    }

    private void SendReplayPlayerSnapshot(RemoteClient recordClient, bool initial)
    {
        // Can't call NetMessage.SyncConnectedPlayer because we don't want to sync ourselves to everyone else by
        // calling SyncOnePlayer with the record client (we aren't a player!).
        for (var i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i].active)
                _netMessageSyncOnePlayer.Invoke(null, [i, recordClient.Id, -1]);
        }

        _netMessageSendNPCHousesAndTravelShop.Invoke(null, [recordClient.Id]);
        NetMessage.SendAnglerQuest(recordClient.Id);
        CreditsRollEvent.SendCreditsRollRemainingTimeToPlayer(recordClient.Id);
        NPC.RevengeManager.SendAllMarkersToPlayer(recordClient.Id);

        NetMessage.SendData(MessageID.AnglerQuest, recordClient.Id,
            text: NetworkText.FromLiteral(Main.player[recordClient.Id].name), number: Main.anglerQuest);

        if (initial)
            NetMessage.SendData(MessageID.FinishedConnectingToServer, recordClient.Id);
    }

    public void StopRecording(string reason = "")
    {
        StopRecordingInner(reason);
    }
    private void StopRecordingInner(string reason="")
    {
        if (!isRecording)
            return;

        isRecording = false;

        RecorderStatus.Stop(Ticks);
        RecorderStatus.SyncToClients(force: true);

        const int RecordClientIndex = ReplayPlayback.RecordClientIndex;
        string savedReplayPath = currentReplayPath;

        var recordClient = Netplay.Clients[RecordClientIndex];

        if (recordClient?.Socket is RecordSocket recordSocket)
        {
            recordSocket.Finish(ReplayStats.GetCurrentWorldName(), ReplayStats.GetCurrentModNames(), Ticks, ReplayFileFlags.New);
            recordSocket.Close();
        }

        if (recordClient != null)
        {
            NetMessage.buffer[RecordClientIndex].broadcast = false;
            recordClient.Reset();
        }

        ReplayPlayback.NotifyFolderChanged();

        currentReplayPath = null;

        if (!string.IsNullOrWhiteSpace(savedReplayPath))
        {
            string message = $"[Reese] Recording stopped at tick {Ticks}. Reason: {reason}. Saved to {Path.GetFileNameWithoutExtension(savedReplayPath)}";
            Log.Info(message);
            Console.WriteLine(message);
        }
    }

    //private void OnNetplayInitializeServer(On_Netplay.orig_InitializeServer orig)
    //{
    //    orig();
    //}

    // FIXME: This is a shitty edit I think?
    private void EditNetMessageSendData(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first store to local 115...
        // (determines whether this player should receive the packet -- this is its initialization)
        cursor.GotoNext(i => i.MatchStloc(115));
        // ...and go back one instruction, to the load of the initial value...
        cursor.Index -= 1;
        // ...to remove it...
        cursor.Remove();
        // ...and replace it with 1/true.
        cursor.EmitLdcI4(1);
    }

    public override void PostUpdateEverything()
    {
        // Automatically start and stop recording when there are players in the world
        if (Main.netMode == NetmodeID.Server)
        {
            bool hasPlayers = ReplayPlayback.HasActivePlayers();
            bool autoStartRecording = ModContent.GetInstance<ClientConfig>()?.AutoStartRecordingOnEnterWorld ?? true;
            if (!isRecording && hasPlayers && autoStartRecording)
                StartRecordingInner();
            else if (isRecording && !hasPlayers)
                StopRecordingInner("No players in server");
        }

        if (isRecording)
        {
            // Advance tick!
            Ticks++;
            RecorderStatus.UpdateTick(Ticks);
            RecorderStatus.SyncToClients();

            if (BaselineIntervalTicks > 0 && Ticks >= nextBaselineTick)
                WriteBaselineCheckpoint();

            // Logging at 1 tick, 5 seconds, 10 seconds, and every 30 minutes thereafter
            if (Ticks == 1 || Ticks == 300 || Ticks == 600 || Ticks % (30 * 60 * 60) == 0)
            {
                string timeString = TimeSpan.FromSeconds(Ticks / 60.0).ToString(@"hh\:mm\:ss");
                string fileName = $"{Path.GetFileNameWithoutExtension(currentReplayPath)}";
                string message = $"[Reese] Reese is currently recording! Filename: {fileName} | Length: {timeString} | Packets: {RecorderStatus.TotalPacketsSent} | Size: {RecorderStatus.TotalBytesSent / 1024.0:F0} KB";

                if (Ticks==600)
                {
                    message += "\n[Reese] The recording has passed 10 seconds! Future logs will now be sent once every 30 minutes. Use /recordstatus to view current recording status info (length, packets, size).";
                }
                Console.WriteLine(message);
                Log.Info(message);
            }
        }
    }

    private void WriteBaselineCheckpoint()
    {
        nextBaselineTick = Ticks + BaselineIntervalTicks;

        if (!isRecording)
            return;

        const int RecordClientIndex = ReplayPlayback.RecordClientIndex;
        RemoteClient recordClient = Netplay.Clients[RecordClientIndex];

        if (recordClient?.Socket is not RecordSocket recordSocket)
            return;

        uint baselineTick = Ticks;
        bool completed = false;
        int byteSize = 0;

        Log.Info($"Recording replay baseline at tick {baselineTick}...");
        recordSocket.BeginBaselineCapture();

        try
        {
            SendReplayWorldSnapshot(recordClient, initial: false);
            completed = true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to record replay baseline at tick {baselineTick}: {e}");
        }
        finally
        {
            if (completed)
                byteSize = recordSocket.EndBaselineCapture(baselineTick);
            else
                recordSocket.CancelBaselineCapture();
        }

        if (completed)
            Log.Info($"Recorded replay baseline at tick {baselineTick}: {byteSize} bytes, {recordSocket.BaselineCount} total baselines.");
    }

    public override void OnWorldUnload()
    {
        if (Main.dedServ)
        {
            StopRecordingInner("Server shutting down due to world unload");

            // Guess our shit isn't closed when the server dies. Would be nice to do it to everyone, but that's a big
            // change from status-quo, so just do it for ourselves.
            foreach (var remoteClient in Netplay.Clients)
            {
                if (remoteClient.Socket is RecordSocket recordSocket)
                    recordSocket.Close();
            }
        }
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
        private bool isFinished;
        private bool isClosed;
        private MemoryStream baselineCaptureStream;
        private BaselinePacketDiagnostics baselinePacketDiagnostics;
        public int BaselineCount => replayFile.Baselines.Count;

        public void BeginBaselineCapture()
        {
            if (isFinished || isClosed)
                throw new InvalidOperationException("Cannot capture a baseline after recording has finished.");

            if (baselineCaptureStream != null)
                throw new InvalidOperationException("Baseline capture is already active.");

            replayFile.FlushTick();
            baselineCaptureStream = new MemoryStream();
            baselinePacketDiagnostics = new BaselinePacketDiagnostics();
        }

        public int EndBaselineCapture(uint tick)
        {
            if (baselineCaptureStream == null)
                return 0;

            byte[] bytes = baselineCaptureStream.ToArray();
            baselineCaptureStream.Dispose();
            baselineCaptureStream = null;

            replayFile.WriteBaselineData(bytes, tick);
            LogBaselineDiagnostics(tick, bytes.Length, baselinePacketDiagnostics);
            baselinePacketDiagnostics = null;
            return bytes.Length;
        }

        public void CancelBaselineCapture()
        {
            baselineCaptureStream?.Dispose();
            baselineCaptureStream = null;
            baselinePacketDiagnostics = null;
        }

        public int GetBaselineMessageCount(int messageId)
        {
            return baselinePacketDiagnostics?.GetCount(messageId) ?? 0;
        }

        public void Finish(string worldName, string[] modNames, uint finalTick, ReplayFileFlags flags = ReplayFileFlags.None)
        {
            if (isFinished || isClosed)
                return;

            isFinished = true;

            // Stop any further traffic to this fake client immediately.
            NetMessage.buffer[remoteClient.Id].broadcast = false;
            remoteClient.IsActive = false;
            remoteClient.State = 0;
            CancelBaselineCapture();

            replayFile.Finish(finalTick, worldName, modNames, flags);
        }

        public void Close()
        {
            if (isClosed)
                return;

            isClosed = true;

            NetMessage.buffer[remoteClient.Id].broadcast = false;
            remoteClient.IsActive = false;
            remoteClient.State = 0;
            CancelBaselineCapture();

            Log.Info("Closing record socket");
            replayFile.Dispose();
        }

        public bool IsConnected() => !isClosed && !isFinished;

        public void Connect(RemoteAddress address) =>
            throw new InvalidOperationException("The recording socket cannot connect");

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
        {
            if (isFinished || isClosed)
            {
                callback?.Invoke(state);
                return;
            }

            if (baselineCaptureStream != null)
            {
                baselineCaptureStream.Write(data, offset, size);
                baselinePacketDiagnostics?.Add(data, offset, size);
            }
            else
            {
                RecorderStatus.TrackPacket(data, offset, size, ticker.Ticks);
                replayFile.WritePacketData(data[offset..(offset + size)], ticker.Ticks);
            }

            callback?.Invoke(state);
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null) =>
            throw new InvalidOperationException("The recording socket cannot receive");

        public bool IsDataAvailable() => false;

        public void SendQueuedPackets()
        {
            if (!isFinished && !isClosed)
                remoteClient.TimeOutTimer = 0;
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The recording socket cannot listen");

        public void StopListening() =>
            throw new InvalidOperationException("The recording socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;

        private static void LogBaselineDiagnostics(uint tick, int byteSize, BaselinePacketDiagnostics diagnostics)
        {
            if (diagnostics == null)
            {
                Log.Info($"Recorded replay baseline at tick {tick}: {byteSize} bytes, no packet diagnostics available.");
                return;
            }

            Log.Info($"Recorded replay baseline at tick {tick}: {byteSize} bytes, {diagnostics.TotalPackets} packets, trailing={diagnostics.TrailingBytes}, malformed={diagnostics.MalformedBytes}.");
            Log.Info("Baseline packet counts: " + string.Join(", ", GetBaselineSpecialCounts(diagnostics)));
            Log.Info("Baseline top packet counts: " + diagnostics.FormatTopCounts(12));
        }

        private static IEnumerable<string> GetBaselineSpecialCounts(BaselinePacketDiagnostics diagnostics)
        {
            yield return Count(MessageID.WorldData, "WorldData");
            yield return Count(MessageID.TileSection, "TileSection");
            yield return Count(MessageID.TileManipulation, "TileManipulation");
            yield return Count(MessageID.TileSquare, "TileSquare");
            yield return Count(MessageID.SyncNPC, "SyncNPC");
            yield return Count(MessageID.SyncProjectile, "SyncProjectile");
            yield return Count(MessageID.SyncItem, "SyncItem");
            yield return Count(MessageID.PlayerInfo, "PlayerInfo");
            yield return Count(MessageID.SyncPlayer, "SyncPlayer");
            yield return Count(MessageID.PlayerControls, "PlayerControls");
            yield return Count(MessageID.PlayerLifeMana, "PlayerLifeMana");
            yield return Count(MessageID.PlayerMana, "PlayerMana");
            yield return Count(MessageID.PlayerBuffs, "PlayerBuffs");
            yield return Count(MessageID.PlayerTeam, "PlayerTeam");

            string Count(int messageId, string name) => $"{name}={diagnostics.GetCount(messageId)}";
        }

        private sealed class BaselinePacketDiagnostics
        {
            private readonly Dictionary<int, int> messageCounts = [];
            private readonly List<byte> pendingBytes = [];

            public int TotalPackets { get; private set; }
            public int MalformedBytes { get; private set; }
            public int TrailingBytes => pendingBytes.Count;

            public void Add(byte[] data, int offset, int size)
            {
                for (int i = 0; i < size; i++)
                    pendingBytes.Add(data[offset + i]);

                ParsePendingPackets();
            }

            public int GetCount(int messageId)
            {
                return messageCounts.TryGetValue(messageId, out int count) ? count : 0;
            }

            public string FormatTopCounts(int maxCount)
            {
                if (messageCounts.Count == 0)
                    return "none";

                return string.Join(", ",
                    messageCounts
                        .OrderByDescending(pair => pair.Value)
                        .ThenBy(pair => pair.Key)
                        .Take(maxCount)
                        .Select(pair => $"{MessageIDCache.GetName(pair.Key)}={pair.Value}"));
            }

            private void ParsePendingPackets()
            {
                while (pendingBytes.Count >= 3)
                {
                    int packetLength = pendingBytes[0] | (pendingBytes[1] << 8);
                    if (packetLength < 3)
                    {
                        MalformedBytes++;
                        pendingBytes.RemoveAt(0);
                        continue;
                    }

                    if (pendingBytes.Count < packetLength)
                        return;

                    int messageId = pendingBytes[2];
                    messageCounts.TryGetValue(messageId, out int count);
                    messageCounts[messageId] = count + 1;
                    TotalPackets++;
                    pendingBytes.RemoveRange(0, packetLength);
                }
            }
        }
    }

}
