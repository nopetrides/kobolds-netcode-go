using System;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a listening host. Handles incoming client connections. When shutting down or
    /// being timed out, transitions to the Offline state.
    /// </summary>
    class HostingState : OnlineState
    {
        [Inject]
        LobbyServiceFacade _mLobbyServiceFacade;
        [Inject]
        IPublisher<ConnectionEventMessage> _mConnectionEventPublisher;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        const int KMaxConnectPayload = 1024;

        public override void Enter()
        {
            //The "BossRoom" server always advances to CharSelect immediately on start. Different games
            //may do this differently.
            SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);

            if (_mLobbyServiceFacade.CurrentUnityLobby != null)
            {
                _mLobbyServiceFacade.BeginTracking();
            }
        }

        public override void Exit()
        {
            SessionManager<SessionPlayerData>.Instance.OnServerEnded();
        }

        public override void OnClientConnected(ulong clientId)
        {
            var playerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            if (playerData != null)
            {
                _mConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.Success, PlayerName = playerData.Value.PlayerName });
            }
            else
            {
                // This should not happen since player data is assigned during connection approval
                Debug.LogError($"No player data associated with client {clientId}");
                var reason = JsonUtility.ToJson(ConnectStatus.GenericDisconnect);
                MConnectionManager.NetworkManager.DisconnectClient(clientId, reason);
            }

        }

        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId != MConnectionManager.NetworkManager.LocalClientId)
            {
                var playerId = SessionManager<SessionPlayerData>.Instance.GetPlayerId(clientId);
                if (playerId != null)
                {
                    var sessionData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(playerId);
                    if (sessionData.HasValue)
                    {
                        _mConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.GenericDisconnect, PlayerName = sessionData.Value.PlayerName });
                    }
                    SessionManager<SessionPlayerData>.Instance.DisconnectClient(clientId);
                }
            }
        }

        public override void OnUserRequestedShutdown()
        {
            var reason = JsonUtility.ToJson(ConnectStatus.HostEndedSession);
            for (var i = MConnectionManager.NetworkManager.ConnectedClientsIds.Count - 1; i >= 0; i--)
            {
                var id = MConnectionManager.NetworkManager.ConnectedClientsIds[i];
                if (id != MConnectionManager.NetworkManager.LocalClientId)
                {
                    MConnectionManager.NetworkManager.DisconnectClient(id, reason);
                }
            }
            MConnectionManager.ChangeState(MConnectionManager.MOffline);
        }

        public override void OnServerStopped()
        {
            MConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
            MConnectionManager.ChangeState(MConnectionManager.MOffline);
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalResponse" exposed by Netcode.NetworkManager. It is run every time a client connects to us.
        /// The complementary logic that runs when the client starts its connection can be found in ClientConnectingState.
        /// </summary>
        /// <remarks>
        /// Multiple things can be done here, some asynchronously. For example, it could authenticate your user against an auth service like UGS' auth service. It can
        /// also send custom messages to connecting users before they receive their connection result (this is useful to set status messages client side
        /// when connection is refused, for example).
        /// Note on authentication: It's usually harder to justify having authentication in a client hosted game's connection approval. Since the host can't be trusted,
        /// clients shouldn't send it private authentication tokens you'd usually send to a dedicated server.
        /// </remarks>
        /// <param name="request"> The initial request contains, among other things, binary data passed into StartClient. In our case, this is the client's GUID,
        /// which is a unique identifier for their install of the game that persists across app restarts.
        ///  <param name="response"> Our response to the approval process. In case of connection refusal with custom return message, we delay using the Pending field.
        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            if (connectionData.Length > KMaxConnectPayload)
            {
                // If connectionData too high, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                response.Approved = false;
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            var gameReturnStatus = GetConnectStatus(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, new NetworkGuid(), 0, true));

                // connection approval will create a player object for you
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Position = Vector3.zero;
                response.Rotation = Quaternion.identity;
                return;
            }

            response.Approved = false;
            response.Reason = JsonUtility.ToJson(gameReturnStatus);
            if (_mLobbyServiceFacade.CurrentUnityLobby != null)
            {
                _mLobbyServiceFacade.RemovePlayerFromLobbyAsync(connectionPayload.playerId);
            }
        }

        ConnectStatus GetConnectStatus(ConnectionPayload connectionPayload)
        {
            if (MConnectionManager.NetworkManager.ConnectedClientsIds.Count >= MConnectionManager.MaxConnectedPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.isDebug != Debug.isDebugBuild)
            {
                return ConnectStatus.IncompatibleBuildType;
            }

            return SessionManager<SessionPlayerData>.Instance.IsDuplicateConnection(connectionPayload.playerId) ?
                ConnectStatus.LoggedInAgain : ConnectStatus.Success;
        }
    }
}
