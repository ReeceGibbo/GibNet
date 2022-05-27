using System;
using Newtonsoft.Json;
using UnityEngine;

namespace GibNet.Packets
{
    public static class JsonHelper
    {
        public static bool SerializeData<T>(T packet, out string data) where T : class, new()
        {
            data = string.Empty;

            try
            {
                data = JsonConvert.SerializeObject(packet);
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Error serializing class {packet.GetType()}");
                return false;
            }
        }

        public static bool DeserializeData<T>(string data, out T packet) where T : class, new()
        {
            try
            {
                var deserialized = JsonConvert.DeserializeObject<T>(data);

                if (deserialized != null)
                {
                    packet = deserialized;
                    return true;
                }

                packet = new T();
                return false;
            }
            catch (Exception e)
            {
                packet = new T();
                Debug.Log($"Error deserializing packet");
                return false;
            }
        }
    }
}