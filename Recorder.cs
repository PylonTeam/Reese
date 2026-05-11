using System;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
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
    // FIXME: Become delegate
    public uint Ticks { get; private set; }
    private static MethodInfo _modNetSyncMods;
    private static MethodInfo _modNetSendNetIds;
    private static MethodInfo _netMessageSyncOnePlayer;
    private static MethodInfo _netMessageSendNPCHousesAndTravelShop;
    private static MethodInfo _netPlayKickClient;

    public override void Load()
    {
        _modNetSyncMods = typeof(ModNet).GetMethod("SyncMods", BindingFlags.NonPublic | BindingFlags.Static);
        _modNetSendNetIds = typeof(ModNet).GetMethod("SendNetIDs", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSyncOnePlayer =
            typeof(NetMessage).GetMethod("SyncOnePlayer", BindingFlags.NonPublic | BindingFlags.Static);
        _netMessageSendNPCHousesAndTravelShop = typeof(NetMessage).GetMethod("SendNPCHousesAndTravelShop",
            BindingFlags.NonPublic | BindingFlags.Static);
        _netPlayKickClient = typeof(Netplay).GetMethod("KickClient", BindingFlags.NonPublic | BindingFlags.Static);

        On_Netplay.InitializeServer += OnNetplayInitializeServer;

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

        // IL_Main.DoUpdate += il =>
        // {
        //     var cursor = new ILCursor(il);
        //     cursor.GotoNext(i => i.MatchStsfld<Main>("drawSkip"));
        //     // cursor.Index += 1;
        //     cursor.EmitDelegate(() =>
        //     {
        //         Ticks++;
        //         if ((Ticks % 60) == 0)
        //             Mod.Logger.Info("Tick!");
        //     });
        // };
    }

    private void StartRecording()
    {
        // FIXME: Will this break an existing recording that we try to end? prob need to do it later.
        Ticks = 0;
        const int RecordClientIndex = 254;
        const string RecordClientName = "Recording";

        var replayFile = ReplayFile.Write(File.Open($"{Main.worldName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.reese",
            FileMode.Create));

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
        // Client waits for world clear and state bullshit, eventually sends SpawnTileData
        // Server sends WorldData (again yes), calculates portal bullshit(???), StatusTextSize (who cares),
        // set State to 3, TileSection for world spawn and maybe player spawn and maybe portal sections, SyncItem and
        // ItemOwner for all active items, SyncNPC for all active NPCs, SyncProjectile for all applicable projectiles,
        // NPCKillCountDeathTally for all NPC types, TileCounts, MoonlordHorror (as a broadcast, probably a bug lol),
        // UpdateTowerShieldStrengths, SyncCavernMonsterType, InitialSpawn.
        // Main.BestiaryTracker.OnPlayerJoining(whoAmI);
        // CreativePowerManager.Instance.SyncThingsToJoiningPlayer(whoAmI);
        // Main.PylonSystem.OnPlayerJoining(whoAmI);

        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        // NOTE: Not sending status text, who cares
        recordClient.State = 3;
        // Let's send ALL tile sections for the ENTIRE world
        // Likely handles world spawn, player spawn, and portal bullshit
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
        // NOTE: Yes, we broadcast this for whatever reason, to match vanilla.
        NetMessage.SendData(MessageID.MoonlordHorror);
        NetMessage.SendData(MessageID.UpdateTowerShieldStrengths, recordClient.Id);
        NetMessage.SendData(MessageID.SyncCavernMonsterType, recordClient.Id);
        NetMessage.SendData(MessageID.InitialSpawn, recordClient.Id);

        Main.BestiaryTracker.OnPlayerJoining(recordClient.Id);
        CreativePowerManager.Instance.SyncThingsToJoiningPlayer(recordClient.Id);
        Main.PylonSystem.OnPlayerJoining(recordClient.Id);

        // Client sends a PlayerSpawn from Player.Spawn
        // Server calls Player.Spawn, sets State to 10, enables broadcast, NetMessage.SyncConnectedPlayer, maybe
        // SetCountsAsHostForGameplay, AnglerQuest, FinishedConnectingToServer, NetMessage.greetPlayer(whoAmI), 
        // NOTE: We really don't want a player for this client, so we start to deviate here.

        recordClient.State = 10;
        NetMessage.buffer[recordClient.Id].broadcast = true;

        // Can't call NetMessage.SyncConnectedPlayer because we don't want to sync ourselves to everyone else by
        // calling SyncOnePlayer with the record client (we aren't a player!)
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
        NetMessage.SendData(MessageID.FinishedConnectingToServer, recordClient.Id);

        // Flush now, so that it comes at update delta 0
        replayFile.FlushTick();
    }

    private void OnNetplayInitializeServer(On_Netplay.orig_InitializeServer orig)
    {
        orig();
        StartRecording();
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
        Ticks++;
        if (Ticks % 60 == 0)
            Mod.Logger.Info("Tick!");
    }

    public override void OnWorldUnload()
    {
        if (Main.dedServ)
        {
            // Guess our shit isn't closed when the server dies. Would be nice to do it to everyone, but that's a big
            // change from status-quo, so just do it for ourselves.
            foreach (var remoteClient in Netplay.Clients)
            {
                if (remoteClient.Socket is RecordSocket recordSocket)
                    recordSocket.Close();
            }
        }
    }

    public class RecordCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            ModContent.GetInstance<Recorder>().StartRecording();
        }

        public override string Command => "record";
        public override CommandType Type => CommandType.Console;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RecordSocket));
        private readonly RecordRemoteAddress _remoteAddress = new();

        public void Close()
        {
            _netPlayKickClient.Invoke(null, [this, NetworkText.FromLiteral("Recording closed")]);
            // This is essentially a flush operation
            SendQueuedPackets();
            Logger.Info("Closing record socket");
            replayFile.Dispose();
        }

        public bool IsConnected() => true;

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
            // TODO: Verify this is actually preventing us from timing out
            //       (and that this is an issue at all which i think it is)
            remoteClient.TimeOutTimer = 0;
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The recording socket cannot listen");

        public void StopListening() => throw new InvalidOperationException("The recording socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;
    }
}