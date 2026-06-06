using Reese.Common.Replayer;
using Reese.Core.Localization;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace Reese;

// FIXME: cache/optimize reflection

// TODO :
// This class has been disabled because it prevents the server from starting, making us unable to use the recorder at all.
[Autoload(false)]
public class EncryptEverything : ModSystem
{
    private class SslSocket : ISocket
    {
        private SslStream ssl;

        // Cached remote address for serverbound connection or from clientbound connection.
        private RemoteAddress remoteAddress;
        private bool done;

        private TcpListener listener;
        private SocketConnectionAccepted acceptCallback;

        public X509Certificate Certificate { get; set; }

        private static X509Certificate GenerateSelfSignedServerCertificate()
        {
            var request = new CertificateRequest("cn=tModLoader Server", ECDsa.Create(), HashAlgorithmName.SHA256);
            var now = DateTimeOffset.Now;
            return request.CreateSelfSigned(now, now.AddYears(1));
        }

        public bool StartListening(SocketConnectionAccepted callback)
        {
            var address = IPAddress.Any;
            if (Program.LaunchParameters.TryGetValue("-ip", out var value) && !IPAddress.TryParse(value, out address))
                address = IPAddress.Any;

            Certificate ??= GenerateSelfSignedServerCertificate();

            listener ??= new TcpListener(address, 7777);
            listener.Start();
            listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);

            acceptCallback = callback;

            return true;
        }

        public bool IsConnected()
        {
            // FIXME: i have no clue!
            return !done && ssl != null && ssl.IsAuthenticated;
        }

        private void OnAcceptTcpClient(IAsyncResult ar)
        {
            var tcpClient = listener.EndAcceptTcpClient(ar);
            var ipEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            var remoteAddress = new TcpAddress(ipEndPoint.Address, ipEndPoint.Port);
            var sslSocket = new SslSocket
            {
                ssl = new SslStream(tcpClient.GetStream()),
                remoteAddress = remoteAddress
            };

            // FIXME: weirdly here maybe it okay tho, reaching into sslSocket to do this thing and give it a callback
            sslSocket.ssl.BeginAuthenticateAsServer(Certificate, sslSocket.OnAuthenticateAsServer, acceptCallback);

            listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        }

        public void Close()
        {
            done = true;
            ModLoader.GetMod("Reese").Logger.Info("ok gotta close socket");

            if (ssl != null)
            {
                // FIXME: dispose async?
                ssl.Dispose();
                // ssl = null;
            }

            listener?.Dispose();
        }

        public void Connect(RemoteAddress address)
        {
            if (address is not TcpAddress tcpAddress)
                throw new ArgumentException("address must be TcpAddress", nameof(address));

            var tcpClient = new TcpClient();
            tcpClient.BeginConnect(tcpAddress.Address, tcpAddress.Port, OnConnect, tcpClient);
            remoteAddress = address;
        }

        private void OnConnect(IAsyncResult ar)
        {
            var tcpClient = (TcpClient)ar.AsyncState!;
            tcpClient.EndConnect(ar);

            // FIXME: dispose async?
            ssl = new SslStream(tcpClient.GetStream(), false, ValidateRemoteCertificate);
            ssl.BeginAuthenticateAsClient("tModLoader Server", OnAuthenticateAsClient, null);
        }

        private void OnAuthenticateAsClient(IAsyncResult ar)
        {
            ssl.EndAuthenticateAsClient(ar);
        }

        private void OnRead(IAsyncResult ar)
        {
            int length;
            try
            {
                length = ssl.EndRead(ar);
            }
            catch (ObjectDisposedException)
            {
                done = true;
                ModLoader.GetMod("Reese").Logger.Info($"EndRead whilst disposed prob ignore");
                return;
            }
            catch (Exception e)
            {
                ModLoader.GetMod("Reese").Logger.Warn($"Fuck while EndRead {e}");
                Close();
                return;
            }

            if (length == 0)
            {
                ModLoader.GetMod("Reese").Logger.Warn("ZERO READ LEN so we close now");
                Close();
                return;
            }

            ((Action<int>)ar.AsyncState!)(length);
        }

