using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GibNet.Packets.Authentication;
using Newtonsoft.Json;
using UnityEngine;

namespace GibNet.Encryption
{
    public static class EncryptionHelper
    {

        public static AesKey CreateAesEncryption()
        {
            using var aes = Aes.Create();
            
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
                
            aes.GenerateIV();
            aes.GenerateKey();

            return new AesKey(aes.Key, aes.IV);
        }

        public static RsaKeyPair CreateRsaEncryption()
        {
            using var rsa = RSA.Create(4096);

            return new RsaKeyPair()
            {
                PublicKey = rsa.ToXmlString(false),
                PrivateKey = rsa.ToXmlString(true)
            };
        }
        
        public static bool EncryptAesData(string data, AesKey key, out string encrypted)
        {
            try
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);

                using var cypher = new AesManaged();
                using var cypherKey = cypher.CreateEncryptor(key.GetKeyBytes(), key.GetIvBytes());
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, cypherKey, CryptoStreamMode.Write);
                using var streamWriter = new StreamWriter(cryptoStream);
                cryptoStream.Write(dataBytes, 0, dataBytes.Length);
                cryptoStream.FlushFinalBlock();

                encrypted = Convert.ToBase64String(memoryStream.ToArray());
                return true;
            }
            catch (Exception e)
            {
                encrypted = "";
                return false;
            }
        }

        public static bool DecryptAesData(string data, AesKey key, out string decrypted)
        {
            try
            {
                var encodedBytes = Convert.FromBase64String(data);

                using var cypher = new AesManaged();
                using var cypherKey = cypher.CreateDecryptor(key.GetKeyBytes(), key.GetIvBytes());
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, cypherKey, CryptoStreamMode.Write);
                cryptoStream.Write(encodedBytes, 0, encodedBytes.Length);
                cryptoStream.FlushFinalBlock();

                decrypted = Encoding.UTF8.GetString(memoryStream.ToArray());
                return true;
            }
            catch (Exception e)
            {
                decrypted = "";
                return false;
            }
        }

        public static bool EncryptRsaData(string data, RsaKeyPair keyPair, out string encryptedData)
        {
            try
            {
                using var rsa = new RSACryptoServiceProvider(4096);
                rsa.FromXmlString(keyPair.PublicKey);

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var encrypted = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);

                encryptedData = Convert.ToBase64String(encrypted);
                return true;
            }
            catch (Exception e)
            {
                encryptedData = "";
                return false;
            }
        }

        public static bool DecryptRsaData(string data, RsaKeyPair keyPair, out string decryptedData)
        {
            try
            {
                using var rsa = new RSACryptoServiceProvider(4096);
                rsa.FromXmlString(keyPair.PrivateKey);

                var encodedBytes = Convert.FromBase64String(data);
                var decrypted = rsa.Decrypt(encodedBytes, RSAEncryptionPadding.Pkcs1);

                decryptedData = Encoding.UTF8.GetString(decrypted);
                return true;
            }
            catch (Exception e)
            {
                decryptedData = "";
                return false;
            }
        }
        
    }
}