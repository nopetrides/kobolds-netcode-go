using Unity.BossRoom.UnityServices.Lobbies;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the
    /// ClientReconnecting state if no reason is given, or to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        [Inject]
        protected LobbyServiceFacade MLobbyServiceFacade;

        public override void Enter()
        {
            if (MLobbyServiceFacade.CurrentUnityLobby != null)
            {
                MLobbyServiceFacade.BeginTracking();
            }
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            var disconnectReason = MConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason) ||
                disconnectReason == "Disconnected due to host shutting down.")
            {
                MConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                MConnectionManager.ChangeState(MConnectionManager.MClientReconnecting);
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                MConnectStatusPublisher.Publish(connectStatus);
                MConnectionManager.ChangeState(MConnectionManager.MOffline);
            }
        }
    }
}
