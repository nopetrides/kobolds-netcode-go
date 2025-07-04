using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class ServicesHelper : MonoBehaviour
    {
        static bool _sInitialLoad;

        Task _mSessionTask;

        ISession _mCurrentSession;
        bool _mIsLeavingSession;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        async void Start()
        {
            UnityServices.Initialized += OnUnityServicesInitialized;
            await UnityServices.InitializeAsync();

            if (!_sInitialLoad)
            {
                _sInitialLoad = true;
                GameplayEventHandler.LoadMainMenuScene();
            }

            NetworkManager.Singleton.OnClientStopped += OnClientStopped;

            GameplayEventHandler.OnStartButtonPressed += OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed += LeaveSession;
            GameplayEventHandler.OnQuitGameButtonPressed += OnQuitGameButtonPressed;

            await VivoxManager.Instance.Initialize();
        }

        async void OnUnityServicesInitialized()
        {
            UnityServices.Initialized -= OnUnityServicesInitialized;
            await SignIn();
        }

        async void OnStartButtonPressed(string playerName, string sessionName)
        {
            var connectTask = ConnectToSession(playerName, sessionName);
            await connectTask;
            GameplayEventHandler.ConnectToSessionComplete(connectTask, sessionName);
        }

        async Task ConnectToSession(string playerName, string sessionName)
        {
            if (AuthenticationService.Instance == null)
            {
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignIn();
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

            if (string.IsNullOrEmpty(sessionName))
            {
                Debug.LogError("Session name is empty. Cannot connect.");
                return;
            }

            await ConnectThroughLiveService(sessionName);
        }

        async Task SignIn()
        {
            try
            {
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                AuthenticationService.Instance.SwitchProfile(GetRandomString(5));
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        async Task ConnectThroughLiveService(string sessionName)
        {
            // Join Session
            var options = new SessionOptions()
            {
                Name = sessionName,
                MaxPlayers = 64,
                IsPrivate = false,
            }.WithDistributedAuthorityNetwork();

            _mCurrentSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
            _mCurrentSession.RemovedFromSession += RemovedFromSession;
            _mCurrentSession.StateChanged += CurrentSessionOnStateChanged;
        }

        void OnQuitGameButtonPressed()
        {
            LeaveSession();
            Application.Quit();
        }

        async void LeaveSession()
        {
            if (_mCurrentSession != null && !_mIsLeavingSession)
            {
                try
                {
                    _mIsLeavingSession = true;
                    await _mCurrentSession.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }
                finally
                {
                    _mIsLeavingSession = false;
                    ExitedSession();
                }
            }
        }

        void SignInFailed(RequestFailedException e)
        {
            AuthenticationService.Instance.SignInFailed -= SignInFailed;
            Debug.LogWarning($"Sign in via Authentication failed: e.ErrorCode {e.ErrorCode}");
        }

        void RemovedFromSession()
        {
            ExitedSession();
        }

        void CurrentSessionOnStateChanged(SessionState sessionState)
        {
            if (sessionState != SessionState.Connected)
            {
                ExitedSession();
            }
        }

        void ExitedSession()
        {
            if (_mCurrentSession != null)
            {
                _mCurrentSession.RemovedFromSession -= RemovedFromSession;
                _mCurrentSession.StateChanged -= CurrentSessionOnStateChanged;
                _mCurrentSession = null;
                GameplayEventHandler.ExitedSession();
            }
        }

        void OnClientStopped(bool obj)
        {
            LeaveSession();
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            }

            GameplayEventHandler.OnStartButtonPressed -= OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed -= LeaveSession;
            GameplayEventHandler.OnQuitGameButtonPressed -= OnQuitGameButtonPressed;
        }

        static string GetRandomString(int length)
        {
            var r = new System.Random();
            var result = new char[length];

            for (var i = 0; i < length; i++)
            {
                result[i] = (char)r.Next('a', 'z' + 1);
            }

            return new string(result);
        }
    }
}
