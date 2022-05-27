using System;
using GibNet.Encryption;
using GibNet.Packets.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GibNet.Packets.Processors
{
    public class ClientPacketProcessor : PacketProcessor
    {

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
        
        public void PacketReceived<T>(Action<T> onReceive) where T : class, new()
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
                            onReceive(packet);
                        break;
                    case PacketEncryptionType.AES when _serverAesKey != null:
                        if (GetAesPacketData(wrapper, _serverAesKey, out packet))
                            onReceive(packet);
                        break;
                    case PacketEncryptionType.RSA when _clientRsaKey != null:
                        if (GetRsaPacketData(wrapper, _clientRsaKey, out packet))
                            onReceive(packet);
                        break;
                    default:
                        NetworkDebug.ClientError("Invalid Packet Received... Disconnecting from Server");
                        _serverPeer.Disconnect();
                        return;
                }
            };
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