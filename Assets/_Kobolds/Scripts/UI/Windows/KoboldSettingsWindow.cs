using System.Collections.Generic;
using System.Linq;
using Kobold.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Windows
{
	/// <summary>
	///     Settings window with audio, video, and theme options
	/// </summary>
	public class KoboldSettingsWindow : KoboldWindow
	{
		private KoboldButton _applyButton;

		// Buttons
		private KoboldButton _backButton;
		private KoboldSlider _footstepsVolumeSlider;

		// Video controls
		private KoboldDropdown _fullscreenDropdown;

		// Audio controls
		private KoboldSlider _masterVolumeSlider;
		private KoboldSlider _musicVolumeSlider;
		private KoboldDropdown _qualityDropdown;
		private KoboldButton _resetButton;
		private KoboldDropdown _resolutionDropdown;
		private KoboldSlider _sfxVolumeSlider;

		// Theme controls
		private KoboldDropdown _themeDropdown;

		public KoboldSettingsWindow() : base("Settings")
		{
			BuildUI();
			LoadCurrentSettings();
		}

		private void BuildUI()
		{
			// Back button
			_backButton = new KoboldButton("← Back");
			_backButton.AddToClassList("back-button");
			_backButton.AnimationDelay = 0.05f;
			_backButton.Clicked += OnBackClicked;
			ContentContainer.Add(_backButton);

			// Title
			var title = new Label("Settings");
			title.AddToClassList("kobold-title");
			ContentContainer.Add(title);

			// Create sections
			CreateAudioSection();
			CreateVideoSection();
			CreateThemeSection();

			// Button row
			CreateButtonRow();
		}

		private void CreateAudioSection()
		{
			var audioContainer = new VisualElement();
			audioContainer.AddToClassList("kobold-container");
			audioContainer.AddToClassList("outlined");

			// Section header
			var audioTitle = new Label("Audio");
			audioTitle.AddToClassList("kobold-subtitle");
			audioContainer.Add(audioTitle);

			// Master Volume
			_masterVolumeSlider = new KoboldSlider("Master Volume")
			{
				MinValue = 0f,
				MaxValue = 100f,
				ValueFormat = "{0:0}%",
				AnimationDelay = 0.1f
			};
			audioContainer.Add(_masterVolumeSlider);

			// Music Volume
			_musicVolumeSlider = new KoboldSlider("Music Volume")
			{
				MinValue = 0f,
				MaxValue = 100f,
				ValueFormat = "{0:0}%",
				AnimationDelay = 0.15f
			};
			audioContainer.Add(_musicVolumeSlider);

			// SFX Volume
			_sfxVolumeSlider = new KoboldSlider("SFX Volume")
			{
				MinValue = 0f,
				MaxValue = 100f,
				ValueFormat = "{0:0}%",
				AnimationDelay = 0.2f
			};
			audioContainer.Add(_sfxVolumeSlider);

			// Footsteps Volume
			_footstepsVolumeSlider = new KoboldSlider("Footsteps Volume")
			{
				MinValue = 0f,
				MaxValue = 100f,
				ValueFormat = "{0:0}%",
				AnimationDelay = 0.25f
			};
			audioContainer.Add(_footstepsVolumeSlider);

			ContentContainer.Add(audioContainer);

			var spacer = new VisualElement();
			spacer.AddToClassList("kobold-spacer-medium");
			ContentContainer.Add(spacer);
		}

		private void CreateVideoSection()
		{
			var videoContainer = new VisualElement();
			videoContainer.AddToClassList("kobold-container");
			videoContainer.AddToClassList("outlined");

			// Section header
			var videoTitle = new Label("Video");
			videoTitle.AddToClassList("kobold-subtitle");
			videoContainer.Add(videoTitle);

			// Fullscreen Mode
			var fullscreenOptions = new List<string>
			{
				"Windowed",
				"Fullscreen",
				"Borderless Window"
			};

			_fullscreenDropdown = new KoboldDropdown("Screen Mode", fullscreenOptions)
			{
				AnimationDelay = 0.3f
			};
			videoContainer.Add(_fullscreenDropdown);

			// Resolution
			var resolutions = GetAvailableResolutions();
			_resolutionDropdown = new KoboldDropdown("Resolution", resolutions)
			{
				AnimationDelay = 0.35f
			};
			videoContainer.Add(_resolutionDropdown);

			// Quality
			var qualityOptions = new List<string>();
			for (var i = 0; i < QualitySettings.names.Length; i++) qualityOptions.Add(QualitySettings.names[i]);

			_qualityDropdown = new KoboldDropdown("Graphics Quality", qualityOptions)
			{
				AnimationDelay = 0.4f
			};
			videoContainer.Add(_qualityDropdown);

			ContentContainer.Add(videoContainer);

			var spacer = new VisualElement();
			spacer.AddToClassList("kobold-spacer-medium");
			ContentContainer.Add(spacer);
		}

		private void CreateThemeSection()
		{
			var themeContainer = new VisualElement();
			themeContainer.AddToClassList("kobold-container");
			themeContainer.AddToClassList("outlined");

			// Section header
			var themeTitle = new Label("UI Theme");
			themeTitle.AddToClassList("kobold-subtitle");
			themeContainer.Add(themeTitle);

			// Theme dropdown
			var themeOptions = new List<string>
			{
				"Dark Theme",
				"Light Theme",
				"High Contrast",
				"Kobold Classic"
			};

			_themeDropdown = new KoboldDropdown("Select Theme", themeOptions)
			{
				AnimationDelay = 0.45f
			};
			themeContainer.Add(_themeDropdown);

			ContentContainer.Add(themeContainer);

			var spacer = new VisualElement();
			spacer.AddToClassList("kobold-spacer-large");
			ContentContainer.Add(spacer);
		}

		private void CreateButtonRow()
		{
			var buttonRow = new VisualElement();
			buttonRow.AddToClassList("button-row");
			buttonRow.style.flexDirection = FlexDirection.Row;
			buttonRow.style.justifyContent = Justify.Center;

			// Reset button
			_resetButton = new KoboldButton("Reset to Defaults");
			_resetButton.AnimationDelay = 0.5f;
			_resetButton.Clicked += OnResetClicked;
			buttonRow.Add(_resetButton);

			var spacer = new VisualElement();
			spacer.style.width = 20;
			buttonRow.Add(spacer);

			// Apply button
			_applyButton = new KoboldButton("Apply Settings");
			_applyButton.AddToClassList("primary");
			_applyButton.AnimationDelay = 0.55f;
			_applyButton.Clicked += OnApplySettings;
			buttonRow.Add(_applyButton);

			ContentContainer.Add(buttonRow);
		}

		private void LoadCurrentSettings()
		{
			// Load audio settings
			_masterVolumeSlider.Value = PlayerPrefs.GetFloat("MasterVolume", 100f);
			_musicVolumeSlider.Value = PlayerPrefs.GetFloat("MusicVolume", 80f);
			_sfxVolumeSlider.Value = PlayerPrefs.GetFloat("SFXVolume", 100f);
			_footstepsVolumeSlider.Value = PlayerPrefs.GetFloat("FootstepsVolume", 70f);

			// Load video settings
			_fullscreenDropdown.Index = Screen.fullScreenMode == FullScreenMode.Windowed ? 0 :
				Screen.fullScreenMode == FullScreenMode.FullScreenWindow ? 1 : 2;

			// Set current resolution
			var currentRes = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
			_resolutionDropdown.Value = currentRes;

			// Set quality level
			_qualityDropdown.Index = QualitySettings.GetQualityLevel();

			// Load theme setting
			_themeDropdown.Index = PlayerPrefs.GetInt("UITheme", 0);
		}

		private List<string> GetAvailableResolutions()
		{
			var resolutions = new List<string>();
			var uniqueResolutions = Screen.resolutions
				.Select(r => $"{r.width}x{r.height}")
				.Distinct()
				.OrderByDescending(r =>
				{
					var parts = r.Split('x');
					return int.Parse(parts[0]) * int.Parse(parts[1]);
				})
				.ToList();

			return uniqueResolutions.Any() ? uniqueResolutions : new List<string> {"1920x1080", "1280x720"};
		}

		private void OnBackClicked()
		{
			KoboldWindowManager.Instance.NavigateBack();
		}

		private void OnApplySettings()
		{
			// Save audio settings
			PlayerPrefs.SetFloat("MasterVolume", _masterVolumeSlider.Value);
			PlayerPrefs.SetFloat("MusicVolume", _musicVolumeSlider.Value);
			PlayerPrefs.SetFloat("SFXVolume", _sfxVolumeSlider.Value);
			PlayerPrefs.SetFloat("FootstepsVolume", _footstepsVolumeSlider.Value);

			// Apply video settings
			var fullscreenMode = _fullscreenDropdown.Index switch
			{
				0 => FullScreenMode.Windowed,
				1 => FullScreenMode.FullScreenWindow,
				2 => FullScreenMode.MaximizedWindow,
				_ => FullScreenMode.Windowed
			};

			// Parse and apply resolution
			var resolutionStr = _resolutionDropdown.Value;
			var parts = resolutionStr.Split('x');
			if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
				Screen.SetResolution(width, height, fullscreenMode);

			// Apply quality
			QualitySettings.SetQualityLevel(_qualityDropdown.Index);

			// Save theme setting
			PlayerPrefs.SetInt("UITheme", _themeDropdown.Index);

			PlayerPrefs.Save();

			// Apply theme change
			ApplyTheme(_themeDropdown.Index);

			Debug.Log("Settings applied successfully!");

			// TODO: Apply audio settings to audio system
			// TODO: Show confirmation UI
		}

		private void OnResetClicked()
		{
			// Reset to default values
			_masterVolumeSlider.Value = 100f;
			_musicVolumeSlider.Value = 80f;
			_sfxVolumeSlider.Value = 100f;
			_footstepsVolumeSlider.Value = 70f;

			_fullscreenDropdown.Index = 0;
			_qualityDropdown.Index = 2; // Assuming "High" is at index 2
			_themeDropdown.Index = 0; // Dark theme

			// Set default resolution
			_resolutionDropdown.Value = "1920x1080";

			Debug.Log("Settings reset to defaults!");
		}

		private void ApplyTheme(int themeIndex)
		{
			// TODO: Implement theme switching
			var themeName = _themeDropdown.Choices[themeIndex];
			Debug.Log($"Applying theme: {themeName}");

			// Example: KoboldThemeManager.Instance?.SetTheme(themeIndex);
		}
	}
}
