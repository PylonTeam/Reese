using System;
using System.IO;
using System.Reflection;
using log4net;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese;

[Autoload(Side = ModSide.Client)]
public class Replayer : ModSystem, ITicker
{
    public uint Ticks { get; private set; }
    public static string PendingReplayPath;

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
        IL_Main.DoUpdate += il =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(i => i.MatchStsfld<Main>("drawSkip"));
            // cursor.Index += 1;
            cursor.EmitDelegate(() =>
            {
                Ticks++;
                if ((Ticks % 60) == 0)
                    Log.Info("Client tick: " + Ticks);
            });
        };
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (address.GetIdentifier() == "10.2.3.4")
        {
            // Get the path of the replay file
            //var stagePath = ReeseReplayPaths.GetFile();
            var stagePath = PendingReplayPath;
            PendingReplayPath = null;

            if (!File.Exists(stagePath))
            {
                Netplay.Disconnect = true;
                Main.statusText = $"Replay file not found: \n'{stagePath}'";
                Log.Warn(Main.statusText);
                return;
            }

            Ticks = 0;
            Log.Info("Connecting to magic replay IP thingy!");
            Netplay.Connection = new RemoteServer();
            Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
            Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(File.OpenRead(stagePath)));
        }
    }

    private class ReplayRemoteAddress : RemoteAddress
    {
        public override string GetIdentifier() => "Replaying";
        public override string GetFriendlyName() => "Replaying";
        public override bool IsLocalHost() => true;

        public override string ToString() => GetFriendlyName();
    }

    public class ReplaySocket(ITicker ticker, ReplayFile replayFile) : ISocket
    {
        private readonly ReplayRemoteAddress _remoteAddress = new();

        public void Close()
        {
            Log.Info("Closing replay socket");
            replayFile.Dispose();
        }

        public bool IsConnected() => true;

        public void Connect(RemoteAddress address)
        {
            Log.Info($"Replay connect to {address}");
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
        {
            // TODO: Is this called even if IsDataAvailable returns false?
            if (!IsDataAvailable())
                throw new InvalidOperationException("I SAID NOTHING WAS AVAILABLE YOU BITCH.");

            var numberOfBytesRead = replayFile.ReadPacketData(data.AsSpan()[offset..(offset + size)]);

            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            return ticker.Ticks >= replayFile.Tick && replayFile.NumberOfPacketDataBytesRemaining > 0;
        }

        public void SendQueuedPackets()
        {
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The replaying socket cannot listen");

        public void StopListening() => throw new InvalidOperationException("The replaying socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;
    }
}