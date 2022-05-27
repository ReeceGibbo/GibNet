using System;
using GibNet.Encryption;
using GibNet.Packets;
using GibNet.Packets.Interfaces;
using GibNet.Packets.Processors;
using LiteNetLib;
using LiteNetLib.Utils;

namespace GibNet
{
    public static class NetworkClient
    {
        private static ClientPacketProcessor _processor;

        public static void SetupPacketProcessor(ClientPacketProcessor processor)
        {
            _processor = processor;
        }

        public static void PacketReceived<T>(Action<T> onReceive) where T : class, new()
        {
            _processor.PacketReceived<T>(onReceive);
        }
        
        public static void Send<T>(T packet, DeliveryMethod options) where T : class, new()
        {
            _processor.Send<T>(packet, options);
        }

    }
}