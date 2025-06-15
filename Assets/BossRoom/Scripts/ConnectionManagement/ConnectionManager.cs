using System;
using System.Collections.Generic;
using Unity.BossRoom.Utils;
using Unity.Netcode;
using UnityEngine;
using UUnity.BossRoom.ConnectionManagement;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
        Reconnecting,             //client lost connection and is attempting to reconnect.
        IncompatibleBuildType,    //client build type is incompatible with server.
        HostEndedSession,         //host intentionally ended the session.
        StartHostFailed,          // server failed to bind
        StartClientFailed         // failed to connect to server and/or invalid network endpoint
    }

    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }

    public struct ConnectionEventMessage : INetworkSerializeByMemcpy
    {
        public ConnectStatus ConnectStatus;
        public FixedPlayerName PlayerName;
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public string playerName;
        public bool isDebug;
    }

    /// <summary>
    /// This state machine handles connection through the NetworkManager. It is responsible for listening to
    /// NetworkManger callbacks and other outside calls and redirecting them to the current ConnectionState object.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        ConnectionState _mCurrentState;

        [Inject]
        NetworkManager _mNetworkManager;
        public NetworkManager NetworkManager => _mNetworkManager;

        [SerializeField]
        int m_NbReconnectAttempts = 2;

        public int NbReconnectAttempts => m_NbReconnectAttempts;

        [Inject]
        IObjectResolver _mResolver;

        public int MaxConnectedPlayers = 8;

        internal readonly OfflineState MOffline = new OfflineState();
        internal readonly ClientConnectingState MClientConnecting = new ClientConnectingState();
        internal readonly ClientConnectedState MClientConnected = new ClientConnectedState();
        internal readonly ClientReconnectingState MClientReconnecting = new ClientReconnectingState();
        internal readonly StartingHostState MStartingHost = new StartingHostState();
        internal readonly HostingState MHosting = new HostingState();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            List<ConnectionState> states = new() { MOffline, MClientConnecting, MClientConnected, MClientReconnecting, MStartingHost, MHosting };
            foreach (var connectionState in states)
            {
                _mResolver.Inject(connectionState);
            }

            _mCurrentState = MOffline;

            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.OnServerStarted += OnServerStarted;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;
        }

        void OnDestroy()
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.OnServerStopped -= OnServerStopped;
        }

        internal void ChangeState(ConnectionState nextState)
        {
            Debug.Log($"{name}: Changed connection state from {_mCurrentState.GetType().Name} to {nextState.GetType().Name}.");

            if (_mCurrentState != null)
            {
                _mCurrentState.Exit();
            }
            _mCurrentState = nextState;
            _mCurrentState.Enter();
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            _mCurrentState.OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            _mCurrentState.OnClientConnected(clientId);
        }

        void OnServerStarted()
        {
            _mCurrentState.OnServerStarted();
        }

        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            _mCurrentState.ApprovalCheck(request, response);
        }

        void OnTransportFailure()
        {
            _mCurrentState.OnTransportFailure();
        }

        void OnServerStopped(bool _) // we don't need this parameter as the ConnectionState already carries the relevant information
        {
            _mCurrentState.OnServerStopped();
        }

        // Note: MultiplayerSDK refactoring
        public void StartClientLobby(string sessionCode, string playerName)
        {
            _mCurrentState.StartClientLobby(sessionCode, playerName);
        }

        public void StartClientIp(string playerName, string ipaddress, int port)
        {
            _mCurrentState.StartClientIP(playerName, ipaddress, port);
        }

        // Note: MultiplayerSDK refactoring
        public void StartHostLobby(string sessionCode, string playerName)
        {
            _mCurrentState.StartHostLobby(sessionCode, playerName);
        }

        public void StartHostIp(string playerName, string ipaddress, int port)
        {
            _mCurrentState.StartHostIP(playerName, ipaddress, port);
        }

        public void RequestShutdown()
        {
            _mCurrentState.OnUserRequestedShutdown();
        }
    }
}
