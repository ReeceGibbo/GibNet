using System;
using System.Collections.Generic;
using GibNet.Encryption;
using GibNet.Packets.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;

namespace GibNet.Packets.Processors
{
    public class PacketProcessor
    {
    
        protected readonly NetDataWriter NetDataWriter;

        protected delegate void PacketReceivedDelegate(NetPeer peer, NetDataReader reader);
        protected readonly Dictionary<ulong, PacketReceivedDelegate> Callbacks;

        protected PacketProcessor()
        {
            NetDataWriter = new NetDataWriter();
            Callbacks = new Dictionary<ulong, PacketReceivedDelegate>();
        }
        
        public void ReadAllPackets(NetPeer peer, NetDataReader reader)
        {
            while (reader.AvailableBytes > 0)
                ReadPacket(peer, reader);
        }
    
        private void ReadPacket(NetPeer peer, NetDataReader reader)
        {
            GetCallbackFromData(reader)(peer, reader);
        }
    
        private PacketReceivedDelegate GetCallbackFromData(NetDataReader reader)
        {
            var hash = reader.GetULong();
            if (!Callbacks.TryGetValue(hash, out var action))
            {
                throw new ParseException("Undefined packet in NetDataReader");
            }
            return action;
        }

        protected bool GetPacketData<T>(PacketWrapper wrapper, out T packet) where T : class, new()
        {
            return JsonHelper.DeserializeData<T>(wrapper.Data, out packet);
        }

        protected bool GetAesPacketData<T>(PacketWrapper wrapper, AesKey key, out T packet) where T : class, new()
        {
            if (EncryptionHelper.DecryptAesData(wrapper.Data, key, out var data))
                if (JsonHelper.DeserializeData<T>(data, out packet))
                    return true;

            packet = null;
            return false;
        }

        protected bool GetRsaPacketData<T>(PacketWrapper wrapper, RsaKeyPair keyPair, out T packet) where T : class, new()
        {
            if (EncryptionHelper.DecryptRsaData(wrapper.Data, keyPair, out var data))
                if (JsonHelper.DeserializeData<T>(data, out packet))
                    return true;

            packet = null;
            return false;
        }

        protected void WriteNoEncryption<T>(NetDataWriter writer, T packet) where T : class, new()
        {
            if (!JsonHelper.SerializeData(packet, out var data)) return;

            var wrapper = new PacketWrapper();

            if (packet is IAuthenticationPacket)
            {
                wrapper.Data = data;
                wrapper.EncryptionType = PacketEncryptionType.NONE;
            }
            else
            {
                throw new ParseException("Attempted to write packet that is not IAuthenticationPacket");
            }

            Write<T>(writer, wrapper);
        }

        protected void WriteAesEncrypted<T>(NetDataWriter writer, T packet, AesKey key) where T : class, new()
        {
            if (!JsonHelper.SerializeData(packet, out var data)) return;

            var wrapper = new PacketWrapper();

            if (packet is IPacket)
            {
                if (EncryptionHelper.EncryptAesData(data, key, out var encrypted))
                {
                    wrapper.Data = encrypted;
                    wrapper.EncryptionType = PacketEncryptionType.AES;
                }
                else
                {
                    throw new Exception("Error encrypting AES data");
                }
            }
            else
            {
                throw new ParseException("Attempted to write packet that is not IPacket");
            }

            Write<T>(writer, wrapper);
        }
        
        protected void WriteRsaEncrypted<T>(NetDataWriter writer, T packet, RsaKeyPair keyPair) where T : class, new()
        {
            if (!JsonHelper.SerializeData(packet, out var data)) return;

            var wrapper = new PacketWrapper();

            if (packet is IAsymmetricPacket)
            {
                if (EncryptionHelper.EncryptRsaData(data, keyPair, out var encrypted))
                {
                    wrapper.Data = encrypted;
                    wrapper.EncryptionType = PacketEncryptionType.RSA;
                }
                else
                {
                    throw new Exception("Error encrypting AES data");
                }
            }
            else
            {
                throw new ParseException("Attempted to write packet that is not IAsymmetricPacket");
            }

            Write<T>(writer, wrapper);
        }
        
        private void Write<T>(NetDataWriter writer, PacketWrapper wrapper)
        {
            if (wrapper.Data != null)
            {
                var serialized = wrapper.Serialize();

                writer.Put(GetHash<T>());
                writer.Put((ushort)serialized.Length);
                writer.Put(serialized);
            }
        }

        protected static ulong GetHash<T>()
        {
            ulong hash = 14695981039346656037UL; //offset
            string typeName = typeof(T).ToString();
            for (var i = 0; i < typeName.Length; i++)
            {
                hash ^= typeName[i];
                hash *= 1099511628211UL; //prime
            }
            return hash;
        }
    }
}