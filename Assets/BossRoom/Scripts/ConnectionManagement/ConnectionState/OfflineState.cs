using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.BossRoom.Utils;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace UUnity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. From this state we can transition to the
    /// ClientConnecting sate, if starting as a client, or the StartingHost state, if starting as a host.
    /// </summary>
    class OfflineState : ConnectionState
    {
        [Inject]
        LobbyServiceFacade _mLobbyServiceFacade;
        [Inject]
        ProfileManager _mProfileManager;
        [Inject]
        LocalLobby _mLocalLobby;

        const string KMainMenuSceneName = "MainMenu";

        public override void Enter()
        {
            _mLobbyServiceFacade.EndTracking();
            MConnectionManager.NetworkManager.Shutdown();
            if (SceneManager.GetActiveScene().name != KMainMenuSceneName)
            {
                SceneLoaderWrapper.Instance.LoadScene(KMainMenuSceneName, useNetworkSceneManager: false);
            }
        }

        public override void Exit() { }

        public override void StartClientIP(string playerName, string ipaddress, int port)
        {
            var connectionMethod = new ConnectionMethodIP(ipaddress, (ushort)port, MConnectionManager, _mProfileManager, playerName);
            MConnectionManager.MClientReconnecting.Configure(connectionMethod);
            MConnectionManager.ChangeState(MConnectionManager.MClientConnecting.Configure(connectionMethod));
        }

        // Note: MultiplayerSDK refactoring
        public override void StartClientLobby(string sessionCode, string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(sessionCode, _mLobbyServiceFacade, _mLocalLobby, MConnectionManager, _mProfileManager, playerName);
            MConnectionManager.MClientReconnecting.Configure(connectionMethod);
            MConnectionManager.ChangeState(MConnectionManager.MClientConnecting.Configure(connectionMethod));
        }

        public override void StartHostIP(string playerName, string ipaddress, int port)
        {
            var connectionMethod = new ConnectionMethodIP(ipaddress, (ushort)port, MConnectionManager, _mProfileManager, playerName);
            MConnectionManager.ChangeState(MConnectionManager.MStartingHost.Configure(connectionMethod));
        }

        // Note: MultiplayerSDK refactoring
        public override void StartHostLobby(string sessionCode, string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(sessionCode, _mLobbyServiceFacade, _mLocalLobby, MConnectionManager, _mProfileManager, playerName);
            MConnectionManager.ChangeState(MConnectionManager.MStartingHost.Configure(connectionMethod));
        }
    }
}
