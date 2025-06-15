using System;
using Kobold.GameManagement;
using Kobold.UI.Components;
using Kobold.UI.Configuration;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Presenters
{
	/// <summary>
	///     Social hub presenter
	/// </summary>
	public class SocialHubPresenter : IUIPresenter
	{
		private KoboldUIConfiguration _config;
		private TextField _playerNameField;
		private VisualElement _root;
		private TextField _sessionNameField;
		private VisualElement _socialHub;

		public void Initialize(VisualElement root, KoboldUIConfiguration config)
		{
			_root = root;
			_config = config;

			// Find the social hub window
			_socialHub = _root.Q<VisualElement>("social-hub-window");
			if (_socialHub == null)
			{
				Debug.LogError("[SocialHubPresenter] Could not find social-hub-window element!");
				return;
			}

			// Get text field references
			_playerNameField = _socialHub.Q<TextField>("player-name-field");
			_sessionNameField = _socialHub.Q<TextField>("session-name-field");

			// Set default values
			LoadDefaults();

			// Bind buttons
			BindButton(
				"back-button", () =>
				{
					PlayClickSound();
					KoboldUISystem.Instance.ShowMenu(KoboldMenu.MainMenu);
				});

			BindButton("join-hub-button", OnJoinClicked);

			// Optional: Add enter key support for text fields
			if (_playerNameField != null)
				_playerNameField.RegisterCallback<KeyDownEvent>(evt =>
				{
					if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) _sessionNameField?.Focus();
				});

			if (_sessionNameField != null)
				_sessionNameField.RegisterCallback<KeyDownEvent>(evt =>
				{
					if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) OnJoinClicked();
				});
		}

		public void OnShow()
		{
			// Refresh defaults when showing
			LoadDefaults();

			// Focus player name field
			_playerNameField?.schedule.Execute(() => _playerNameField.Focus()).ExecuteLater(100);
		}

		public void OnHide()
		{
			// Save current values as preferences
			if (_playerNameField != null && !string.IsNullOrWhiteSpace(_playerNameField.value))
				PlayerPrefs.SetString("PlayerName", _playerNameField.value);

			if (_sessionNameField != null && !string.IsNullOrWhiteSpace(_sessionNameField.value))
				PlayerPrefs.SetString("LastSession", _sessionNameField.value);
		}

		public void Cleanup()
		{
			// Cleanup if needed
		}

		private void LoadDefaults()
		{
			if (_playerNameField != null)
				_playerNameField.value = PlayerPrefs.GetString("PlayerName", _config.defaultPlayerName);

			if (_sessionNameField != null)
				_sessionNameField.value = PlayerPrefs.GetString("LastSession", _config.defaultSessionName);
		}

		private void OnJoinClicked()
		{
			var playerName = _playerNameField?.value ?? _config.defaultPlayerName;
			var sessionName = _sessionNameField?.value ?? _config.defaultSessionName;

			// Validate input
			if (string.IsNullOrWhiteSpace(playerName))
			{
				Debug.LogError("[SocialHubPresenter] Player name cannot be empty!");
				PlayErrorSound();
				_playerNameField?.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(sessionName))
			{
				Debug.LogError("[SocialHubPresenter] Session name cannot be empty!");
				PlayErrorSound();
				_sessionNameField?.Focus();
				return;
			}

			// Save preferences
			PlayerPrefs.SetString("PlayerName", playerName);
			PlayerPrefs.SetString("LastSession", sessionName);
			PlayerPrefs.Save();

			PlayClickSound();

			// Fire event to start connection
			KoboldEventHandler.StartSocialHubPressed(playerName, sessionName);
		}

		private void BindButton(string buttonName, Action action)
		{
			// Find the KoboldButtonElement
			var koboldButtonElement = _socialHub.Q<VisualElement>(buttonName);

			if (koboldButtonElement != null)
			{
				var koboldButton = koboldButtonElement.Q<KoboldButton>();
				if (koboldButton != null)
				{
					koboldButton.Clicked += action;
					return;
				}
			}

			// Fallback to standard button
			var button = _socialHub.Q<Button>(buttonName);
			if (button != null)
				button.clicked += action;
			else
				Debug.LogWarning($"[SocialHubPresenter] Button '{buttonName}' not found!");
		}

		private void PlayClickSound()
		{
			if (_config.enableUISounds) KoboldUISystem.Instance.PlayUISound(UISoundType.Click);
		}

		private void PlayErrorSound()
		{
			if (_config.enableUISounds) KoboldUISystem.Instance.PlayUISound(UISoundType.Error);
		}
	}
}
