using MonoMod.Cil;
using Reese.Common.Replay.Events;
using Reese.Common.Replayer;
using Reese.Core.Configs;
using Reese.Core.Stats;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese.Common.Record;

// TODO: auto stop

[Autoload(Side = ModSide.Server)]
public class Recorder : ModSystem, ITicker
{
    private RecordSocket recordSocket;
    public byte WhoAmI { get; private set; }
    public uint Tick { get; private set; }
    public bool IsRecording => recordSocket != null;
    private string currentReplayPath;
    private uint nextBaselineTick;
    private uint baselineIntervalTicks;
    private bool suppressAutoStartAfterMaxLength;
    private const uint TicksPerMinute = 60 * 60;

    // Reflection fields
    private static MethodInfo _modNetSyncMods;
    private static MethodInfo _modNetSendNetIds;
    private static MethodInfo _netMessageSyncOnePlayer;
    private static MethodInfo _netMessageSendNPCHousesAndTravelShop;

    public override void Load()
    {
        _modNetSyncMods = typeof(ModNet).GetMethod("SyncMods", BindingFlags.NonPublic | BindingFlags.Static);
        _modNetSendNetIds = typeof(ModNet).GetMethod("SendNetIDs", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSyncOnePlayer =
            typeof(NetMessage).GetMethod("SyncOnePlayer", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSendNPCHousesAndTravelShop = typeof(NetMessage).GetMethod("SendNPCHousesAndTravelShop",
            BindingFlags.NonPublic | BindingFlags.Static);

        // FIXME: This should only be done for the replay client, not ALL clients!
        // Always broadcast DamageNPC regardless of distance to the client's player.
        IL_NetMessage.SendData += EditNetMessageSendData;
    }

    public override void Unload()
    {
        IL_NetMessage.SendData -= EditNetMessageSendData;

        // make sure we don't leave a ref to our socket lying around
        foreach (var client in Netplay.Clients)
        {
            if (client.Socket is RecordSocket)
            {
                client.Socket.Close();
                client.Socket = new TcpSocket();
            }
        }
    }

    private void PrepareNetplay(RemoteClient client, ReplayFile replay)
    {
        const string recordClientName = "Recording";

        client.Reset();
        client.Name = recordClientName;
        // FIXME: File name too long? file path too long? do we care is that our problem??
        client.Socket = recordSocket = new RecordSocket(this, client, replay);
        WhoAmI = (byte)client.Id;

        // RemoteClient.Update would set this because Socket.IsConnected() returned true, but we need this now, so
        // fast-track it.
        client.IsActive = true;
    }

    public ReplayFile Start()
    {
        if (IsRecording)
            throw new InvalidOperationException("recording already in progress");

        var dir = ReplayPaths.GetFolder();

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception e)
        {
            throw new Exception("failed to create auto replay directory", e);
        }

        const string prefix = "Reese";
        var num = GetNextReplayNumber(dir, prefix);

        currentReplayPath = Path.Combine(dir, $"{prefix}_{num:0000}.reese");

        var stream = File.OpenWrite(currentReplayPath);
        return Start(stream);
    }

    public ReplayFile Start(Stream stream)
    {
        if (IsRecording)
            throw new InvalidOperationException("recording already in progress");

        var now = DateTime.UtcNow;
        Tick = 0;
        baselineIntervalTicks = GetBaselineIntervalTicks();
        nextBaselineTick = baselineIntervalTicks;
        suppressAutoStartAfterMaxLength = false;
        TimelineRecorder.Begin();
        TimelineRecorder.RecordActivePlayersJoined(Tick);
        ModContent.GetInstance<TimelineTrackerSystem>().ResetForRecordingStart();
        const int recordClientIndex = 254;
        var client = Netplay.Clients[recordClientIndex];

        var replay = ReplayFile.Write(stream);
        Console.WriteLine(
            $"[Reese] Client {recordClientIndex} started recording to: \"{Path.GetFileName(currentReplayPath)}\"");
        PrepareNetplay(client, replay);

        replay.MetaInfo.WhoAmI = (byte)client.Id;
        replay.MetaInfo.Title = "A Terraria World";
        replay.MetaInfo.Start = now;
        replay.MetaInfo.WorldName = Main.worldName;

        recordSocket.BeginBaselineCapture();
        SendSignOn(client);
        SendReplayWorldSnapshot(client, true);
        recordSocket.EndBaselineCapture();

        RecorderStatus.Start(currentReplayPath);
        RecorderStatus.SyncToClients();

        for (var i = 0; i < Netplay.MaxConnections; i++)
        {
            if (i != client.Id && Netplay.Clients[i].IsActive)
                RecorderStatus.SendStartMessage(i);
        }

        return replay;
    }

    private static void SendSignOn(RemoteClient client)
    {
        // Client says hello
        // Server sets State to 1 and syncs mods
        client.State = 1;
        _modNetSyncMods.Invoke(null, [client.Id]);
        // Client syncs mods to indicate it's done and ready
        // Server sends net IDs and PlayerInfo
        _modNetSendNetIds.Invoke(null, [client.Id]);
        NetMessage.SendData(MessageID.PlayerInfo, client.Id);
        // Client eventually sends RequestWorldData
        // Server sets State to 2 and sends WorldData and syncs invasion
        client.State = 2;
        NetMessage.SendData(MessageID.WorldData, client.Id);
        Main.SyncAnInvasion(client.Id);
    }

    private static void SendReplayWorldSnapshot(RemoteClient client, bool signingOn = false)
    {
        // Client waits for world clear and state bullshit, eventually sends SpawnTileData.
        // Server sends WorldData (again yes), calculates portal bullshit(???), StatusTextSize (who cares),
        // set State to 3, TileSection for world spawn and maybe player spawn and maybe portal sections, SyncItem and
        // ItemOwner for all active items, SyncNPC for all active NPCs, SyncProjectile for all applicable projectiles,
        // NPCKillCountDeathTally for all NPC types, TileCounts, MoonlordHorror, UpdateTowerShieldStrengths,
        // SyncCavernMonsterType, InitialSpawn.
        NetMessage.SendData(MessageID.WorldData, client.Id);
        if (signingOn)
        {
            // NOTE: Not sending status text, who cares
            client.State = 3;

            // should have been done already, but just in case.
            client.ResetSections();
        }
        else
        {
            Main.SyncAnInvasion(client.Id);
        }

        for (var x = 0; x < Main.maxSectionsX; x++)
        {
            for (var y = 0; y < Main.maxSectionsY; y++)
                NetMessage.SendSection(client.Id, x, y);
        }

        for (var i = 0; i < Main.maxItems; i++)
        {
            var item = Main.item[i];
            if (item.active)
            {
                NetMessage.SendData(MessageID.SyncItem, client.Id, number: i);
                NetMessage.SendData(MessageID.ItemOwner, client.Id, number: i);
            }
        }

        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (npc.active)
                NetMessage.SendData(MessageID.SyncNPC, client.Id, number: i);
        }

        // NOTE: Applicable projectiles is a subset of all projectiles, but in our mod, that subset is ALWAYS equal to
        // all projectiles anyhow.
        for (var i = 0; i < 1000; i++)
        {
            if (Main.projectile[i].active)
                NetMessage.SendData(MessageID.SyncProjectile, client.Id, number: i);
        }

        for (var i = 0; i < NPCLoader.NPCCount; i++)
            NetMessage.SendData(MessageID.NPCKillCountDeathTally, client.Id, number: i);

        NetMessage.SendData(MessageID.TileCounts, client.Id);

        if (signingOn)
            NetMessage.SendData(MessageID.MoonlordHorror);
        else
            NetMessage.SendData(MessageID.MoonlordHorror, client.Id);

        NetMessage.SendData(MessageID.UpdateTowerShieldStrengths, client.Id);
        NetMessage.SendData(MessageID.SyncCavernMonsterType, client.Id);

        if (signingOn)
        {
            NetMessage.SendData(MessageID.InitialSpawn, client.Id);
            Main.BestiaryTracker.OnPlayerJoining(client.Id);
            CreativePowerManager.Instance.SyncThingsToJoiningPlayer(client.Id);
            Main.PylonSystem.OnPlayerJoining(client.Id);

            // Client sends a PlayerSpawn from Player.Spawn. Server calls Player.Spawn, sets State to 10, enables
            // broadcast, NetMessage.SyncConnectedPlayer, maybe SetCountsAsHostForGameplay, AnglerQuest,
            // FinishedConnectingToServer, NetMessage.greetPlayer(whoAmI).
            // NOTE: We really don't want a player for this client, so we start to deviate here.
            client.State = 10;
            NetMessage.buffer[client.Id].broadcast = true;
        }

        // Can't call NetMessage.SyncConnectedPlayer because we don't want to sync ourselves to everyone else by
        // calling SyncOnePlayer with the record client (we aren't a player!).
        for (var i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i].active)
                _netMessageSyncOnePlayer.Invoke(null, [i, client.Id, -1]);
        }

        _netMessageSendNPCHousesAndTravelShop.Invoke(null, [client.Id]);
        NetMessage.SendAnglerQuest(client.Id);
        CreditsRollEvent.SendCreditsRollRemainingTimeToPlayer(client.Id);
        NPC.RevengeManager.SendAllMarkersToPlayer(client.Id);

        NetMessage.SendData(MessageID.AnglerQuest, client.Id,
            text: NetworkText.FromLiteral(Main.player[client.Id].name),
            number: Main.anglerQuest);

        if (signingOn)
            NetMessage.SendData(MessageID.FinishedConnectingToServer, client.Id);

        // ReplaySnapshotEvents.RaiseReplaySnapshotWriting(client.Id, Ticks, initial);
    }

    public void Stop(NetworkText reason)
    {
        if (!IsRecording)
            return;

        NetMessage.SendData(MessageID.Kick, WhoAmI, text: reason);

        TimelineEvent[] timelineEvents = TimelineRecorder.Finish();
        ModContent.GetInstance<TimelineTrackerSystem>().EndRecording();

        RecorderStatus.Stop(Tick);
        RecorderStatus.SyncToClients(force: true);

        string savedReplayPath = currentReplayPath;
        string worldName = ReplayStats.GetCurrentWorldName();
        string[] modNames = ReplayStats.GetCurrentModNames();
        ReplayModBundle modBundle = ShouldCaptureModsUsedInReplay()
            ? ReplayModBundle.CaptureLoadedMods()
            : null;

        if (modBundle != null)
            Log.Info(
                $"Stopping recording with embedded replay mod bundle: {modBundle.Mods.Length} mods, {ReplayModFile.FormatBytes(modBundle.Mods.Sum(x => x.PayloadLength))}.");
        else
            Log.Info(
                "Stopping recording without an embedded replay mod bundle because CaptureModsUsedInReplay is disabled.");

        // recordSocket.Finish(Ticks, worldName, modNames, ReplayFileFlags.New, timelineEvents, modBundle);
        recordSocket.Close();

        NetMessage.buffer[WhoAmI].broadcast = false;
        var client = Netplay.Clients[WhoAmI];
        client.IsActive = false;
        client.State = 0;

        // ReplayPlayback.NotifyFolderChanged();
        currentReplayPath = null;

        bool savedReplayFinished = !string.IsNullOrWhiteSpace(savedReplayPath) && File.Exists(savedReplayPath);

        if (!string.IsNullOrWhiteSpace(savedReplayPath))
        {
            string message =
                $"[Reese] Recording stopped at tick {Tick}. Reason: {reason}. Saved to {Path.GetFileNameWithoutExtension(savedReplayPath)}";
            Log.Info(message);
            Console.WriteLine(message);

            if (savedReplayFinished)
                RecorderEvents.RaiseRecordingFinished(savedReplayPath, worldName, modNames, Tick, reason);
        }

        recordSocket = null;
        WhoAmI = 0;
    }

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
            bool hasPlayers = Main.player.Any(player => player.active);
            bool autoStartRecording = ModContent.GetInstance<ServerConfig>()?.AutoStartRecordingWhenPlayersAreInWorld ??
                                      false;

            if (!hasPlayers)
            {
                suppressAutoStartAfterMaxLength = false;

                if (IsRecording)
                    Stop(NetworkText.FromKey("Mods.Reese.Recorder.Stop.NoPlayers"));
            }
            else if (!IsRecording && autoStartRecording && CanAutoStartRecording())
            {
                Start();
            }
        }

        if (IsRecording)
        {
            // Advance tick!
            Tick++;
            RecorderStatus.UpdateTick(Tick);
            RecorderStatus.SyncToClients();

            if (ShouldAutoStopRecording(out int maxRecordingLengthMinutes))
            {
                suppressAutoStartAfterMaxLength = !ShouldAutoStartRecordingAfterMaxLength();
                Stop(NetworkText.FromKey("Mods.Reese.Recorder.Stop.MaxLength"));
                return;
            }

            UpdateBaselineSchedule();

            if (baselineIntervalTicks > 0 && Tick >= nextBaselineTick)
                WriteBaselineCheckpoint();

            // Logging at 1 tick, 5 seconds, 10 seconds, and every 30 minutes thereafter
            if (Tick == 1 || Tick == 300 || Tick == 600 || Tick % (30 * 60 * 60) == 0)
            {
                string timeString = TimeSpan.FromSeconds(Tick / 60.0).ToString(@"hh\:mm\:ss");
                string fileName = $"{Path.GetFileNameWithoutExtension(currentReplayPath)}";
                string message =
                    $"[Reese] Reese is currently recording! Filename: {fileName} | Length: {timeString} | Packets: {RecorderStatus.TotalPacketsSent} | Size: {RecorderStatus.TotalBytesSent / 1024.0:F0} KB";

                if (Tick == 600)
                {
                    message +=
                        "\n[Reese] The recording has passed 10 seconds! Future logs will now be sent once every 30 minutes. Use /recordstatus to view current recording status info (length, packets, size).";
                }

                Console.WriteLine(message);
                Log.Info(message);
            }
        }
    }

    private void WriteBaselineCheckpoint()
    {
        nextBaselineTick = Tick + baselineIntervalTicks;

        if (!IsRecording)
            return;

        uint baselineTick = Tick;
        bool completed = false;
        int byteSize = 0;

        Log.Info($"Recording replay baseline at tick {baselineTick}...");
        recordSocket.BeginBaselineCapture();

        try
        {
            SendReplayWorldSnapshot(Netplay.Clients[WhoAmI]);
            completed = true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to record replay baseline at tick {baselineTick}: {e}");
        }
        finally
        {
            if (completed)
                byteSize = recordSocket.EndBaselineCapture();
            else
                recordSocket.CancelBaselineCapture();
        }
    }

    private void UpdateBaselineSchedule()
    {
        uint configuredBaselineIntervalTicks = GetBaselineIntervalTicks();
        if (baselineIntervalTicks == configuredBaselineIntervalTicks)
            return;

        baselineIntervalTicks = configuredBaselineIntervalTicks;
        nextBaselineTick = baselineIntervalTicks > 0 ? Tick + baselineIntervalTicks : 0;
    }

    private static uint GetBaselineIntervalTicks()
    {
        int configuredSeconds = ModContent.GetInstance<ServerConfig>()?.BaselineIntervalSeconds
                                ?? ServerConfig.DefaultBaselineIntervalSeconds;
        configuredSeconds = Math.Clamp(configuredSeconds, 0, ServerConfig.MaxBaselineIntervalSeconds);
        int configuredTicks = checked(configuredSeconds * 60);

        return (uint)Math.Max(0, configuredTicks);
    }

    private bool ShouldAutoStopRecording(out int maxRecordingLengthMinutes)
    {
        maxRecordingLengthMinutes = GetMaxRecordingLengthMinutes();
        if (maxRecordingLengthMinutes <= 0)
            return false;

        ulong maxRecordingTicks = (ulong)maxRecordingLengthMinutes * TicksPerMinute;
        return Tick >= maxRecordingTicks;
    }

    private static int GetMaxRecordingLengthMinutes()
    {
        int configuredMinutes = GetMaxLengthRecordingConfig()?.MaxRecordingLengthMinutes ?? 0;

        return Math.Max(0, configuredMinutes);
    }

    private bool CanAutoStartRecording()
    {
        return !suppressAutoStartAfterMaxLength || ShouldAutoStartRecordingAfterMaxLength();
    }

    private static bool ShouldAutoStartRecordingAfterMaxLength()
    {
        return GetMaxLengthRecordingConfig()?.AutoStartRecordingAfterMaxLength ?? false;
    }

    private static ServerConfig.MaxLengthRecordingConfig GetMaxLengthRecordingConfig()
    {
        return ModContent.GetInstance<ServerConfig>()?.maxLengthRecordingConfig;
    }

    private static bool ShouldCaptureModsUsedInReplay()
    {
        return ModContent.GetInstance<ServerConfig>()?.CaptureModsUsedInReplay ?? true;
    }

    public override void OnWorldUnload()
    {
        if (Main.dedServ)
            Stop(NetworkText.FromLiteral("shutting down"));
    }

    public static int GetNextReplayNumber(string dir, string prefix)
    {
        int next = 1;

        foreach (string path in Directory.EnumerateFiles(dir, $"{prefix}_*.reese", SearchOption.AllDirectories))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string suffix = name.Length > prefix.Length + 1 ? name[(prefix.Length + 1)..] : string.Empty;
            if (int.TryParse(suffix, out int number) && number >= next)
                next = number + 1;
        }

        return next;
    }
}

