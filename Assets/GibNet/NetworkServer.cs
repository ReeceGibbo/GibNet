using System;
using System.Collections.Generic;
using GibNet.Encryption;
using GibNet.Packets;
using GibNet.Packets.Processors;
using LiteNetLib;
using LiteNetLib.Utils;

namespace GibNet
{
    public static class NetworkServer
    {
        private static ServerPacketProcessor _processor;
        
        public static void SetupPacketProcessor(ServerPacketProcessor processor)
        {
            _processor = processor;
        }

        public static void PacketReceived<T>(Action<NetPeer, T> onReceive) where T : class, new()
        {
            _processor.PacketReceived<T>(onReceive);
        }
        
        public static void Send<T>(NetPeer peer, T packet, DeliveryMethod options) where T : class, new()
        {
            _processor.Send<T>(peer, packet, options);
        }
        
    }
}