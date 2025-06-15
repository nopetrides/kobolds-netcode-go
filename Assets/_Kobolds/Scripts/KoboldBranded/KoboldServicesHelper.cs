using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.Services;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Kobold.GameManagement;
using Kobold.Vivox;

namespace Kobold.Services
{
	public class KoboldServicesHelper : MonoBehaviour
	{
		public enum SessionType
		{
			SocialHub,
			Mission
		}

		static bool _sInitialLoad;

		// Track both session types
		ISession _mSocialHubSession;
		ISession _mMissionSession;
		SessionType _mCurrentSessionType;
		bool _mIsLeavingSession;

		// Configuration
		[Header("Session Configuration")]
		[SerializeField] int _socialHubMaxPlayers = 64;

		[SerializeField] int _missionMaxPlayers = 8;

		[Header("Scene Configuration")]
		[SerializeField] string _mainMenuScene = "MainMenu";

		[SerializeField] string _socialHubScene = "HubScene_TownMarket";
		[SerializeField] string _missionLobbyScene = "MissionLobby";

		// Properties
		public ISession CurrentSocialHubSession => _mSocialHubSession;
		public ISession CurrentMissionSession => _mMissionSession;
		public bool IsInSocialHub => _mSocialHubSession != null && _mSocialHubSession.State == SessionState.Connected;
		public bool IsInMission => _mMissionSession != null && _mMissionSession.State == SessionState.Connected;

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
				KoboldEventHandler.LoadMainMenuScene();
			}

			NetworkManager.Singleton.OnClientStopped += OnClientStopped;

			// Subscribe to Kobold events
    
			// Session events - maintain compatibility with original UI
			KoboldEventHandler.OnStartButtonPressed += OnStartButtonPressed;
    
			// New specific session events
			KoboldEventHandler.OnStartSocialHubPressed += OnStartSocialHubPressed;
			KoboldEventHandler.OnCreateMissionPressed += OnCreateMissionPressed;
			KoboldEventHandler.OnJoinMissionPressed += OnJoinMissionPressed;
			KoboldEventHandler.OnLeaveMissionPressed += LeaveMissionSession;
			KoboldEventHandler.OnReturnToSocialHubPressed += OnReturnToSocialHubPressed;
			KoboldEventHandler.OnReturnToMainMenuButtonPressed += LeaveCurrentSession;
			KoboldEventHandler.OnQuitGameButtonPressed += OnQuitGameButtonPressed;

