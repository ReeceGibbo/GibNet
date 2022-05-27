using GibNet.Packets.Interfaces;

namespace GibNet.Packets.Authentication
{
    public class ServerEncryptionResponse : IAsymmetricPacket
    {
        public string Key { get; set; }
        public string Iv { get; set; }
    }
}