public class RecordRemoteAddress : RemoteAddress
{
    public override string GetIdentifier() => "ReeseRecord";
    public override string GetFriendlyName() => "Reese Record";
    public override bool IsLocalHost() => true;

    public override string ToString() => GetFriendlyName();
}

public class RecordSocket(ITicker ticker, RemoteClient client, ReplayFile replay) : ISocket
{
    private static readonly RecordRemoteAddress RemoteAddress = new();
    private MemoryStream baselineCapture;

    public ReplayFile Replay => replay;

    public void BeginBaselineCapture()
    {
        if (baselineCapture != null)
            throw new InvalidOperationException("Baseline capture is already active.");

        baselineCapture = new MemoryStream();
    }

    public int EndBaselineCapture()
    {
        if (baselineCapture == null)
            return 0;

        var data = baselineCapture.ToArray();
        baselineCapture.Dispose();
        baselineCapture = null;

        Replay.WriteBaseline(data, ticker.Tick);
        return data.Length;
    }

    public void CancelBaselineCapture()
    {
        baselineCapture?.Dispose();
        baselineCapture = null;
    }

    public void Close()
    {
        if (Replay.Terminated)
            return;

        CancelBaselineCapture();

        Log.Info("Closing record socket");
        Replay.Dispose();
    }

    public bool IsConnected()
    {
        return !Replay.Terminated;
    }

    public void Connect(RemoteAddress address) =>
        throw new InvalidOperationException("The recording socket cannot connect");

    public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
    {
        if (Replay.Terminated)
            throw new InvalidOperationException("cannot record to a terminated replay");

        if (baselineCapture != null)
        {
            baselineCapture.Write(data, offset, size);
        }
        else
        {
            RecorderStatus.TrackPacket(data, offset, size, ticker.Tick);
            Replay.WriteData(data[offset..(offset + size)], (int)ticker.Tick);
        }

        callback?.Invoke(state);
    }

    public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback,
        object state = null) =>
        throw new InvalidOperationException("The recording socket cannot receive");

    public bool IsDataAvailable() => false;

    public void SendQueuedPackets()
    {
        if (!Replay.Terminated)
            client.TimeOutTimer = 0;
    }

    public bool StartListening(SocketConnectionAccepted callback) =>
        throw new InvalidOperationException("The recording socket cannot listen");

    public void StopListening() =>
        throw new InvalidOperationException("The recording socket cannot listen");

    public RemoteAddress GetRemoteAddress() => RemoteAddress;
}
