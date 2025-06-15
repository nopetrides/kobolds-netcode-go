using System;
using Kobold.GameManagement;
using Kobold.UI.Components;
using Kobold.UI.Theming;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kobold.UI.Views
{
	public class KoboldHomeScreenView : MonoBehaviour
	{
		[Header("UI Document")]
		[SerializeField] private UIDocument _document;

		[Header("Configuration")]
		[SerializeField] private string _defaultPlayerName = "Kobold";

		[SerializeField] private string _defaultSessionName = "KoboldHub";

		// State tracking
		private bool _isConnecting;
		private bool _isInitialized;

		// Panels
		private VisualElement _mainPanel;
		private TextField _playerNameField;
		private Button _quickMissionButton;
		private Button _quitButton;

		private VisualElement _root;
		private TextField _sessionNameField;
		private Button _settingsButton;
		private VisualElement _settingsPanel;
		private Button _socialHubButton;
		private VisualElement _socialHubPanel;

		private void Awake()
		{
			// Ensure we have a UIDocument reference
			if (_document == null)
			{
				_document = GetComponent<UIDocument>();
				if (_document == null)
				{
					Debug.LogError($"[{name}] No UIDocument component found!");
					enabled = false;
				}
			}
		}

		private void OnEnable()
		{
			if (!TryInitializeUI())
			{
				Debug.LogError($"[{name}] Failed to initialize UI");
				enabled = false;
				return;
			}

			// Register with theme manager
			RegisterWithThemeManager();

			ShowMainPanel();
		}

		private void OnDisable()
		{
			if (_isInitialized)
			{
				UnregisterCallbacks();
				UnregisterFromThemeManager();
			}
		}

		private bool TryInitializeUI()
		{
			try
			{
				if (_document == null || _document.rootVisualElement == null)
				{
					Debug.LogError($"[{name}] UIDocument or root element is null");
					return false;
				}

				_root = _document.rootVisualElement;

				if (!CacheUIElements()) return false;

				SetupDefaultValues();
				RegisterCallbacks();

				_isInitialized = true;
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{name}] Error initializing UI: {ex}");
				return false;
			}
		}

		private bool CacheUIElements()
		{
			// Get panel references
			_mainPanel = _root.Q<VisualElement>("main-panel");
			_socialHubPanel = _root.Q<VisualElement>("social-hub-panel");
			_settingsPanel = _root.Q<VisualElement>("settings-panel");

			if (_mainPanel == null || _socialHubPanel == null || _settingsPanel == null)
			{
				Debug.LogError($"[{name}] One or more panels not found in UXML");
				return false;
			}

			// Main menu buttons
			_socialHubButton = _root.Q<Button>("social-hub-button");
			_quickMissionButton = _root.Q<Button>("quick-mission-button");
			_settingsButton = _root.Q<Button>("settings-button");
			_quitButton = _root.Q<Button>("quit-button");

			if (_socialHubButton == null || _quickMissionButton == null ||
				_settingsButton == null || _quitButton == null)
			{
				Debug.LogError($"[{name}] One or more main menu buttons not found");
				return false;
			}

			// Social hub panel elements
			_playerNameField = _root.Q<TextField>("player-name-field");
			_sessionNameField = _root.Q<TextField>("session-name-field");

			if (_playerNameField == null || _sessionNameField == null)
				Debug.LogWarning($"[{name}] Player or session name fields not found");

			return true;
		}

		private void SetupDefaultValues()
		{
			// Set default values with null checks
			if (_playerNameField != null)
				_playerNameField.value = PlayerPrefs.GetString("PlayerName", _defaultPlayerName);

			if (_sessionNameField != null)
				_sessionNameField.value = PlayerPrefs.GetString("LastSession", _defaultSessionName);
		}

		private void RegisterCallbacks()
		{
			// Main menu buttons
			_socialHubButton.clicked += ShowSocialHubPanel;
			_quickMissionButton.clicked += OpenQuickMission;
			_settingsButton.clicked += ShowSettingsPanel;
			_quitButton.clicked += QuitGame;

			// Social hub panel buttons
			var joinHubButton = _root.Q<Button>("join-hub-button");
			if (joinHubButton != null)
				joinHubButton.clicked += JoinSocialHub;

			var backFromHubButton = _root.Q<Button>("back-from-hub-button");
			if (backFromHubButton != null)
				backFromHubButton.clicked += ShowMainPanel;

			// Settings panel buttons
			var backFromSettingsButton = _root.Q<Button>("back-from-settings-button");
			if (backFromSettingsButton != null)
				backFromSettingsButton.clicked += ShowMainPanel;
		}

		private void UnregisterCallbacks()
		{
			// Main menu buttons
			if (_socialHubButton != null) _socialHubButton.clicked -= ShowSocialHubPanel;
			if (_quickMissionButton != null) _quickMissionButton.clicked -= OpenQuickMission;
			if (_settingsButton != null) _settingsButton.clicked -= ShowSettingsPanel;
			if (_quitButton != null) _quitButton.clicked -= QuitGame;

			// Social hub panel buttons
			var joinHubButton = _root?.Q<Button>("join-hub-button");
			if (joinHubButton != null) joinHubButton.clicked -= JoinSocialHub;

			var backFromHubButton = _root?.Q<Button>("back-from-hub-button");
			if (backFromHubButton != null) backFromHubButton.clicked -= ShowMainPanel;

			// Settings panel buttons
			var backFromSettingsButton = _root?.Q<Button>("back-from-settings-button");
			if (backFromSettingsButton != null) backFromSettingsButton.clicked -= ShowMainPanel;
		}

		private void RegisterWithThemeManager()
		{
			var themeManager = KoboldThemeManager.Instance;
			if (themeManager != null && _document != null)
				themeManager.RegisterUIDocument(_document);
			else
				Debug.LogWarning($"[{name}] Theme manager not available");
		}

		private void UnregisterFromThemeManager()
		{
			// Don't create new instances during cleanup
			if (!Application.isPlaying)
				return;

			// Use FindObjectOfType instead of Instance to avoid creating new ones
			var themeManager = FindFirstObjectByType<KoboldThemeManager>();
			if (themeManager != null && _document != null) themeManager.UnregisterUIDocument(_document);
		}

		private void ShowMainPanel()
		{
			_mainPanel?.RemoveFromClassList("hidden");
			_socialHubPanel?.AddToClassList("hidden");
			_settingsPanel?.AddToClassList("hidden");

			// Re-enable buttons if they were disabled during connection
			if (_isConnecting)
			{
				_isConnecting = false;
				SetButtonsEnabled(true);
			}
		}

		private void ShowSocialHubPanel()
		{
			_mainPanel?.AddToClassList("hidden");
			_socialHubPanel?.RemoveFromClassList("hidden");
			_settingsPanel?.AddToClassList("hidden");

			PlayUISound(UISoundType.Click);
		}

		private void ShowSettingsPanel()
		{
			_mainPanel?.AddToClassList("hidden");
			_socialHubPanel?.AddToClassList("hidden");
			_settingsPanel?.RemoveFromClassList("hidden");

			PlayUISound(UISoundType.Click);
		}

		private void JoinSocialHub()
		{
			if (_isConnecting)
			{
				Debug.LogWarning($"[{name}] Already connecting to a session");
				return;
			}

			var playerName = _playerNameField?.value ?? _defaultPlayerName;
			var sessionName = _sessionNameField?.value ?? _defaultSessionName;

			// Validate input
			if (string.IsNullOrWhiteSpace(playerName))
			{
				Debug.LogError("Player name cannot be empty!");
				ShowError("Please enter a player name");
				PlayUISound(UISoundType.Error);
				return;
			}

			if (string.IsNullOrWhiteSpace(sessionName))
			{
				Debug.LogError("Session name cannot be empty!");
				ShowError("Please enter a session name");
				PlayUISound(UISoundType.Error);
				return;
			}

			// Save preferences
			PlayerPrefs.SetString("PlayerName", playerName);
			PlayerPrefs.SetString("LastSession", sessionName);
			PlayerPrefs.Save();

			// Disable buttons while connecting
			_isConnecting = true;
			SetButtonsEnabled(false);

			// Fire event to connect
			try
			{
				KoboldEventHandler.StartSocialHubPressed(playerName, sessionName);
				PlayUISound(UISoundType.Click);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{name}] Error starting social hub connection: {ex}");
				_isConnecting = false;
				SetButtonsEnabled(true);
				ShowError("Failed to start connection");
			}
		}

		private void OpenQuickMission()
		{
			if (_isConnecting)
			{
				Debug.LogWarning($"[{name}] Already connecting to a session");
				return;
			}

			// For now, just load a default player name
			var playerName = PlayerPrefs.GetString("PlayerName", _defaultPlayerName);

			if (string.IsNullOrWhiteSpace(playerName)) playerName = _defaultPlayerName;

			try
			{
				// This would open mission browser or quick join
				KoboldEventHandler.QuickMissionPressed(playerName, "QuickMatch");
				PlayUISound(UISoundType.Click);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{name}] Error starting quick mission: {ex}");
				ShowError("Failed to start quick mission");
			}
		}

		private void QuitGame()
		{
			PlayUISound(UISoundType.Click);

#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
		}

		private void SetButtonsEnabled(bool shouldEnable)
		{
			_socialHubButton?.SetEnabled(shouldEnable);
			_quickMissionButton?.SetEnabled(shouldEnable);
			_settingsButton?.SetEnabled(shouldEnable);
			_quitButton?.SetEnabled(shouldEnable);

			// Also disable/enable social hub panel buttons
			var joinHubButton = _root?.Q<Button>("join-hub-button");
			joinHubButton?.SetEnabled(shouldEnable);
		}

		private void ShowError(string message)
		{
			// TODO: Implement proper error display UI
			Debug.LogError($"[{name}] UI Error: {message}");

			// For now, just log the error
			// In a full implementation, this would show an error popup or notification
		}

		private void PlayUISound(UISoundType soundType)
		{
			if (!Application.isPlaying)
				return;

			try
			{
				// Only access Instance if we're not shutting down
				var themeManager = FindFirstObjectByType<KoboldThemeManager>();
				themeManager?.PlayUISound(soundType);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[{name}] Failed to play UI sound: {ex.Message}");
			}
		}

#region Editor Support

#if UNITY_EDITOR
		[ContextMenu("Force Refresh UI")]
		private void ForceRefreshUI()
		{
			if (Application.isPlaying && _isInitialized)
			{
				UnregisterCallbacks();
				_isInitialized = false;
				OnDisable();
				OnEnable();
			}
		}
#endif

#endregion
	}
}