        private bool ValidateRemoteCertificate(object sender, X509Certificate x509Certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            ModLoader.GetMod("Reese").Logger.Info($"client saying YES to server cert {x509Certificate}");
            return true;
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
        {
            ssl.BeginWrite(data, offset, size, OnWrite, () => callback(state));
        }

        private void OnWrite(IAsyncResult ar)
        {
            try
            {
                ssl.EndWrite(ar);
            }
            catch (ObjectDisposedException)
            {
                done = true;
                ModLoader.GetMod("Reese").Logger.Info("EndWrite whilst disposed prob ignore");
                return;
            }
            catch (Exception e)
            {
                ModLoader.GetMod("Reese").Logger.Warn($"Fuck while EndWrite {e}");
                Close();
                return;
            }

            ((Action)ar.AsyncState!)();
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null)
        {
            ssl.BeginRead(data, offset, size, OnRead, (int size) => callback(state, size));
        }

        public bool IsDataAvailable()
        {
            // FIXME: LOL NO?
            return !done && ssl != null && ssl.IsAuthenticated;
        }

        public void SendQueuedPackets()
        {
            return;
        }

        private void OnAuthenticateAsServer(IAsyncResult ar)
        {
            ssl.EndAuthenticateAsServer(ar);

            ((SocketConnectionAccepted)ar.AsyncState!)(this);
        }

        public void StopListening()
        {
            listener.Stop();
        }

        public RemoteAddress GetRemoteAddress()
        {
            return remoteAddress;
        }
    }

    public override void Load()
    {
        On_Netplay.InitializeServer += OnNetplayInitializeServer;
        On_Netplay.ClientLoopSetup += OnNetplayClientLoopSetup;
        On_Netplay.ServerLoop += OnNetplayServerLoop;
    }

    private void OnNetplayServerLoop(On_Netplay.orig_ServerLoop orig)
    {
        orig();

        Log.Info("okay gonna kick disconnect and close all remote clients");
        foreach (var remoteClient in Netplay.Clients)
        {
            if (!remoteClient.IsActive)
                continue;

            typeof(Netplay).GetMethod("KickClient", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,
                [remoteClient.Socket, NetworkText.FromLiteral("Server shutting down")]);

            Player.Hooks.PlayerDisconnect(remoteClient.Id);
            remoteClient.Reset();
        }
    }

    private void OnNetplayClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        if (ReplayPlayback.IsReplayPlayback)
        {
            Log.Debug("Replay playback connection: skipping SSL socket replacement.");
            return;
        }

        Netplay.Connection.Socket = new SslSocket();
    }

    public override void Unload()
    {
        if (Netplay.Connection.Socket is SslSocket)
        {
            Log.Error(
                "We are unloading while the Netplay.Connection socket is an SslSocket! surely THIS IS WRONG!");
        }
    }

    // FIXME: not big enough??
    private const int RemoteClientReadBufferLength = 1024;

    private void OnNetplayInitializeServer(On_Netplay.orig_InitializeServer orig)
    {
        Main.myPlayer = 255;
        Netplay.ServerIP = IPAddress.Any;
        Main.menuMode = MenuID.MultiplayerJoining;
        Main.statusText = Loc.Get("ServerStartedSsl");
        Main.netMode = NetmodeID.Server;
        Netplay.Disconnect = false;

        for (var i = 0; i < Netplay.MaxConnections; i++)
        {
            var client = new RemoteClient();
            client.Reset();
            client.Id = i;
            client.ReadBuffer = new byte[RemoteClientReadBufferLength];

            Netplay.Clients[i] = client;
        }

        Netplay.TcpListener = new SslSocket();
        typeof(Netplay).GetMethod("StartListening", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, []);
    }
}
