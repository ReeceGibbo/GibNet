using LiteNetLib;
using UnityEngine;

namespace GibNet
{
    public static class NetworkDebug
    {
        public static void ServerError(string text)
        {
            Debug.Log($"<color=#{(byte)(255f):X2}{(byte)(0f):X2}{(byte)(0f):X2}>[SERVER] {text}</color>");
        }

        public static void ServerErrorFromPeer(NetPeer peer, string text)
        {
            Debug.Log($"<color=#{(byte)(255f):X2}{(byte)(0f):X2}{(byte)(0f):X2}>[SERVER] {peer.EndPoint.Address}:{peer.EndPoint.Port} - {text}</color>");
        }

        public static void ServerMessageFromPeer(NetPeer peer, string text)
        {
            Debug.Log($"<color=#{(byte)(255f):X2}{(byte)(255f):X2}{(byte)(255f):X2}>[SERVER] {peer.EndPoint.Address}:{peer.EndPoint.Port} - {text}</color>");
        }

        public static void ClientError(string text)
        {
            Debug.Log($"<color=#{(byte)(255f):X2}{(byte)(0f):X2}{(byte)(0f):X2}>[CLIENT] {text}</color>");
        }
        
        public static void ClientMessage(string text)
        {
            Debug.Log($"<color=#{(byte)(255f):X2}{(byte)(255f):X2}{(byte)(255f):X2}>[CLIENT] {text}</color>");
        }
    }
}