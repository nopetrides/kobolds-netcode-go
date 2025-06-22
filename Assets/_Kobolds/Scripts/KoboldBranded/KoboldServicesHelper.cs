using System;
using System.Threading;
using System.Threading.Tasks;
using Kobold;
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
		
		public static bool HasInitialized => _hasInitialized;

		// Configuration
		[Header("Session Configuration")]
		[SerializeField] private int _socialHubMaxPlayers = 64;

		[SerializeField] private int _missionMaxPlayers = 8;

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
				//if (SceneManager.GetActiveScene().name == "KoboldBoot") 
					//KoboldEventHandler.LoadMainMenuScene(nameof(SceneNames.KoboldMainMenu));
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
			// New specific session events
			KoboldEventHandler.OnStartSocialHubPressed += OnStartSocialHubPressed;
			KoboldEventHandler.OnStartKoboldMissionPressed += OnStartKoboldMissionPressed;
			KoboldEventHandler.OnReturnToSocialHubPressed += OnReturnToSocialHubPressed;
			KoboldEventHandler.OnReturnToMainMenuButtonPressed += LeaveCurrentSession;
			KoboldEventHandler.OnQuitGameButtonPressed += OnQuitGameButtonPressed;
		}

		private void UnsubscribeFromEvents()
		{
			KoboldEventHandler.OnStartSocialHubPressed -= OnStartSocialHubPressed;
			KoboldEventHandler.OnStartKoboldMissionPressed -= OnStartKoboldMissionPressed;
			KoboldEventHandler.OnReturnToSocialHubPressed -= OnReturnToSocialHubPressed;
			KoboldEventHandler.OnReturnToMainMenuButtonPressed -= LeaveCurrentSession;
			KoboldEventHandler.OnQuitGameButtonPressed -= OnQuitGameButtonPressed;
		}

		private void OnStartSocialHubPressed(string playerName, string sessionName)
		{
			Debug.Log("[KoboldServicesHelper.OnStartSocialHubPressed]");
			_ = StartSocialHubTask(playerName, sessionName); // fire and forget
		}
		
		private async Task StartSocialHubTask(string playerName, string sessionName)
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

		private void OnStartKoboldMissionPressed(string playerName, string sessionName)
		{
			Debug.Log("[KoboldServicesHelper.OnStartKoboldMissionPressed]");
			_ = StartMissionTask(playerName, sessionName); // fire and forget
		}
		
		private async Task StartMissionTask(string playerName, string sessionName)
		{
			try
			{
				var connectTask = ConnectToKoboldMission(playerName, sessionName);
				await connectTask;
				KoboldEventHandler.MissionConnectionComplete(connectTask, sessionName);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldServicesHelper] Failed to connect to social hub: {ex}");
				KoboldEventHandler.MissionConnectionComplete(Task.FromException(ex), sessionName);
			}
		}
		

		private void OnReturnToSocialHubPressed()
		{
			Debug.Log("[KoboldServicesHelper.OnReturnToSocialHubPressed]");
			if (CurrentMissionSession != null)
			{
				LeaveMissionSession();
				// Load social hub scene
				KoboldEventHandler.LoadInGameScene(nameof(SceneNames.KoboldHub));
			}
		}

		public async Task ConnectToSocialHub(string playerName, string sessionName)
		{
			Debug.Log("[KoboldServicesHelper.ConnectToSocialHub]");
			try
			{
				if (AuthenticationService.Instance == null)
					throw new InvalidOperationException("Authentication service is not available");

				if (!AuthenticationService.Instance.IsSignedIn) await SignIn();

				await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

				if (string.IsNullOrEmpty(sessionName)) throw new ArgumentException("Session name cannot be empty");

				_currentSessionType = SessionType.SocialHub;
				await CreateOrJoinSession(sessionName, _socialHubMaxPlayers, true);
			}
			catch (Exception e)
			{
				Debug.LogError($"[ConnectToSocialHub] Failed to connect to session: {e}");
			}
		}


		private async Task CreateOrJoinSession(string sessionName, int maxPlayers, bool isSocialHub)
		{
			Debug.Log("[KoboldServicesHelper.CreateOrJoinSession]");
			try
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
			catch (Exception e)
			{
				Debug.LogError($"[CreateOrJoinSession] Failed to connect to session: {e}");
			}
		}
		
		public async Task<bool> ConnectToKoboldMission(string playerName, string sessionName)
		{
			Debug.Log("[KoboldServicesHelper.ConnectToKoboldMission]");

			try
			{
				if (AuthenticationService.Instance == null)
					throw new InvalidOperationException("Authentication service is not available");

				if (!AuthenticationService.Instance.IsSignedIn)
					await SignIn();

				await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

				if (string.IsNullOrEmpty(sessionName))
					throw new ArgumentException("Session name cannot be empty");

				_currentSessionType = SessionType.Mission;

				await CreateOrJoinSession(sessionName, _missionMaxPlayers, isSocialHub: false);

				return CurrentMissionSession != null;
			}
			catch (Exception e)
			{
				Debug.LogError($"[ConnectToKoboldMission] Failed to connect: {e}");
				return false;
			}
		}


		public async void LeaveMissionSession()
		{
			Debug.Log("[KoboldServicesHelper.LeaveMissionSession]");
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
			Debug.Log("[KoboldServicesHelper.LeaveCurrentSession]");
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
			Debug.Log("[KoboldServicesHelper.SignIn]");
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
			Debug.Log("[KoboldServicesHelper.OnQuitGameButtonPressed]");
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
			Debug.Log("[KoboldServicesHelper.OnRemovedFromSocialHubSession]");
			CleanupSocialHubSession();
		}

		private void OnSocialHubSessionStateChanged(SessionState sessionState)
		{
			Debug.Log("[KoboldServicesHelper.OnSocialHubSessionStateChanged]");
			if (sessionState != SessionState.Connected) CleanupSocialHubSession();
		}

		private void OnRemovedFromMissionSession()
		{
			Debug.Log("[KoboldServicesHelper.OnRemovedFromMissionSession]");
			CleanupMissionSession();
		}

		private void OnMissionSessionStateChanged(SessionState sessionState)
		{
			Debug.Log("[KoboldServicesHelper.OnMissionSessionStateChanged]");
			if (sessionState != SessionState.Connected) CleanupMissionSession();
		}

		private void OnClientStopped(bool obj)
		{
			Debug.Log("[KoboldServicesHelper.OnClientStopped]");
			LeaveCurrentSession();
		}

		private void CleanupSocialHubSession()
		{
			Debug.Log("[KoboldServicesHelper.CleanupSocialHubSession]");
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
			Debug.Log("[KoboldServicesHelper.CleanupMissionSession]");
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
