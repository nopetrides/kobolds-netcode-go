using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.UI;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.BossRoom.Utils;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states.
    /// </summary>
    /// <remarks> OnNetworkSpawn() won't ever run, because there is no network connection at the main menu screen.
    /// Fortunately we know you are a client, because all players are clients when sitting at the main menu screen.
    /// </remarks>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.MainMenu;

        [SerializeField]
        NameGenerationData m_NameGenerationData;
        [SerializeField]
        LobbyUIMediator m_LobbyUIMediator;
        [SerializeField]
        IpuiMediator m_IPUIMediator;
        [SerializeField]
        Button m_LobbyButton;
        [SerializeField]
        GameObject m_SignInSpinner;
        [SerializeField]
        UIProfileSelector m_UIProfileSelector;
        [SerializeField]
        UITooltipDetector m_UGSSetupTooltipDetector;

        [Inject]
        AuthenticationServiceFacade _mAuthServiceFacade;
        [Inject]
        LocalLobbyUser _mLocalUser;
        [Inject]
        LocalLobby _mLocalLobby;
        [Inject]
        ProfileManager _mProfileManager;

        protected override void Awake()
        {
            base.Awake();

            m_LobbyButton.interactable = false;
            m_LobbyUIMediator.Hide();

            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                OnSignInFailed();
                return;
            }

            TrySignIn();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(m_NameGenerationData);
            builder.RegisterComponent(m_LobbyUIMediator);
            builder.RegisterComponent(m_IPUIMediator);
        }

        private async void TrySignIn()
        {
            try
            {
                var unityAuthenticationInitOptions =
                    _mAuthServiceFacade.GenerateAuthenticationOptions(_mProfileManager.Profile);

                await _mAuthServiceFacade.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                _mProfileManager.OnProfileChanged += OnProfileChanged;
            }
            catch (Exception)
            {
                OnSignInFailed();
            }
        }

        private void OnAuthSignIn()
        {
            m_LobbyButton.interactable = true;
            m_UGSSetupTooltipDetector.enabled = false;
            m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Note: MultiplayerSDK refactoring
            //m_LocalUser.ID = AuthenticationService.Instance.PlayerId;

            // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
            //m_LocalLobby.AddUser(m_LocalUser);
        }

        private void OnSignInFailed()
        {
            if (m_LobbyButton)
            {
                m_LobbyButton.interactable = false;
                m_UGSSetupTooltipDetector.enabled = true;
            }

            if (m_SignInSpinner)
            {
                m_SignInSpinner.SetActive(false);
            }
        }

        protected override void OnDestroy()
        {
            _mProfileManager.OnProfileChanged -= OnProfileChanged;
            base.OnDestroy();
        }

        async void OnProfileChanged()
        {
            m_LobbyButton.interactable = false;
            m_SignInSpinner.SetActive(true);
            await _mAuthServiceFacade.SwitchProfileAndReSignInAsync(_mProfileManager.Profile);

            m_LobbyButton.interactable = true;
            m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Updating LocalUser and LocalLobby
            _mLocalLobby.RemoveUser(_mLocalUser);
            _mLocalUser.ID = AuthenticationService.Instance.PlayerId;
            _mLocalLobby.AddUser(_mLocalUser);
        }

        public void OnStartClicked()
        {
            m_LobbyUIMediator.ToggleJoinLobbyUI();
            m_LobbyUIMediator.Show();
        }

        public void OnDirectIPClicked()
        {
            m_LobbyUIMediator.Hide();
            m_IPUIMediator.Show();
        }

        public void OnChangeProfileClicked()
        {
            m_UIProfileSelector.Show();
        }
    }
}
