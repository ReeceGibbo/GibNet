using System;

namespace GibNet.Encryption
{
    public class AesKey
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesKey(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }
        
        public AesKey(string key, string iv)
        {
            _key = Convert.FromBase64String(key);
            _iv = Convert.FromBase64String(iv);
        }

        public byte[] GetKeyBytes()
        {
            return _key;
        }

        public byte[] GetIvBytes()
        {
            return _iv;
        }
        
        public string GetKey()
        {
            return Convert.ToBase64String(_key);
        }

        public string GetIv()
        {
            return Convert.ToBase64String(_iv);
        }
    }
}