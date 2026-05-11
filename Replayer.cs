using log4net;
using MonoMod.RuntimeDetour;
using Reese.Common.Replayer;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese;

[Autoload(Side = ModSide.Client)]
public class Replayer : ModSystem, ITicker
{
    private delegate void HighFpsSupportConfigEnsureValidateStateDelegate(object self);

    /// <summary>Playback position used for packet gating and HUD (advanced in <see cref="AdvancePlaybackTick"/>).</summary>
    public uint Ticks { get; private set; }

    internal void SetPlaybackTickForSeek(uint value) => Ticks = value;

    public static ReplayMetadata ActiveMetadata { get; private set; } = new ReplayMetadata();
    public static uint ActiveDurationTicks { get; private set; }

    private static ReplaySocket _activePlaybackSocket;

    public static uint CurrentTick => ModContent.GetInstance<Replayer>()?.Ticks ?? 0;

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        if (address.GetIdentifier() != "10.2.3.4")
            return;

        Ticks = 0;
        Mod.Logger.Info("Connecting to magic replay IP thingy!");
        Netplay.Connection = new RemoteServer();
        Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue];

        string replayPath = ReplaySession.CurrentPath;

        if (string.IsNullOrWhiteSpace(replayPath) || !File.Exists(replayPath))
        {
            Mod.Logger.Error($"Replay path missing or invalid: {replayPath ?? "<null>"}");
            ReplaySession.End("missing replay path");
            Main.menuMode = 0;
            return;
        }

        Mod.Logger.Info($"Opening replay file: {replayPath}");

        FileStream stream = File.Open(replayPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ReplayFile replayFile = ReplayFile.Read(stream);

        ActiveMetadata = replayFile.Metadata;
        ActiveDurationTicks = ActiveMetadata.DurationTicks;

        var socket = new ReplaySocket(this, replayFile);
        _activePlaybackSocket = socket;
        Netplay.Connection.Socket = socket;
    }

    public void AdvancePlaybackTick()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        if (ActiveDurationTicks > 0 && Ticks >= ActiveDurationTicks)
            return;

        Ticks++;
    }

    private static void ClearPlaybackSocketIf(ReplaySocket socket)
    {
        if (ReferenceEquals(_activePlaybackSocket, socket))
            _activePlaybackSocket = null;
    }

    /// <summary>Clears static playback state when a session ends or the mod unloads.</summary>
    public void ClearPlaybackState()
    {
        ActiveMetadata = new ReplayMetadata();
        ActiveDurationTicks = 0;
        _activePlaybackSocket = null;
    }

    public static void SeekToTick(uint targetTick)
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        var replayer = ModContent.GetInstance<Replayer>();
        uint duration = ActiveDurationTicks;
        if (duration > 0)
            targetTick = Math.Min(targetTick, duration);

        bool resetStream = targetTick < replayer.Ticks;
        if (resetStream && _activePlaybackSocket?.ResetToStart() != true)
        {
            Main.statusText = "Unable to seek replay";
            return;
        }

        if (!resetStream && targetTick == replayer.Ticks)
            return;

        replayer.SetPlaybackTickForSeek(targetTick);
        _activePlaybackSocket?.ClearWaitLog();

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;
    }

    public static void SeekToStart()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        var replayer = ModContent.GetInstance<Replayer>();
        if (_activePlaybackSocket?.ResetToStart() != true)
        {
            Main.statusText = "Unable to seek replay";
            return;
        }

        replayer.SetPlaybackTickForSeek(0);

        if (Netplay.Connection != null)
            Netplay.Connection.StatusText = string.Empty;
    }

    public static void SeekToEnd()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        uint duration = ActiveDurationTicks;
        if (duration == 0)
            return;

        SeekToTick(duration);
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(0f);
    }

    public override void Unload()
    {
        ClearPlaybackState();
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReplaySocket));
        private readonly ReplayRemoteAddress _remoteAddress = new();
        private bool _reportedWaitingForTick;

        public void Close()
        {
            ClearPlaybackSocketIf(this);
            Logger.Info("Closing replay socket");
            replayFile.Dispose();
        }

        public bool ResetToStart()
        {
            _reportedWaitingForTick = false;
            return replayFile.ResetRead();
        }

        public void ClearWaitLog() => _reportedWaitingForTick = false;

        public bool IsConnected() => true;

        public void Connect(RemoteAddress address)
        {
            Logger.Info($"Replay connect to {address}");
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            //callback?.Invoke(state);
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
        {
            if (!IsDataAvailable())
            {
                callback(state, 0);
                return;
            }

            var numberOfBytesRead = replayFile.ReadPacketData(data.AsSpan()[offset..(offset + size)]);
            callback(state, numberOfBytesRead);
        }

        public bool IsDataAvailable()
        {
            bool tickOk = ticker.Ticks >= replayFile.Tick;
            bool bytesOk = replayFile.NumberOfPacketDataBytesRemaining > 0;
            if (!tickOk && bytesOk && !_reportedWaitingForTick)
            {
                _reportedWaitingForTick = true;
                Logger.Info($"Replay waiting for tick {replayFile.Tick}; playback tick is {ticker.Ticks}");
            }

            return tickOk && bytesOk;
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
