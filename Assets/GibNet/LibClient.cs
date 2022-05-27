using System;
using System.Net;
using System.Net.Sockets;
using GibNet.Encryption;
using GibNet.Packets;
using GibNet.Packets.Authentication;
using GibNet.Packets.Processors;
using LiteNetLib;
using UnityEngine;

namespace GibNet
{
    public class LibClient : INetEventListener
    {

        private readonly NetManager _client;
        private readonly ClientPacketProcessor _processor;

        private bool _clientConnected;
        private bool _connectionComplete;
        
        // Events
        public event Action OnStartClient;
        public event Action OnClientConnect;
        public event Action OnClientDisconnect;
        public event Action OnStopClient;

        public LibClient()
        {
            _client = new NetManager(this);
            _processor = new ClientPacketProcessor();

            _clientConnected = false;
            _connectionComplete = false;
            
            NetworkClient.SetupPacketProcessor(_processor);
            
            // Setup Authentication Events
            NetworkClient.PacketReceived<ServerEncryptionRequest>((encryption) =>
            {
                NetworkDebug.ClientMessage($"Server Encryption Request");
                var response = new ClientEncryptionResponse()
                {
                    PublicKey = _processor.CreateClientEncryption().PublicKey
                };
                NetworkClient.Send(response, DeliveryMethod.ReliableOrdered);
            });
            
            NetworkClient.PacketReceived<ServerEncryptionResponse>((encryption) =>
            {
                NetworkDebug.ClientMessage($"Server Encryption Generation");
                _processor.SetupServerEncryption(new AesKey(encryption.Key, encryption.Iv));

                var response = new ClientEncryptionComplete();
                NetworkClient.Send(response, DeliveryMethod.ReliableOrdered);
            });
            
            NetworkClient.PacketReceived<ServerEncryptionComplete>((complete) =>
            {
                NetworkDebug.ClientMessage($"Server Encryption Complete");
                _connectionComplete = true;
                OnClientConnect?.Invoke();
            });
        }

        public void Connect()
        {
            _client.Start();
            _client.Connect("localhost", 9050, "");
            _clientConnected = true;
            
            OnStartClient?.Invoke();
        }

        public void Disconnect()
        {
            _client.DisconnectAll();
            
            OnStopClient?.Invoke();
        }

        public void Update()
        {
            if (_clientConnected)
                _client.PollEvents();
        }

        public void Destroy()
        {
            _client?.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            _processor.AssignServer(peer);
            NetworkDebug.ClientMessage($"Has connected to server {peer.EndPoint.Address}:{peer.EndPoint.Port}");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            NetworkDebug.ClientMessage($"Has disconnected from server {peer.EndPoint.Address}:{peer.EndPoint.Port} due to {disconnectInfo.Reason}");
            _clientConnected = false;
            
            if (_connectionComplete)
                OnClientDisconnect?.Invoke();
            
            _connectionComplete = false;
            
            _processor.Disconnect();
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
            request.Reject();
        }
    }
}