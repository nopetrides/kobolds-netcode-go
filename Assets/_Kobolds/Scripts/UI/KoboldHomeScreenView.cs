using System;
using System.Threading.Tasks;
using Kobold.GameManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI
{
	public class KoboldHomeScreenView : KoboldUIView
	{
		private Button _quickMissionButton;
		private Button _quitButton;

		private Button _settingsButton;

		// Main menu buttons
		private Button _socialHubButton;
		private Label _versionLabel;

		private bool _allowInteraction;

		private void Start()
		{
			// You can subscribe to events here if needed
		}

		private void OnDestroy()
		{
			// Unsubscribe from events
		}

		public override void Initialize(VisualElement viewRoot)
		{
			base.Initialize(viewRoot);

			// Find all buttons in the main menu
			_socialHubButton = MRoot.Q<Button>("social-hub-button");
			_quickMissionButton = MRoot.Q<Button>("quick-mission-button");
			_settingsButton = MRoot.Q<Button>("settings-button");
			_quitButton = MRoot.Q<Button>("quit-button");
			_versionLabel = MRoot.Q<Label>("version-label");

			// Set version
			if (_versionLabel != null) _versionLabel.text = $"Version {Application.version}";

			// Debug to make sure we found everything
			Debug.Log(
				$"[KoboldHomeScreenView] Buttons found - Social: {_socialHubButton != null}, Quick: {_quickMissionButton != null}, Settings: {_settingsButton != null}, Quit: {_quitButton != null}");
		}

		protected override void RegisterEvents()
		{
			if (_socialHubButton != null)
				_socialHubButton.clicked += HandleSocialHubPressed;

			if (_quickMissionButton != null)
				_quickMissionButton.clicked += HandleQuickMissionPressed;

			if (_settingsButton != null)
				_settingsButton.clicked += HandleSettingsPressed;

			if (_quitButton != null)
				_quitButton.clicked += HandleQuitPressed;

			Debug.Log("[KoboldHomeScreenView] Events registered");
		}

		protected override void UnregisterEvents()
		{
			if (_socialHubButton != null)
				_socialHubButton.clicked -= HandleSocialHubPressed;

			if (_quickMissionButton != null)
				_quickMissionButton.clicked -= HandleQuickMissionPressed;

			if (_settingsButton != null)
				_settingsButton.clicked -= HandleSettingsPressed;

			if (_quitButton != null)
				_quitButton.clicked -= HandleQuitPressed;
		}

		private void HandleSocialHubPressed()
		{
			if (!_allowInteraction) return;
			Debug.Log("[KoboldHomeScreenView] Social Hub button pressed");

			_allowInteraction = false;
			KoboldEventHandler.OnSocialHubConnectionCompleted += OnSocialHubConnected;
			// You could show a different view or fire an event
			// For now, let's use the pattern from the original with default values
			var playerName = PlayerPrefs.GetString("PlayerName", "Kobold");
			var sessionName = PlayerPrefs.GetString("LastSession", nameof(SceneNames.KoboldHub));
				
			KoboldEventHandler.StartSocialHubPressed(playerName, sessionName);
		}

		private void HandleQuickMissionPressed()
		{
			if (!_allowInteraction) return;
			Debug.Log("[KoboldHomeScreenView] Quick Mission button pressed");
			
			_allowInteraction = false;
			KoboldEventHandler.OnMissionConnectionCompleted += OnMissionConnected;
			
			var playerName = PlayerPrefs.GetString("PlayerName", "Kobold");
			KoboldEventHandler.QuickMissionPressed(playerName, "QuickMatch");
		}

		private void HandleSettingsPressed()
		{
			if (!_allowInteraction) return;
			Debug.Log("[KoboldHomeScreenView] Settings button pressed");
			
			_allowInteraction = false;
			// You'll need to implement settings view switching
			// For now just log it
			// TODO: Show settings view
			
			_allowInteraction = true;
		}

		private void OnSocialHubConnected(Task task, string sessionName)
		{
			KoboldEventHandler.OnSocialHubConnectionCompleted -= OnSocialHubConnected;
			try
			{
				if (!task.IsCompletedSuccessfully)
				{
					// allow retry
					_allowInteraction = true;
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[KoboldEventHandler.OnSocialHubConnected] Failed to connect with exception: {e}");
			}
		}

		private void OnMissionConnected(Task task, string sessionName)
		{
			KoboldEventHandler.OnMissionConnectionCompleted -= OnMissionConnected;
			
			try
			{
				if (!task.IsCompletedSuccessfully)
				{
					// allow retry
					_allowInteraction = true;
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[KoboldEventHandler.OnMissionConnected] Failed to connect with exception: {e}");
			}
		}

		private void HandleQuitPressed()
		{
			Debug.Log("[KoboldHomeScreenView] Quit button pressed");

#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
		}
	}
}
