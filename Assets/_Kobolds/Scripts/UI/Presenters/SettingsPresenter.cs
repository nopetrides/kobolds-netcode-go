using System;
using System.Collections.Generic;
using System.Linq;
using Kobold.UI.Components;
using Kobold.UI.Configuration;
using Kobold.UI.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Presenters
{
	/// <summary>
	///     Settings presenter
	/// </summary>
	public class SettingsPresenter : IUIPresenter
	{
		private KoboldUIConfiguration _config;
		private Slider _footstepsVolumeSlider;
		private DropdownField _fullscreenDropdown;

		// Control references
		private Slider _masterVolumeSlider;
		private Slider _musicVolumeSlider;

		// Store original values for cancel functionality
		private Dictionary<string, float> _originalAudioValues;
		private int _originalFullscreenMode;
		private int _originalQuality;
		private string _originalResolution;
		private int _originalTheme;
		private DropdownField _qualityDropdown;
		private DropdownField _resolutionDropdown;
		private VisualElement _root;
		private VisualElement _settings;
		private Slider _sfxVolumeSlider;
		private DropdownField _themeDropdown;

		public void Initialize(VisualElement root, KoboldUIConfiguration config)
		{
			_root = root;
			_config = config;

			// Find the settings window
			_settings = _root.Q<VisualElement>("settings-window");
			if (_settings == null)
			{
				Debug.LogError("[SettingsPresenter] Could not find settings-window element!");
				return;
			}

			// Get all control references
			CacheControls();

			// Setup dropdowns
			SetupDropdowns();

			// Bind buttons
			BindButton("back-button", OnBackClicked);
			BindButton("apply-button", OnApplyClicked);
			BindButton("reset-button", OnResetClicked);

			// Add real-time preview for audio sliders
			_masterVolumeSlider?.RegisterValueChangedCallback(evt => PreviewAudioVolume("Master", evt.newValue));
			_musicVolumeSlider?.RegisterValueChangedCallback(evt => PreviewAudioVolume("Music", evt.newValue));
			_sfxVolumeSlider?.RegisterValueChangedCallback(evt => PreviewAudioVolume("SFX", evt.newValue));
			_footstepsVolumeSlider?.RegisterValueChangedCallback(evt => PreviewAudioVolume("Footsteps", evt.newValue));
		}

		public void OnShow()
		{
			LoadCurrentSettings();
			StoreOriginalValues();
		}

		public void OnHide()
		{
			// Could revert to original values here if not applied
		}

		public void Cleanup()
		{
			// Cleanup if needed
		}

		private void CacheControls()
		{
			// Audio sliders
			_masterVolumeSlider = _settings.Q<Slider>("master-volume-slider");
			_musicVolumeSlider = _settings.Q<Slider>("music-volume-slider");
			_sfxVolumeSlider = _settings.Q<Slider>("sfx-volume-slider");
			_footstepsVolumeSlider = _settings.Q<Slider>("footsteps-volume-slider");

			// Video dropdowns
			_fullscreenDropdown = _settings.Q<DropdownField>("fullscreen-dropdown");
			_resolutionDropdown = _settings.Q<DropdownField>("resolution-dropdown");
			_qualityDropdown = _settings.Q<DropdownField>("quality-dropdown");

			// Theme dropdown
			_themeDropdown = _settings.Q<DropdownField>("theme-dropdown");
		}

		private void SetupDropdowns()
		{
			// Fullscreen modes
			if (_fullscreenDropdown != null)
				_fullscreenDropdown.choices = new List<string> {"Windowed", "Fullscreen", "Borderless Window"};

			// Available resolutions
			if (_resolutionDropdown != null) _resolutionDropdown.choices = GetAvailableResolutions();

			// Quality levels
			if (_qualityDropdown != null) _qualityDropdown.choices = QualitySettings.names.ToList();

			// Available themes
			if (_themeDropdown != null)
			{
				var themeManager = KoboldThemeManager.Instance;
				if (themeManager != null) _themeDropdown.choices = themeManager.GetThemeNames();
			}
		}

		private void LoadCurrentSettings()
		{
			// Audio
			_masterVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 100f));
			_musicVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 80f));
			_sfxVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 100f));
			_footstepsVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("FootstepsVolume", 70f));

			// Video
			if (_fullscreenDropdown != null) _fullscreenDropdown.index = GetFullscreenModeIndex();

			if (_resolutionDropdown != null)
				_resolutionDropdown.value = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";

			if (_qualityDropdown != null) _qualityDropdown.index = QualitySettings.GetQualityLevel();

			// Theme
			if (_themeDropdown != null)
			{
				var themeManager = KoboldThemeManager.Instance;
				if (themeManager != null) _themeDropdown.index = themeManager.GetCurrentThemeIndex();
			}
		}

		private void StoreOriginalValues()
		{
			_originalAudioValues = new Dictionary<string, float>
			{
				["Master"] = _masterVolumeSlider?.value ?? 100f,
				["Music"] = _musicVolumeSlider?.value ?? 80f,
				["SFX"] = _sfxVolumeSlider?.value ?? 100f,
				["Footsteps"] = _footstepsVolumeSlider?.value ?? 70f
			};

			_originalFullscreenMode = _fullscreenDropdown?.index ?? 0;
			_originalResolution = _resolutionDropdown?.value ?? "1920x1080";
			_originalQuality = _qualityDropdown?.index ?? 2;
			_originalTheme = _themeDropdown?.index ?? 0;
		}

		private void OnBackClicked()
		{
			PlayClickSound();
			KoboldUISystem.Instance.ShowMenu(KoboldMenu.MainMenu);
		}

		private void OnApplyClicked()
		{
			// Save audio settings
			PlayerPrefs.SetFloat("MasterVolume", _masterVolumeSlider?.value ?? 100f);
			PlayerPrefs.SetFloat("MusicVolume", _musicVolumeSlider?.value ?? 80f);
			PlayerPrefs.SetFloat("SFXVolume", _sfxVolumeSlider?.value ?? 100f);
			PlayerPrefs.SetFloat("FootstepsVolume", _footstepsVolumeSlider?.value ?? 70f);

			// Apply video settings
			ApplyVideoSettings();

			// Apply theme
			if (_themeDropdown != null) KoboldThemeManager.Instance?.SetThemeByIndex(_themeDropdown.index);

			PlayerPrefs.Save();

			// Update stored original values
			StoreOriginalValues();

			PlaySuccessSound();
			Debug.Log("[SettingsPresenter] Settings applied!");
		}

		private void OnResetClicked()
		{
			// Reset to default values
			_masterVolumeSlider?.SetValueWithoutNotify(100f);
			_musicVolumeSlider?.SetValueWithoutNotify(80f);
			_sfxVolumeSlider?.SetValueWithoutNotify(100f);
			_footstepsVolumeSlider?.SetValueWithoutNotify(70f);

			if (_fullscreenDropdown != null) _fullscreenDropdown.index = 0;
			if (_qualityDropdown != null) _qualityDropdown.index = 2;
			if (_themeDropdown != null) _themeDropdown.index = 0;
			if (_resolutionDropdown != null) _resolutionDropdown.value = "1920x1080";

			PlayClickSound();
			Debug.Log("[SettingsPresenter] Settings reset to defaults!");
		}

		private void ApplyVideoSettings()
		{
			if (_fullscreenDropdown != null && _resolutionDropdown != null)
			{
				var fullscreenMode = _fullscreenDropdown.index switch
				{
					0 => FullScreenMode.Windowed,
					1 => FullScreenMode.FullScreenWindow,
					2 => FullScreenMode.MaximizedWindow,
					_ => FullScreenMode.Windowed
				};

				var resolutionStr = _resolutionDropdown.value;
				var parts = resolutionStr.Split('x');
				if (parts.Length == 2 &&
					int.TryParse(parts[0], out var width) &&
					int.TryParse(parts[1], out var height))
					Screen.SetResolution(width, height, fullscreenMode);
			}

			if (_qualityDropdown != null) QualitySettings.SetQualityLevel(_qualityDropdown.index);
		}

		private void PreviewAudioVolume(string volumeType, float value)
		{
			// This would connect to your audio system to preview volume changes
			// For example: KoboldAudioManager.Instance?.SetVolume(volumeType, value / 100f);
			Debug.Log($"[SettingsPresenter] Preview {volumeType} volume: {value}%");
		}

		private List<string> GetAvailableResolutions()
		{
			return Screen.resolutions
				.Select(r => $"{r.width}x{r.height}")
				.Distinct()
				.OrderByDescending(r =>
				{
					var parts = r.Split('x');
					return int.Parse(parts[0]) * int.Parse(parts[1]);
				})
				.ToList();
		}

		private int GetFullscreenModeIndex()
		{
			return Screen.fullScreenMode switch
			{
				FullScreenMode.Windowed => 0,
				FullScreenMode.FullScreenWindow => 1,
				FullScreenMode.MaximizedWindow => 2,
				_ => 0
			};
		}

		private void BindButton(string buttonName, Action action)
		{
			// Find the KoboldButtonElement
			var koboldButtonElement = _settings.Q<VisualElement>(buttonName);

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
			var button = _settings.Q<Button>(buttonName);
			if (button != null)
				button.clicked += action;
			else
				Debug.LogWarning($"[SettingsPresenter] Button '{buttonName}' not found!");
		}

		private void PlayClickSound()
		{
			if (_config.enableUISounds) KoboldUISystem.Instance.PlayUISound(UISoundType.Click);
		}

		private void PlaySuccessSound()
		{
			if (_config.enableUISounds) KoboldUISystem.Instance.PlayUISound(UISoundType.Success);
		}
	}
}