			// Initialize Vivox
			if (KoboldVivoxManager.Instance != null)
			{
				await KoboldVivoxManager.Instance.Initialize();
			}
		}

		// Keep original handler for compatibility with existing UI
		async void OnStartButtonPressed(string playerName, string sessionName)
		{
			// Default behavior - join social hub
			var connectTask = ConnectToSocialHub(playerName, sessionName);
			await connectTask;
			KoboldEventHandler.ConnectToSessionComplete(connectTask, sessionName);
		}

		// New specific handlers
		async void OnStartSocialHubPressed(string playerName, string sessionName)
		{
			var connectTask = ConnectToSocialHub(playerName, sessionName);
			await connectTask;
			KoboldEventHandler.SocialHubConnectionComplete(connectTask, sessionName);
		}

		async void OnCreateMissionPressed(string missionName, int maxPlayers)
		{
			var success = await CreateMissionSession(missionName, maxPlayers);
			if (success)
			{
				KoboldEventHandler.MissionCreated(_mMissionSession.Code, maxPlayers);
			}
		}

		async void OnJoinMissionPressed(string sessionCode)
		{
			var success = await JoinMissionSession(sessionCode);
			if (success)
			{
				KoboldEventHandler.MissionJoined(sessionCode);
				KoboldEventHandler.MissionConnectionComplete(Task.CompletedTask, sessionCode);
			}
		}

		async void OnReturnToSocialHubPressed()
		{
			if (_mMissionSession != null)
			{
				LeaveMissionSession();
				// Load social hub scene
				KoboldEventHandler.LoadInGameScene();
			}
		}

		// Update cleanup methods to fire correct events
		void CleanupSocialHubSession()
		{
			if (_mSocialHubSession != null)
			{
				_mSocialHubSession.RemovedFromSession -= OnRemovedFromSocialHubSession;
				_mSocialHubSession.StateChanged -= OnSocialHubSessionStateChanged;
				_mSocialHubSession = null;
				KoboldEventHandler.ExitedSocialHub(); // Fire specific event
				KoboldEventHandler.ExitedSession(); // Also fire generic for compatibility
			}
		}

		void CleanupMissionSession()
		{
			if (_mMissionSession != null)
			{
				_mMissionSession.RemovedFromSession -= OnRemovedFromMissionSession;
				_mMissionSession.StateChanged -= OnMissionSessionStateChanged;
				_mMissionSession = null;
				KoboldEventHandler.ExitedMission(); // Fire mission-specific event
				// Don't fire ExitedSession here - we're still in social hub
			}
		}

		// Remember to unsubscribe in OnDestroy
		void OnDestroy()
		{
			if (NetworkManager.Singleton)
			{
				NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
			}

			KoboldEventHandler.OnStartButtonPressed -= OnStartButtonPressed;
			KoboldEventHandler.OnStartSocialHubPressed -= OnStartSocialHubPressed;
			KoboldEventHandler.OnCreateMissionPressed -= OnCreateMissionPressed;
			KoboldEventHandler.OnJoinMissionPressed -= OnJoinMissionPressed;
			KoboldEventHandler.OnLeaveMissionPressed -= LeaveMissionSession;
			KoboldEventHandler.OnReturnToSocialHubPressed -= OnReturnToSocialHubPressed;
			KoboldEventHandler.OnReturnToMainMenuButtonPressed -= LeaveCurrentSession;
			KoboldEventHandler.OnQuitGameButtonPressed -= OnQuitGameButtonPressed;
		}

		async void OnUnityServicesInitialized()
		{
			UnityServices.Initialized -= OnUnityServicesInitialized;
			await SignIn();
		}

		// New method for joining social hub specifically
		public async Task ConnectToSocialHub(string playerName, string sessionName)
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

			_mCurrentSessionType = SessionType.SocialHub;
			await CreateOrJoinSession(sessionName, _socialHubMaxPlayers, true);
		}

		// New method for creating/joining missions
		public async Task<bool> CreateMissionSession(string missionName, int maxPlayers = 0)
		{
			if (!IsInSocialHub)
			{
				Debug.LogError("Must be in social hub to create a mission");
				return false;
			}

			if (maxPlayers <= 0)
			{
				maxPlayers = _missionMaxPlayers;
			}

			try
			{
				_mCurrentSessionType = SessionType.Mission;
				var missionSessionName = $"Mission_{missionName}_{Guid.NewGuid():N}";
				await CreateOrJoinSession(missionSessionName, maxPlayers, false);
				return _mMissionSession != null;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		public async Task<bool> JoinMissionSession(string sessionCode)
		{
			if (!IsInSocialHub)
			{
				Debug.LogError("Must be in social hub to join a mission");
				return false;
			}

			try
			{
				_mCurrentSessionType = SessionType.Mission;
				var options = new SessionOptions()
				{
					MaxPlayers = _missionMaxPlayers,
					IsPrivate = false,
				}.WithDistributedAuthorityNetwork();

				_mMissionSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);

				if (_mMissionSession != null)
				{
					_mMissionSession.RemovedFromSession += OnRemovedFromMissionSession;
					_mMissionSession.StateChanged += OnMissionSessionStateChanged;
				}

				return _mMissionSession != null;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		async Task CreateOrJoinSession(string sessionName, int maxPlayers, bool isSocialHub)
		{
			var options = new SessionOptions()
			{
				Name = sessionName,
				MaxPlayers = maxPlayers,
				IsPrivate = false,
			}.WithDistributedAuthorityNetwork();

			var session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);

			if (isSocialHub)
			{
				_mSocialHubSession = session;
				_mSocialHubSession.RemovedFromSession += OnRemovedFromSocialHubSession;
				_mSocialHubSession.StateChanged += OnSocialHubSessionStateChanged;
			}
			else
			{
				_mMissionSession = session;
				_mMissionSession.RemovedFromSession += OnRemovedFromMissionSession;
				_mMissionSession.StateChanged += OnMissionSessionStateChanged;
			}
		}

		public async void LeaveMissionSession()
		{
			if (_mMissionSession != null && !_mIsLeavingSession)
			{
				try
				{
					_mIsLeavingSession = true;
					await _mMissionSession.LeaveAsync();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
				finally
				{
					_mIsLeavingSession = false;
					CleanupMissionSession();
				}
			}
		}

		async void LeaveCurrentSession()
		{
			// Leave mission first if in one
			if (_mMissionSession != null)
			{
				LeaveMissionSession();
			}

			// Then leave social hub
			if (_mSocialHubSession != null && !_mIsLeavingSession)
			{
				try
				{
					_mIsLeavingSession = true;
					await _mSocialHubSession.LeaveAsync();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
				finally
				{
					_mIsLeavingSession = false;
					CleanupSocialHubSession();
				}
			}
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

		void OnQuitGameButtonPressed()
		{
			LeaveCurrentSession();
			Application.Quit();
		}

		void SignInFailed(RequestFailedException e)
		{
			AuthenticationService.Instance.SignInFailed -= SignInFailed;
			Debug.LogWarning($"Sign in via Authentication failed: e.ErrorCode {e.ErrorCode}");
		}

		// Session event handlers
		void OnRemovedFromSocialHubSession()
		{
			CleanupSocialHubSession();
		}

		void OnSocialHubSessionStateChanged(SessionState sessionState)
		{
			if (sessionState != SessionState.Connected)
			{
				CleanupSocialHubSession();
			}
		}

		void OnRemovedFromMissionSession()
		{
			CleanupMissionSession();
		}

		void OnMissionSessionStateChanged(SessionState sessionState)
		{
			if (sessionState != SessionState.Connected)
			{
				CleanupMissionSession();
			}
		}

		void OnClientStopped(bool obj)
		{
			LeaveCurrentSession();
		}

		static string GetRandomString(int length)
		{
			var r = new System.Random();
			var result = new char[length];

			for (var i = 0; i < length; i++)
			{
				result[i] = (char) r.Next('a', 'z' + 1);
			}

			return new string(result);
		}
	}
}