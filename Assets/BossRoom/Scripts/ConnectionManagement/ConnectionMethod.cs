using System.Threading.Tasks;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.BossRoom.Utils;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client side.
    /// Please override this abstract class to add a new transport or way of connecting.
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        protected ConnectionManager MConnectionManager;
        readonly ProfileManager _mProfileManager;
        protected readonly string MPlayerName;
        protected const string KDtlsConnType = "dtls";

        /// <summary>
        /// Setup the host connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupHostConnectionAsync();


        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupClientConnectionAsync();

        /// <summary>
        /// Setup the client for reconnection prior to reconnecting
        /// </summary>
        /// <returns>
        /// success = true if succeeded in setting up reconnection, false if failed.
        /// shouldTryAgain = true if we should try again after failing, false if not.
        /// </returns>
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        public ConnectionMethodBase(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
        {
            MConnectionManager = connectionManager;
            _mProfileManager = profileManager;
            MPlayerName = playerName;
        }

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            MConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        /// Using authentication, this makes sure your session is associated with your account and not your device. This means you could reconnect
        /// from a different device for example. A playerId is also a bit more permanent than player prefs. In a browser for example,
        /// player prefs can be cleared as easily as cookies.
        /// The forked flow here is for debug purposes and to make UGS optional in Boss Room. This way you can study the sample without
        /// setting up a UGS account. It's recommended to investigate your own initialization and IsSigned flows to see if you need
        /// those checks on your own and react accordingly. We offer here the option for offline access for debug purposes, but in your own game you
        /// might want to show an error popup and ask your player to connect to the internet.
        protected string GetPlayerId()
        {
            if (Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + _mProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + _mProfileManager.Profile;
        }
    }

    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    class ConnectionMethodIP : ConnectionMethodBase
    {
        string _mIpaddress;
        ushort _mPort;

        public ConnectionMethodIP(string ip, ushort port, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            _mIpaddress = ip;
            _mPort = port;
            MConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), MPlayerName);
            var utp = (UnityTransport)MConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(_mIpaddress, _mPort);
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            // Nothing to do here
            return (true, true);
        }

        public override async Task SetupHostConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), MPlayerName); // Need to set connection payload for host as well, as host is a client too
            var utp = (UnityTransport)MConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(_mIpaddress, _mPort);
        }
    }

    // Note: MultiplayerSDK refactoring
    /// <summary>
    /// UTP's Relay connection setup using the Lobby integration
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {
        LobbyServiceFacade _mLobbyServiceFacade;
        /*LocalLobby m_LocalLobby;*/

        string _mSessionName;
        bool _mIsPrivate = true;

        public ConnectionMethodRelay(string sessionName, LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            _mSessionName = sessionName;
            _mLobbyServiceFacade = lobbyServiceFacade;
            /*m_LocalLobby = localLobby;*/
            MConnectionManager = connectionManager;
        }

        // Note: MultiplayerSDK refactoring
        public override async Task SetupClientConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay client");

            // TODO: where to grab name
            SetConnectionPayload(GetPlayerId(), MPlayerName);

            // don't need this either?
            /*if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                throw new Exception("Trying to start relay while Lobby isn't set");
            }

            Debug.Log($"Setting Unity Relay client with join code {m_LocalLobby.RelayJoinCode}");*/

            // don't need to do, relay allocation handled by service
            /*// Create client joining allocation from join code
            var joinedAllocation = await RelayService.Instance.JoinAllocationAsync(m_LocalLobby.RelayJoinCode);
            Debug.Log($"client: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}, " +
                $"host: {joinedAllocation.HostConnectionData[0]} {joinedAllocation.HostConnectionData[1]}, " +
                $"client: {joinedAllocation.AllocationId}");*/

            /*await m_LobbyServiceFacade.UpdatePlayerDataAsync(joinedAllocation.AllocationId.ToString(), m_LocalLobby.RelayJoinCode);*/
            await _mLobbyServiceFacade.TryJoinLobbyAsync(_mSessionName/*, lobbyCode*/);
            
            // TODO: do we need this?
            // Configure UTP with allocation
            /*var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(joinedAllocation, k_DtlsConnType));*/
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            if (_mLobbyServiceFacade.CurrentUnityLobby == null)
            {
                Debug.Log("Lobby does not exist anymore, stopping reconnection attempts.");
                return (false, false);
            }

            // When using Lobby with Relay, if a user is disconnected from the Relay server, the server will notify the
            // Lobby service and mark the user as disconnected, but will not remove them from the lobby. They then have
            // some time to attempt to reconnect (defined by the "Disconnect removal time" parameter on the dashboard),
            // after which they will be removed from the lobby completely.
            // See https://docs.unity.com/lobby/reconnect-to-lobby.html
            var lobby = await _mLobbyServiceFacade.ReconnectToLobbyAsync();
            var success = lobby != null;
            Debug.Log(success ? "Successfully reconnected to Lobby." : "Failed to reconnect to Lobby.");
            return (success, true); // return a success if reconnecting to lobby returns a lobby
        }

        // Note: MultiplayerSDK refactoring
        public override async Task SetupHostConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay host");

            SetConnectionPayload(GetPlayerId(), MPlayerName); // Need to set connection payload for host as well, as host is a client too

            /*// Create relay allocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(m_ConnectionManager.MaxConnectedPlayers, region: null);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            Debug.Log($"server: connection data: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}, " +
                $"allocation ID:{hostAllocation.AllocationId}, region:{hostAllocation.Region}");

            m_LocalLobby.RelayJoinCode = joinCode;

            // next line enables lobby and relay services integration
            await m_LobbyServiceFacade.UpdateLobbyDataAndUnlockAsync();
            await m_LobbyServiceFacade.UpdatePlayerDataAsync(hostAllocation.AllocationIdBytes.ToString(), joinCode);*/

            // TODO: needed?
            /*// Setup UTP with relay connection info
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(hostAllocation, k_DtlsConnType)); // This is with DTLS enabled for a secure connection

            Debug.Log($"Created relay allocation with join code {m_LocalLobby.RelayJoinCode}");*/
            
            var lobbyCreationAttempt = await _mLobbyServiceFacade.TryCreateLobbyAsync(_mSessionName, MConnectionManager.MaxConnectedPlayers, _mIsPrivate);

            Debug.Log($"{lobbyCreationAttempt.Success} lobbyCreationAttempt.Lobby.Id: {lobbyCreationAttempt.Lobby.Id} lobbyCreationAttempt.Lobby.Code {lobbyCreationAttempt.Lobby.Code}");
        }
    }
}
