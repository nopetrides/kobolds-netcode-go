using Unity.BossRoom.Gameplay.Configuration;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    // Note: MultiplayerSDK refactoring
    public class LobbyUIMediator : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] LobbyJoiningUI m_LobbyJoiningUI;
        [SerializeField] LobbyCreationUI m_LobbyCreationUI;
        [SerializeField] UITinter m_JoinToggleHighlight;
        [SerializeField] UITinter m_JoinToggleTabBlocker;
        [SerializeField] UITinter m_CreateToggleHighlight;
        [SerializeField] UITinter m_CreateToggleTabBlocker;
        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] GameObject m_LoadingSpinner;

        AuthenticationServiceFacade _mAuthenticationServiceFacade;
        LobbyServiceFacade _mLobbyServiceFacade;
        LocalLobbyUser _mLocalUser;
        LocalLobby _mLocalLobby;
        NameGenerationData _mNameGenerationData;
        ConnectionManager _mConnectionManager;
        ISubscriber<ConnectStatus> _mConnectStatusSubscriber;

        const string KDefaultLobbyName = "no-name";
        
        ISession _session;

        [Inject]
        void InjectDependenciesAndInitialize(
            AuthenticationServiceFacade authenticationServiceFacade,
            LobbyServiceFacade lobbyServiceFacade,
            LocalLobbyUser localUser,
            LocalLobby localLobby,
            NameGenerationData nameGenerationData,
            ISubscriber<ConnectStatus> connectStatusSub,
            ConnectionManager connectionManager
        )
        {
            _mAuthenticationServiceFacade = authenticationServiceFacade;
            _mNameGenerationData = nameGenerationData;
            _mLocalUser = localUser;
            _mLobbyServiceFacade = lobbyServiceFacade;
            _mLocalLobby = localLobby;
            _mConnectionManager = connectionManager;
            _mConnectStatusSubscriber = connectStatusSub;
            RegenerateName();

            _mConnectStatusSubscriber.Subscribe(OnConnectStatus);
        }

        void OnConnectStatus(ConnectStatus status)
        {
            if (status is ConnectStatus.GenericDisconnect or ConnectStatus.StartClientFailed)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnDestroy()
        {
            if (_mConnectStatusSubscriber != null)
            {
                _mConnectStatusSubscriber.Unsubscribe(OnConnectStatus);
            }
        }

        // Note: MultiplayerSDK refactoring
        //Lobby and Relay calls done from UI
        public async void CreateLobbyRequest(string lobbyName, bool isPrivate)
        {
            // before sending request to lobby service, populate an empty lobby name, if necessary
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = KDefaultLobbyName;
            }

            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await _mAuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            // don't create lobby here just yet, advance state machine instead
            /*// create lobby
            var lobbyCreationAttempt = await m_LobbyServiceFacade.TryCreateLobbyAsync(lobbyName, m_ConnectionManager.MaxConnectedPlayers, isPrivate);*/
            _mConnectionManager.StartHostLobby(lobbyName, _mLocalUser.DisplayName);
            
            UnblockUIAfterLoadingIsComplete();
            return;

            //if loby created, start host
            /*if (lobbyCreationAttempt.Success)
            {
                m_LocalUser.IsHost = true;
                m_LobbyServiceFacade.SetRemoteLobby(lobbyCreationAttempt.Lobby);

                Debug.Log($"Created lobby with ID: {m_LocalLobby.LobbyID} and code {m_LocalLobby.LobbyCode}");
                m_ConnectionManager.StartHostLobby(m_LocalUser.DisplayName);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }*/
        }

        public async void QueryLobbiesRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            if (blockUI)
            {
                BlockUIWhileLoadingIsInProgress();
            }

            bool playerIsAuthorized = await _mAuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (blockUI && !playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await _mLobbyServiceFacade.RetrieveAndPublishLobbyListAsync();

            if (blockUI)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        // Note: MultiplayerSDK refactoring
        public async void JoinLobbyWithCodeRequest(string lobbyCode)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await _mAuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }
            
            // TODO: need to unblock the UI elsewhere?
            
            _mConnectionManager.StartClientLobby(lobbyCode, _mLocalUser.DisplayName);
            return;
            
            /*var result = await m_LobbyServiceFacade.TryJoinLobbyAsync(null, lobbyCode);

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }*/
        }

        // Note: MultiplayerSDK refactoring
        public async void JoinLobbyRequest(ISessionInfo sessionInfo)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await _mAuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            //m_ConnectionManager.StartClientLobby(sessionInfo., m_LocalUser.DisplayName);
            
            // for now!
            /*var result = await m_LobbyServiceFacade.TryJoinLobbyAsync(lobby.LobbyID, lobby.LobbyCode);

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }*/
        }

        public async void QuickJoinRequest()
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await _mAuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var result = await _mLobbyServiceFacade.TryQuickJoinLobbyAsync();

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnJoinedLobby(Unity.Services.Lobbies.Models.Lobby remoteLobby)
        {
            _mLobbyServiceFacade.SetRemoteLobby(remoteLobby);

            Debug.Log($"Joined lobby with code: {_mLocalLobby.LobbyCode}, Internal Relay Join Code{_mLocalLobby.RelayJoinCode}");
            _mConnectionManager.StartClientLobby(string.Empty, _mLocalUser.DisplayName);
        }

        //show/hide UI

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
            m_LobbyCreationUI.Hide();
            m_LobbyJoiningUI.Hide();
        }

        public void ToggleJoinLobbyUI()
        {
            m_LobbyJoiningUI.Show();
            m_LobbyCreationUI.Hide();
            m_JoinToggleHighlight.SetToColor(1);
            m_JoinToggleTabBlocker.SetToColor(1);
            m_CreateToggleHighlight.SetToColor(0);
            m_CreateToggleTabBlocker.SetToColor(0);
        }

        public void ToggleCreateLobbyUI()
        {
            m_LobbyJoiningUI.Hide();
            m_LobbyCreationUI.Show();
            m_JoinToggleHighlight.SetToColor(0);
            m_JoinToggleTabBlocker.SetToColor(0);
            m_CreateToggleHighlight.SetToColor(1);
            m_CreateToggleTabBlocker.SetToColor(1);
        }

        public void RegenerateName()
        {
            _mLocalUser.DisplayName = _mNameGenerationData.GenerateName();
            m_PlayerNameLabel.text = _mLocalUser.DisplayName;
        }

        void BlockUIWhileLoadingIsInProgress()
        {
            m_CanvasGroup.interactable = false;
            m_LoadingSpinner.SetActive(true);
        }

        void UnblockUIAfterLoadingIsComplete()
        {
            //this callback can happen after we've already switched to a different scene
            //in that case the canvas group would be null
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = true;
                m_LoadingSpinner.SetActive(false);
            }
        }
    }
}
