using GibNet.Packets.Interfaces;

namespace GibNet.Packets.Authentication
{
    public class ServerTickValue : IPacket
    {
        public uint ticks;
        public int ping;
    }
}