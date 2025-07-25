using System;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Base class representing a connection state.
    /// </summary>
    abstract class ConnectionState
    {
        [Inject]
        protected ConnectionManager MConnectionManager;

        [Inject]
        protected IPublisher<ConnectStatus> MConnectStatusPublisher;

        public abstract void Enter();

        public abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) { }
        public virtual void OnClientDisconnect(ulong clientId) { }

        public virtual void OnServerStarted() { }

        public virtual void StartClientIP(string playerName, string ipaddress, int port) { }
        
        // Note: MultiplayerSDK refactoring
        public virtual void StartClientLobby(string sessionCode, string playerName) { }

        public virtual void StartHostIP(string playerName, string ipaddress, int port) { }

        // Note: MultiplayerSDK refactoring
        public virtual void StartHostLobby(string sessionCode, string playerName) { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }

        public virtual void OnTransportFailure() { }

        public virtual void OnServerStopped() { }
    }
}
