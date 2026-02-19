using System;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese;

public class Replayer : ModSystem
{
    private delegate void HighFpsSupportConfigEnsureValidateStateDelegate(object self);

    private Hook _highFpsSupportConfigEnsureValidStateHook;

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
    }

    public override void PostSetupContent()
    {
        _highFpsSupportConfigEnsureValidStateHook = new Hook(
            ModLoader.GetMod("HighFPSSupport").GetType().Assembly.GetType("HighFPSSupport.Config")
                .GetMethod("EnsureValidState", BindingFlags.Public | BindingFlags.Instance),
            OnHighFpsSupportConfigEnsureValidState);
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (address.GetIdentifier() == "10.2.3.4")
        {
            Mod.Logger.Info("Connecting to magic replay IP thingy!");
            Netplay.Connection = new RemoteServer();
            Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
            Netplay.Connection.Socket = new ReplaySocket(ReplayFile.Read(File.OpenRead("record.bin")));
        }
    }

    private void OnHighFpsSupportConfigEnsureValidState(HighFpsSupportConfigEnsureValidateStateDelegate orig,
        object self)
    {
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

    private class ReplaySocket(ReplayFile replayFile) : ISocket
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
            return Main.GameUpdateCount >= replayFile.GameUpdateCount &&
                   replayFile.NumberOfPacketDataBytesRemaining > 0;
        }

        public void SendQueuedPackets()
        {
        }

        public bool StartListening(SocketConnectionAccepted callback) =>
            throw new InvalidOperationException("The replaying socket cannot listen");

        public void StopListening() => throw new InvalidOperationException("The replaying socket cannot listen");

        public RemoteAddress GetRemoteAddress() => _remoteAddress;

        public class UpdateRateCommand : ModCommand
        {
            public override void Action(CommandCaller caller, string input, string[] args)
            {
                if (args.Length < 1 || !int.TryParse(args[0], out var updateRate))
                    return;

                Main.instance.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / updateRate);
                caller.Reply(
                    $"Update rate is now {updateRate}/s ({Main.instance.TargetElapsedTime.TotalMilliseconds:F4}ms)");

                if (Main.FrameSkipMode != FrameSkipMode.On)
                    caller.Reply("Hey, check your frame skip setting!", Color.Orange);
            }

            public override string Command => "updaterate";
            public override CommandType Type => CommandType.Chat;
        }
    }
}