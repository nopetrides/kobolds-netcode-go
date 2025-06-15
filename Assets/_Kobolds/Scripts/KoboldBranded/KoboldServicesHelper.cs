using System;
using System.Threading;
using System.Threading.Tasks;
using Kobold.GameManagement;
using Kobold.Vivox;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Kobold.Services
{
	public class KoboldServicesHelper : MonoBehaviour
	{
		public enum SessionType
		{
			SocialHub,
			Mission
		}

		// Thread-safe initialization tracking
		private static readonly object InitLock = new();
		private static bool _isInitializing;
		private static bool _hasInitialized;

		// Configuration
		[Header("Session Configuration")]
		[SerializeField] private int _socialHubMaxPlayers = 64;

		[SerializeField] private int _missionMaxPlayers = 8;

		[Header("Scene Configuration")]
		[SerializeField] private string _mainMenuScene = "MainMenu";

		[SerializeField] private string _socialHubScene = "HubScene_TownMarket";
		[SerializeField] private string _missionLobbyScene = "MissionLobby";

		// Cancellation for cleanup
		private CancellationTokenSource _cancellationTokenSource;
		private SessionType _currentSessionType;
		private bool _isLeavingSession;

		// Track both session types

		// Properties
		public ISession CurrentSocialHubSession { get; private set; }

		public ISession CurrentMissionSession { get; private set; }

		public bool IsInSocialHub =>
			CurrentSocialHubSession != null && CurrentSocialHubSession.State == SessionState.Connected;

		public bool IsInMission =>
			CurrentMissionSession != null && CurrentMissionSession.State == SessionState.Connected;

		private void Awake()
		{
			DontDestroyOnLoad(this);
			_cancellationTokenSource = new CancellationTokenSource();
		}

		private void Start()
		{
			// Use proper async initialization
			InitializeAsync().ContinueWith(
				task =>
				{
					if (task.IsFaulted)
						Debug.LogError($"[KoboldServicesHelper] Initialization failed: {task.Exception}");
				}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void OnDestroy()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();

			if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientStopped -= OnClientStopped;

			UnsubscribeFromEvents();
		}

		// Reset static state when domain reloads
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			lock (InitLock)
			{
				_isInitializing = false;
				_hasInitialized = false;
			}

			Debug.Log("[KoboldServicesHelper] Static state reset");
		}

		private async Task InitializeAsync()
		{
			try
			{
				// Thread-safe initialization check
				lock (InitLock)
				{
					if (_isInitializing || _hasInitialized)
					{
						Debug.Log("[KoboldServicesHelper] Already initializing or initialized");
						return;
					}

					_isInitializing = true;
				}

				// Initialize Unity Services with timeout
				var initTask = UnityServices.InitializeAsync();
				var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), _cancellationTokenSource.Token);

				var completedTask = await Task.WhenAny(initTask, timeoutTask);

				if (completedTask == timeoutTask) throw new TimeoutException("Unity Services initialization timed out");

				await initTask; // Ensure we get any exceptions

				Debug.Log("[KoboldServicesHelper] Unity Services initialized successfully");

				// Subscribe to events after successful initialization
				SubscribeToEvents();

				// Initialize NetworkManager callbacks
				if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientStopped += OnClientStopped;

				// Initialize Vivox
				if (KoboldVivoxManager.Instance != null) await InitializeVivoxAsync();

				// Load main menu scene after everything is initialized
				lock (InitLock)
				{
					_hasInitialized = true;
					_isInitializing = false;
				}

				// Only load main menu if we're not already in a scene
				if (SceneManager.GetActiveScene().name == "KoboldBoot") KoboldEventHandler.LoadMainMenuScene(_mainMenuScene);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Initialization failed: {ex}");
				lock (InitLock)
				{
					_isInitializing = false;
				}

				throw;
			}
		}

		private async Task InitializeVivoxAsync()
		{
			try
			{
				await KoboldVivoxManager.Instance.Initialize();
				Debug.Log("[KoboldServicesHelper] Vivox initialized successfully");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Vivox initialization failed: {ex}");
				// Don't throw - Vivox failure shouldn't prevent game from running
			}
		}

		private void SubscribeToEvents()
		{
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
		}

		private void UnsubscribeFromEvents()
		{
			KoboldEventHandler.OnStartButtonPressed -= OnStartButtonPressed;
			KoboldEventHandler.OnStartSocialHubPressed -= OnStartSocialHubPressed;
			KoboldEventHandler.OnCreateMissionPressed -= OnCreateMissionPressed;
			KoboldEventHandler.OnJoinMissionPressed -= OnJoinMissionPressed;
			KoboldEventHandler.OnLeaveMissionPressed -= LeaveMissionSession;
			KoboldEventHandler.OnReturnToSocialHubPressed -= OnReturnToSocialHubPressed;
			KoboldEventHandler.OnReturnToMainMenuButtonPressed -= LeaveCurrentSession;
			KoboldEventHandler.OnQuitGameButtonPressed -= OnQuitGameButtonPressed;
		}

		// Event handlers with proper async patterns
		private async void OnStartButtonPressed(string playerName, string sessionName)
		{
			try
			{
				var connectTask = ConnectToSocialHub(playerName, sessionName);
				await connectTask;
				KoboldEventHandler.ConnectToSessionComplete(connectTask, sessionName);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Failed to connect to session: {ex}");
				KoboldEventHandler.ConnectToSessionComplete(Task.FromException(ex), sessionName);
			}
		}

		private async void OnStartSocialHubPressed(string playerName, string sessionName)
		{
			try
			{
				var connectTask = ConnectToSocialHub(playerName, sessionName);
				await connectTask;
				KoboldEventHandler.SocialHubConnectionComplete(connectTask, sessionName);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Failed to connect to social hub: {ex}");
				KoboldEventHandler.SocialHubConnectionComplete(Task.FromException(ex), sessionName);
			}
		}

		private async void OnCreateMissionPressed(string missionName, int maxPlayers)
		{
			try
			{
				var success = await CreateMissionSession(missionName, maxPlayers);
				if (success) KoboldEventHandler.MissionCreated(CurrentMissionSession.Code, maxPlayers);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Failed to create mission: {ex}");
			}
		}

		private async void OnJoinMissionPressed(string sessionCode)
		{
			try
			{
				var success = await JoinMissionSession(sessionCode);
				if (success)
				{
					KoboldEventHandler.MissionJoined(sessionCode);
					KoboldEventHandler.MissionConnectionComplete(Task.CompletedTask, sessionCode);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Failed to join mission: {ex}");
				KoboldEventHandler.MissionConnectionComplete(Task.FromException(ex), sessionCode);
			}
		}

		private void OnReturnToSocialHubPressed()
		{
			if (CurrentMissionSession != null)
			{
				LeaveMissionSession();
				// Load social hub scene
				KoboldEventHandler.LoadInGameScene(_socialHubScene);
			}
		}

		public async Task ConnectToSocialHub(string playerName, string sessionName)
		{
			if (AuthenticationService.Instance == null)
				throw new InvalidOperationException("Authentication service is not available");

			if (!AuthenticationService.Instance.IsSignedIn) await SignIn();

			await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

			if (string.IsNullOrEmpty(sessionName)) throw new ArgumentException("Session name cannot be empty");

			_currentSessionType = SessionType.SocialHub;
			await CreateOrJoinSession(sessionName, _socialHubMaxPlayers, true);
		}

		public async Task<bool> CreateMissionSession(string missionName, int maxPlayers = 0)
		{
			if (!IsInSocialHub)
			{
				Debug.LogError("Must be in social hub to create a mission");
				return false;
			}

			if (maxPlayers <= 0) maxPlayers = _missionMaxPlayers;

			try
			{
				_currentSessionType = SessionType.Mission;
				var missionSessionName = $"Mission_{missionName}_{Guid.NewGuid():N}";
				await CreateOrJoinSession(missionSessionName, maxPlayers, false);
				return CurrentMissionSession != null;
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
				_currentSessionType = SessionType.Mission;
				var options = new SessionOptions
				{
					MaxPlayers = _missionMaxPlayers,
					IsPrivate = false
				}.WithDistributedAuthorityNetwork();

				CurrentMissionSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);

				if (CurrentMissionSession != null)
				{
					CurrentMissionSession.RemovedFromSession += OnRemovedFromMissionSession;
					CurrentMissionSession.StateChanged += OnMissionSessionStateChanged;
				}

				return CurrentMissionSession != null;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		private async Task CreateOrJoinSession(string sessionName, int maxPlayers, bool isSocialHub)
		{
			var options = new SessionOptions
			{
				Name = sessionName,
				MaxPlayers = maxPlayers,
				IsPrivate = false
			}.WithDistributedAuthorityNetwork();

			var session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);

			if (isSocialHub)
			{
				CurrentSocialHubSession = session;
				CurrentSocialHubSession.RemovedFromSession += OnRemovedFromSocialHubSession;
				CurrentSocialHubSession.StateChanged += OnSocialHubSessionStateChanged;
			}
			else
			{
				CurrentMissionSession = session;
				CurrentMissionSession.RemovedFromSession += OnRemovedFromMissionSession;
				CurrentMissionSession.StateChanged += OnMissionSessionStateChanged;
			}
		}

		public async void LeaveMissionSession()
		{
			if (CurrentMissionSession != null && !_isLeavingSession)
				try
				{
					_isLeavingSession = true;
					await CurrentMissionSession.LeaveAsync();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
				finally
				{
					_isLeavingSession = false;
					CleanupMissionSession();
				}
		}

		private async void LeaveCurrentSession()
		{
			// Leave mission first if in one
			if (CurrentMissionSession != null) LeaveMissionSession();

			// Then leave social hub
			if (CurrentSocialHubSession != null && !_isLeavingSession)
				try
				{
					_isLeavingSession = true;
					await CurrentSocialHubSession.LeaveAsync();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
				finally
				{
					_isLeavingSession = false;
					CleanupSocialHubSession();
				}
		}

		private async Task SignIn()
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
			finally
			{
				AuthenticationService.Instance.SignInFailed -= SignInFailed;
			}
		}

		private void OnQuitGameButtonPressed()
		{
			LeaveCurrentSession();
			Application.Quit();
		}

		private void SignInFailed(RequestFailedException e)
		{
			Debug.LogWarning($"Sign in via Authentication failed: e.ErrorCode {e.ErrorCode}");
		}

		// Session event handlers
		private void OnRemovedFromSocialHubSession()
		{
			CleanupSocialHubSession();
		}

		private void OnSocialHubSessionStateChanged(SessionState sessionState)
		{
			if (sessionState != SessionState.Connected) CleanupSocialHubSession();
		}

		private void OnRemovedFromMissionSession()
		{
			CleanupMissionSession();
		}

		private void OnMissionSessionStateChanged(SessionState sessionState)
		{
			if (sessionState != SessionState.Connected) CleanupMissionSession();
		}

		private void OnClientStopped(bool obj)
		{
			LeaveCurrentSession();
		}

		private void CleanupSocialHubSession()
		{
			if (CurrentSocialHubSession != null)
			{
				CurrentSocialHubSession.RemovedFromSession -= OnRemovedFromSocialHubSession;
				CurrentSocialHubSession.StateChanged -= OnSocialHubSessionStateChanged;
				CurrentSocialHubSession = null;
				KoboldEventHandler.ExitedSocialHub();
				KoboldEventHandler.ExitedSession();
			}
		}

		private void CleanupMissionSession()
		{
			if (CurrentMissionSession != null)
			{
				CurrentMissionSession.RemovedFromSession -= OnRemovedFromMissionSession;
				CurrentMissionSession.StateChanged -= OnMissionSessionStateChanged;
				CurrentMissionSession = null;
				KoboldEventHandler.ExitedMission();
			}
		}

		private static string GetRandomString(int length)
		{
			var r = new Random();
			var result = new char[length];

			for (var i = 0; i < length; i++) result[i] = (char) r.Next('a', 'z' + 1);

			return new string(result);
		}
	}
}
