using LiteNetLib;
using LiteNetLib.Utils;
using SLProxy.Enums;
using SLProxy.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SLProxy
{
    internal class UdpProxy
    {
        /// <summary>
        /// Milliseconds
        /// </summary>
        public NetPacketPool packetPool = new NetPacketPool();

        public static readonly Dictionary<int, ServerInfo> Servers = new Dictionary<int, ServerInfo>();

        private int _serverId;

        public ServerInfo Settings => Servers[_serverId];

        public async Task Start(int serverId)
        {
            _serverId = serverId;
            Servers.Add(_serverId, ProxyConfig.Instance.Redirects[_serverId]);

            var connections = new ConcurrentDictionary<IPEndPoint, UdpConnection>();

            // TCP will lookup every time while this is only once.
            var ips = await Dns.GetHostAddressesAsync(Settings.ServerIp).ConfigureAwait(false);
            var remoteServerEndPoint = new IPEndPoint(ips[0], Settings.ServerPort);

            var localServer = new UdpClient(AddressFamily.InterNetwork);
            IPAddress localIpAddress = IPAddress.Any;
            localServer.Client.Bind(new IPEndPoint(localIpAddress, Settings.LocalPort));

            Console.WriteLine($"UDP Proxy started redirecting from \"{localIpAddress}:{Settings.LocalPort}\" to \"{Settings.ServerIp}:{Settings.ServerPort}\".");

            var _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                    foreach (var connection in connections.ToArray())
                    {
                        if (connection.Value.LastActivity + ProxyConfig.Instance.ConnectionTimeout < Environment.TickCount64)
                        {
                            connections.TryRemove(connection.Key, out UdpConnection? c);
                            connection.Value.Stop();
                        }
                    }
                }
            });

            while (true)
            {
                try
                {
                    var message = await localServer.ReceiveAsync().ConfigureAwait(false);
                    var sourceEndPoint = message.RemoteEndPoint;
                    byte[] finalMessage = null;
                    PreAuth preAuth = null;

                    if (!connections.ContainsKey(sourceEndPoint))
                    {
                        finalMessage = message.Buffer;
                        preAuth = ValidatePreAuth(sourceEndPoint, ref finalMessage);
                        if (preAuth == null) continue;

                        if (!Servers[_serverId].Players.ContainsKey(preAuth.UserID) && preAuth.IsChallenge)
                            Servers[_serverId].Players.Add(preAuth.UserID, new PlayerInfo()
                            {
                                Flags = preAuth.Flags,
                                Country = preAuth.Country
                            });
                    }


                    var client = connections.GetOrAdd(sourceEndPoint,
                        ep =>
                        {
                            var udpConnection = new UdpConnection(_serverId, (preAuth != null && preAuth.IsChallenge) ? preAuth.UserID : null, localServer, sourceEndPoint, remoteServerEndPoint);
                            udpConnection.Run();
                            return udpConnection;
                        });

                    await client.SendToServerAsync(finalMessage != null ? finalMessage : message.Buffer).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"an exception occurred on receiving a client datagram: {ex}");
                }
            }
        }

        public PreAuth ValidatePreAuth(IPEndPoint address, ref byte[] message)
        {
            NetPacket packet = packetPool.GetPacket(NetConstants.MaxPacketSize);

            packet.Size = message.Length;
            packet.RawData = message;

            if (packet.Property != PacketProperty.ConnectRequest) return null;

            var request = NetConnectRequestPacket.FromData(packet);
            string failedOn = string.Empty;
            var preAuth = PreAuth.ReadPreAuth(request.Data, ref failedOn);

            if (!preAuth.IsValid)
            {
                Console.WriteLine($"[PreAuth] {failedOn} is invalid for {preAuth.UserID}, rejected connection.");
                packetPool.Recycle(packet);
                request.Data.Clear();
                return null;
            }

            NetDataWriter writer = NetDataWriter.FromBytes(message, false);
            writer.Put(address.Address.ToString().Replace("::ffff:", ""));
            message = writer.Data;

            packetPool.Recycle(packet);
            return preAuth;
        }
    }

    internal class UdpConnection
    {
        private readonly UdpClient _localServer;
        private readonly UdpClient _forwardClient;
        public long LastActivity { get; private set; } = Environment.TickCount64;
        private readonly IPEndPoint _sourceEndpoint;
        private readonly IPEndPoint _remoteEndpoint;
        private readonly EndPoint? _serverLocalEndpoint;
        private EndPoint? _forwardLocalEndpoint;
        private bool _isRunning;
        private long _totalBytesForwarded;
        private long _totalBytesResponded;
        private readonly TaskCompletionSource<bool> _forwardConnectionBindCompleted = new TaskCompletionSource<bool>();

        private int _serverId;
        private readonly string _userId;

        public UdpConnection(int serverId, string userId, UdpClient localServer, IPEndPoint sourceEndpoint, IPEndPoint remoteEndpoint)
        {
            _serverId = serverId;
            _userId = userId;
            _localServer = localServer;
            _serverLocalEndpoint = _localServer.Client.LocalEndPoint;

            _isRunning = true;
            _remoteEndpoint = remoteEndpoint;
            _sourceEndpoint = sourceEndpoint;

            _forwardClient = new UdpClient(AddressFamily.InterNetwork);
        }

        public async Task SendToServerAsync(byte[] message)
        {
            LastActivity = Environment.TickCount64;

            await _forwardConnectionBindCompleted.Task.ConfigureAwait(false);
            var sent = await _forwardClient.SendAsync(message, message.Length, _remoteEndpoint).ConfigureAwait(false);
            Interlocked.Add(ref _totalBytesForwarded, sent);
        }

        public void Run()
        {
            Task.Run(async () =>
            {
                using (_forwardClient)
                {
                    _forwardClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                    _forwardLocalEndpoint = _forwardClient.Client.LocalEndPoint;
                    _forwardConnectionBindCompleted.SetResult(true);
                    Console.WriteLine($"New connection {_sourceEndpoint.Address} ({_userId}) redirecting from {_serverLocalEndpoint} to {_remoteEndpoint}.");
                    while (_isRunning)
                    {
                        try
                        {
                            var result = await _forwardClient.ReceiveAsync().ConfigureAwait(false);
                            LastActivity = Environment.TickCount64;
                            var sent = await _localServer.SendAsync(result.Buffer, result.Buffer.Length, _sourceEndpoint).ConfigureAwait(false);
                            Interlocked.Add(ref _totalBytesResponded, sent);
                        }
                        catch (Exception ex)
                        {
                            if (_isRunning)
                            {
                                Console.WriteLine($"An exception occurred while receiving a server datagram : {ex}");
                            }
                        }
                    }
                }
            });
        }

        public void Stop()
        {
            try
            {
                Console.WriteLine($"Closed connection {_sourceEndpoint.Address} ({_userId}) connected to {_serverLocalEndpoint} while being redirected to {_remoteEndpoint}. ( Forwarded {_totalBytesForwarded} bytes, responded {_totalBytesResponded} bytes )");
                _isRunning = false;
                _forwardClient.Close();

                if (_userId != null && UdpProxy.Servers[_serverId].Players.ContainsKey(_userId))
                    UdpProxy.Servers[_serverId].Players.Remove(_userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred while closing UdpConnection : {ex}");
            }
        }
    }
}
