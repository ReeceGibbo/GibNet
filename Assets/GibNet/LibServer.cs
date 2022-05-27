using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GibNet.Encryption;
using GibNet.Packets;
using GibNet.Packets.Authentication;
using GibNet.Packets.Interfaces;
using GibNet.Packets.Processors;
using LiteNetLib;
using UnityEngine;

namespace GibNet
{
    public class LibServer : INetEventListener
    {
        private readonly NetManager _server;
        private readonly ServerPacketProcessor _processor;

        private bool _serverStarted;
        
        // Authentication
        private readonly Dictionary<int, bool> _serverConnectionStates;
        private const int AuthPeriodSeconds = 30;
        
        // Events
        public event Action OnStartServer;
        public event Action<NetPeer> OnServerConnect;
        public event Action<NetPeer> OnServerDisconnect;
        public event Action OnStopServer;
    
        public LibServer()
        {
            _server = new NetManager(this);
            _processor = new ServerPacketProcessor();

            _serverStarted = false;
            _serverConnectionStates = new Dictionary<int, bool>();
            
            NetworkServer.SetupPacketProcessor(_processor);
            
            // Setup Authentication Events
            NetworkServer.PacketReceived<ClientEncryptionResponse>((peer, encryption) =>
            {
                NetworkDebug.ServerMessageFromPeer(peer, $"Client Encryption Generation");
                var keyPair = new RsaKeyPair()
                {
                    PublicKey = encryption.PublicKey,
                    PrivateKey = ""
                };
                
                var serverEncryption = _processor.AddClientEncryption(peer, keyPair);

                var response = new ServerEncryptionResponse()
                {
                    Key = serverEncryption.GetKey(),
                    Iv = serverEncryption.GetIv()
                };
                NetworkServer.Send(peer, response, DeliveryMethod.ReliableOrdered);
            });
            
            NetworkServer.PacketReceived<ClientEncryptionComplete>((peer, complete) =>
            {
                NetworkDebug.ServerMessageFromPeer(peer, $"Client Encryption Complete");
                var response = new ServerEncryptionComplete();
                NetworkServer.Send(peer, response, DeliveryMethod.ReliableOrdered);

                _serverConnectionStates[peer.Id] = true;
                
                OnServerConnect?.Invoke(peer);
            });
        }

        public void StartServer()
        {
            _server.Start(9050);
            _serverStarted = true;
            
            OnStartServer?.Invoke();
        }

        public void StopServer()
        {
            _server.Stop();
            _serverConnectionStates.Clear();
            
            _processor.Disconnect();
            
            OnStopServer?.Invoke();
        }
    
        public void Update()
        {
            if (_serverStarted)
                _server.PollEvents();
        }
        
        public void Destroy()
        {
            _server?.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NetworkDebug.ServerMessageFromPeer(peer, "Has connected to the server");
            _serverConnectionStates.Add(peer.Id, false);

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(AuthPeriodSeconds * 1000);
                CheckConnectionState(peer);
            });

            // Send Encryption Request Packet
            var requestEncryption = new ServerEncryptionRequest();
            NetworkServer.Send(peer, requestEncryption, DeliveryMethod.ReliableOrdered);
            NetworkDebug.ServerMessageFromPeer(peer, $"Client Encryption Request");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            NetworkDebug.ServerMessageFromPeer(peer, "Has disconnected from the server");

            if (_serverConnectionStates[peer.Id])
            {
                OnServerDisconnect?.Invoke(peer);
            }
            
            _serverConnectionStates.Remove(peer.Id);

            _processor.RemoveClientEncryption(peer);
        }
    
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            _processor.ReadAllPackets(peer, reader);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }
        
        private void CheckConnectionState(NetPeer peer)
        {
            if (_serverConnectionStates.TryGetValue(peer.Id, out var value))
            {
                if (!value)
                {
                    peer.Disconnect();
                }
            }
        }
    }
}