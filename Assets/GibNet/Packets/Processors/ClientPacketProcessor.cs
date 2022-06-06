using System;
using System.Collections.Generic;
using GibNet.Encryption;
using GibNet.Packets.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEditor.VersionControl;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace GibNet.Packets.Processors
{
    public class ClientPacketProcessor : PacketProcessor
    {
        
        private readonly Dictionary<Type, object> _packetActions = new();

        private NetPeer _serverPeer;
        
        private AesKey _serverAesKey;
        private RsaKeyPair _clientRsaKey;

        public void AssignServer(NetPeer server)
        {
            _serverPeer = server;
        }
        
        public RsaKeyPair CreateClientEncryption()
        {
            return _clientRsaKey = EncryptionHelper.CreateRsaEncryption();
        }

        public void SetupServerEncryption(AesKey key)
        {
            _serverAesKey = key;
        }

        public void Disconnect()
        {
            _serverPeer = null;
            _serverAesKey = null;
            _clientRsaKey = null;
        }

        private void Add<T>(Action<T> foo) where T : class, new()
        {
            if (!_packetActions.TryGetValue(typeof(T), out var tmp))
            {
                tmp = new List<Action<T>>();
                _packetActions[typeof(T)] = tmp;
            }

            var list = (List<Action<T>>)tmp;
            list.Add(foo);
        }

        private void Invoke<T>(T packet) where T : class, new()
        {
            if (_packetActions.TryGetValue(typeof(T), out var tmp))
            {
                var list = (List<Action<T>>)tmp;
                foreach (var action in list)
                {
                    action(packet);
                }
            }
        }
        
        public void PacketReceived<T>(Action<T> onReceive) where T : class, new()
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
                                Invoke(packet);
                            break;
                        case PacketEncryptionType.AES when _serverAesKey != null:
                            if (GetAesPacketData(wrapper, _serverAesKey, out packet))
                                Invoke(packet);
                            break;
                        case PacketEncryptionType.RSA when _clientRsaKey != null:
                            if (GetRsaPacketData(wrapper, _clientRsaKey, out packet))
                                Invoke(packet);
                            break;
                        default:
                            NetworkDebug.ClientError("Invalid Packet Received... Disconnecting from Server");
                            _serverPeer.Disconnect();
                            return;
                    }
                };
            }
        }
        
        public void Send<T>(T packet, DeliveryMethod options) where T : class, new()
        {
            NetDataWriter.Reset();

            switch (packet)
            {
                case IAuthenticationPacket:
                    WriteNoEncryption(NetDataWriter, packet);
                    break;
                case IAsymmetricPacket:
                    WriteRsaEncrypted(NetDataWriter, packet, _clientRsaKey);
                    break;
                case IPacket:
                    WriteAesEncrypted(NetDataWriter, packet, _serverAesKey);
                    break;
                default:
                    NetworkDebug.ClientError("We tried to send an invalid packet... Disconnecting from Server");
                    _serverPeer.Disconnect();
                    return;
            }
        
            _serverPeer.Send(NetDataWriter, options);
        }

    }
}