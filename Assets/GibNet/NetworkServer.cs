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
        
        public static void SendToAll<T>(T packet, DeliveryMethod options) where T : class, new()
        {
            _processor.SendToAll<T>(packet, options);
        }
        
        public static void SendToAllExcluded<T>(NetPeer excluded, T packet, DeliveryMethod options) where T : class, new()
        {
            _processor.SendToAllExcluded<T>(excluded, packet, options);
        }
        
        public static uint GetTicks()
        {
            return LibServer.ServerTicks;
        }
        
    }
}