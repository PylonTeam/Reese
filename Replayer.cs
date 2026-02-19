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
    private delegate void HighFpsSupportConfigEnsureValidateStateDelegate(object self);

    public uint Ticks { get; private set; }
    private Hook _highFpsSupportConfigEnsureValidStateHook;

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
                    Mod.Logger.Info("Tick!");
            });
        };
    }

    public override void PostSetupContent()
    {
        if (!Main.dedServ)
        {
            if (ModLoader.TryGetMod("HighFPSSupport", out var highFpsSupport))
            {
                Mod.Logger.Info("Enabling HighFPSSupport interop to allow tick rate modification for replays");
                // If we have the High FPS Support mod installed and loaded, we want to override their config validator
                // (which ensures their tick rate modification only functions in single-player) to also function for
                // multiplayer clients if a replay is being played.
                _highFpsSupportConfigEnsureValidStateHook = new Hook(
                    highFpsSupport.GetType().Assembly.GetType("HighFPSSupport.Config").GetMethod("EnsureValidState",
                        BindingFlags.Public | BindingFlags.Instance), OnHighFpsSupportConfigEnsureValidState);
            }
        }
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (address.GetIdentifier() == "10.2.3.4")
        {
            Ticks = 0;
            Mod.Logger.Info("Connecting to magic replay IP thingy!");
            Netplay.Connection = new RemoteServer();
            Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
            Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(File.OpenRead("record.bin")));
        }
    }

    private void OnHighFpsSupportConfigEnsureValidState(HighFpsSupportConfigEnsureValidateStateDelegate orig,
        object self)
    {
        // If we are watching a replay, then don't allow this to be invoked -- it will reset the tick rate option to the
        // default, because it is only meant to function in single-player. In our scenario, it's totally okay for it to
        // function with this multiplayer client.
        if (Netplay.Connection?.Socket is ReplaySocket)
            return;

        orig(self);
    }

    public override void Unload()
    {
        _highFpsSupportConfigEnsureValidStateHook?.Dispose();
        _highFpsSupportConfigEnsureValidStateHook = null;
    }

    private class ReplayRemoteAddress : RemoteAddress
    {
        public override string GetIdentifier() => "Replaying";
        public override string GetFriendlyName() => "Replaying";
        public override bool IsLocalHost() => true;

        public override string ToString() => GetFriendlyName();
    }

    private class ReplaySocket(ITicker ticker, ReplayFile replayFile) : ISocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReplaySocket));
        private readonly ReplayRemoteAddress _remoteAddress = new();

        public void Close()
        {
            Logger.Info("Closing replay socket");
            replayFile.Dispose();
        }

        public bool IsConnected() => true;

        public void Connect(RemoteAddress address)
        {
            Logger.Info($"Replay connect to {address}");
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