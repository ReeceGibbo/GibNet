using System;
using System.Collections.Generic;
using System.Linq;
using GibNet.Encryption;
using GibNet.Packets.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GibNet.Packets.Processors
{
    public class ServerPacketProcessor : PacketProcessor
    {
        
        private readonly Dictionary<Type, object> _packetActions = new();

        private readonly LibServer _libServer;
        
        private readonly Dictionary<int, AesKey> _clientAesKeys;
        private readonly Dictionary<int, RsaKeyPair> _clientRsaKeys;

        public ServerPacketProcessor(LibServer libServer)
        {
            _libServer = libServer;
            
            _clientAesKeys = new Dictionary<int, AesKey>();
            _clientRsaKeys = new Dictionary<int, RsaKeyPair>();
        }

        public AesKey AddClientEncryption(NetPeer peer, RsaKeyPair keyPair)
        {
            _clientRsaKeys.Add(peer.Id, keyPair);
            
            var key = EncryptionHelper.CreateAesEncryption();
            _clientAesKeys.Add(peer.Id, key);
            return key;
        }

        public void RemoveClientEncryption(NetPeer peer)
        {
            _clientRsaKeys.Remove(peer.Id);
            _clientAesKeys.Remove(peer.Id);
        }

        public void Disconnect()
        {
            _clientRsaKeys.Clear();
            _clientAesKeys.Clear();
        }
        
        private void Add<T>(Action<NetPeer, T> foo) where T : class, new()
        {
            if (!_packetActions.TryGetValue(typeof(T), out var tmp))
            {
                tmp = new List<Action<NetPeer, T>>();
                _packetActions[typeof(T)] = tmp;
            }

            var list = (List<Action<NetPeer, T>>)tmp;
            list.Add(foo);
        }

        private void Invoke<T>(NetPeer peer, T packet) where T : class, new()
        {
            if (_packetActions.TryGetValue(typeof(T), out var tmp))
            {
                var list = (List<Action<NetPeer, T>>)tmp;
                foreach (var action in list)
                {
                    action(peer, packet);
                }
            }
        }

        public void PacketReceived<T>(Action<NetPeer, T> onReceive) where T : class, new()
        {
            Add(onReceive);
            
            if (!Callbacks.ContainsKey(GetHash<T>()))
            {
                Callbacks[GetHash<T>()] = (peer, reader) =>
                {
                    var byteSize = reader.GetUShort();

                    var data = new byte[byteSize];
                    reader.GetBytes(data, byteSize);

                    var wrapper = PacketWrapper.Deserialize(data);

                    T packet;

                    switch (wrapper.EncryptionType)
                    {
                        case PacketEncryptionType.NONE:
                            if (GetPacketData(wrapper, out packet))
                                Invoke(peer, packet);
                            break;
                        case PacketEncryptionType.AES:
                            if (!_clientAesKeys.ContainsKey(peer.Id))
                            {
                                NetworkDebug.ServerErrorFromPeer(peer, "AES Key is NULL... Disconnecting Client");
                                peer.Disconnect();
                                return;
                            }

                            if (GetAesPacketData(wrapper, _clientAesKeys[peer.Id], out packet))
                                Invoke(peer, packet);
                            break;
                        case PacketEncryptionType.RSA:
                            if (!_clientRsaKeys.ContainsKey(peer.Id))
                            {
                                NetworkDebug.ServerErrorFromPeer(peer, "RSA Key is NULL... Disconnecting Client");
                                peer.Disconnect();
                                return;
                            }

                            if (GetRsaPacketData(wrapper, _clientRsaKeys[peer.Id], out packet))
                                Invoke(peer, packet);
                            break;
                        default:
                            NetworkDebug.ServerErrorFromPeer(peer, "Invalid Packet Received... Disconnecting Client");
                            peer.Disconnect();
                            return;
                    }
                };
            }
        }
        
        public void Send<T>(NetPeer peer, T packet, DeliveryMethod options) where T : class, new()
        {
            NetDataWriter.Reset();
            
            switch (packet)
            {
                case IAuthenticationPacket:
                    WriteNoEncryption(NetDataWriter, packet);
                    break;
                case IAsymmetricPacket when _clientRsaKeys.TryGetValue(peer.Id, out var keyPair):
                    WriteRsaEncrypted(NetDataWriter, packet, keyPair);
                    break;
                case IPacket when _clientAesKeys.TryGetValue(peer.Id, out var key):
                    WriteAesEncrypted(NetDataWriter, packet, key);
                    break;
                default:
                    NetworkDebug.ServerErrorFromPeer(peer, "Encryption either not setup or we tried to send invalid packet... Disconnecting Peer");
                    peer.Disconnect();
                    return;
            }
            
            peer.Send(NetDataWriter, options);
        }

        public void SendToAll<T>(T packet, DeliveryMethod options) where T : class, new()
        {
            foreach (var peer in _libServer.GetAuthenticatedPeers())
            {
                Send(peer, packet, options);
            }
        }
        
        public void SendToAllExcluded<T>(NetPeer excluded, T packet, DeliveryMethod options) where T : class, new()
        {
            foreach (var peer in _libServer.GetAuthenticatedPeers().Where(peer => peer.Id != excluded.Id))
            {
                Send(peer, packet, options);
            }
        }
        
    }
}