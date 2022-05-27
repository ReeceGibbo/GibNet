using System;
using System.Collections;
using System.Collections.Generic;
using GibNet.Encryption;
using GibNet.Packets;
using GibNet.Packets.Authentication;
using LiteNetLib;
using UnityEngine;

namespace GibNet
{
    public class NetworkManager : MonoBehaviour
    {
        private LibClient _libClient;
        private LibServer _libServer;
        
        public void Awake()
        {
            _libClient = new LibClient();
            _libServer = new LibServer();
            
            _libClient.OnStartClient += OnStartClient;
            _libClient.OnClientConnect += OnClientConnect;
            _libClient.OnClientDisconnect += OnClientDisconnect;
            _libClient.OnStopClient += OnStopClient;
            
            _libServer.OnStartServer += OnStartServer;
            _libServer.OnServerConnect += OnServerConnect;
            _libServer.OnServerDisconnect += OnServerDisconnect;
            _libServer.OnStopServer += OnStopServer;
        }

        public void Start()
        {
            _libServer.StartServer();
            _libClient.Connect();
        }

        public void FixedUpdate()
        {
            _libClient.Update();
            _libServer.Update();
        }

        public void OnDestroy()
        {
            _libClient.Destroy();
            _libServer.Destroy();
        }

        protected virtual void OnStartServer() { }
        protected virtual void OnServerConnect(NetPeer peer) { }
        protected virtual void OnServerDisconnect(NetPeer peer) { }
        protected virtual void OnStopServer() { }
        protected virtual void OnStartClient() { }
        protected virtual void OnClientConnect() { }
        protected virtual void OnClientDisconnect() { }
        protected virtual void OnStopClient() { }
    }
}