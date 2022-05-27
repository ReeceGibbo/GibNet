using GibNet.Packets.Interfaces;

namespace GibNet.Packets.Authentication
{
    public class ClientEncryptionResponse : IAuthenticationPacket
    {
        public string PublicKey { get; set; }
    }
}