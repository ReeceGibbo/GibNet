using System.IO;

namespace GibNet.Packets
{
    public struct PacketWrapper
    {
        public string Data { get; set; }
        public PacketEncryptionType EncryptionType { get; set; }

        public byte[] Serialize()
        {
            using var m = new MemoryStream();
            using (var writer = new BinaryWriter(m)) {
                writer.Write(Data);
                writer.Write((byte) EncryptionType);
            }
            return m.ToArray();
        }

        public static PacketWrapper Deserialize(byte[] data) {
            var result = new PacketWrapper();
            using var m = new MemoryStream(data);
            using var reader = new BinaryReader(m);
            result.Data = reader.ReadString();
            result.EncryptionType = (PacketEncryptionType)reader.ReadByte();

            return result;
        }
    }
}