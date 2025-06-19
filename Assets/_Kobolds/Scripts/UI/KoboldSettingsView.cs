using System;
using System.Collections.Generic;
using System.Linq;
using Kobold.UI.Components;
using Kobold.UI.Configuration;
using Kobold.UI.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI
{
	/*public class KoboldSettingsView : KoboldUIView
	{
		[SerializeField] private bool _closeOnApply = true;

		private Slider _masterVolumeSlider;
		private Slider _musicVolumeSlider;
		private Slider _sfxVolumeSlider;
		private Slider _footstepsVolumeSlider;

		private DropdownField _fullscreenDropdown;
		private DropdownField _resolutionDropdown;
		private DropdownField _qualityDropdown;
		private DropdownField _themeDropdown;

		private Dictionary<string, float> _originalAudioValues;
		private int _originalFullscreenMode;
		private int _originalQuality;
		private string _originalResolution;
		private int _originalTheme;

		private KoboldUIConfiguration _config;

		public void Inject(KoboldUIConfiguration config)
		{
			_config = config;
		}

		public override void Initialize(VisualElement viewRoot)
		{
			base.Initialize(viewRoot);

			_masterVolumeSlider = MRoot.Q<Slider>("master-volume-slider");
			_musicVolumeSlider = MRoot.Q<Slider>("music-volume-slider");
			_sfxVolumeSlider = MRoot.Q<Slider>("sfx-volume-slider");
			_footstepsVolumeSlider = MRoot.Q<Slider>("footsteps-volume-slider");

			_fullscreenDropdown = MRoot.Q<DropdownField>("fullscreen-dropdown");
			_resolutionDropdown = MRoot.Q<DropdownField>("resolution-dropdown");
			_qualityDropdown = MRoot.Q<DropdownField>("quality-dropdown");
			_themeDropdown = MRoot.Q<DropdownField>("theme-dropdown");

			BindButton("apply-button", OnApplyClicked);
			BindButton("reset-button", OnResetClicked);
			BindButton("back-button", OnBackClicked);

			SetupDropdowns();
			RegisterSliderPreviews();
		}

		protected override void HandleOnShown()
		{
			LoadCurrentSettings();
			StoreOriginalValues();
		}

		private void BindButton(string name, Action callback)
		{
			var btn = MRoot.Q<Button>(name);
			if (btn != null) btn.clicked += callback;
			else Debug.LogWarning($"[KoboldSettingsView] Could not find button: {name}");
		}

		private void RegisterSliderPreviews()
		{
			_masterVolumeSlider?.RegisterValueChangedCallback(e => PreviewAudio("Master", e.newValue));
			_musicVolumeSlider?.RegisterValueChangedCallback(e => PreviewAudio("Music", e.newValue));
			_sfxVolumeSlider?.RegisterValueChangedCallback(e => PreviewAudio("SFX", e.newValue));
			_footstepsVolumeSlider?.RegisterValueChangedCallback(e => PreviewAudio("Footsteps", e.newValue));
		}

		private void SetupDropdowns()
		{
			_fullscreenDropdown.choices = new List<string> {"Windowed", "Fullscreen", "Borderless Window"};
			_resolutionDropdown.choices = Screen.resolutions.Select(r => $"{r.width}x{r.height}").Distinct().ToList();
			_qualityDropdown.choices = QualitySettings.names.ToList();
			_themeDropdown.choices = KoboldThemeManager.Instance?.GetThemeNames();
		}

		private void LoadCurrentSettings()
		{
			_masterVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 100f));
			_musicVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 80f));
			_sfxVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 100f));
			_footstepsVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("FootstepsVolume", 70f));

			_fullscreenDropdown.index = GetFullscreenModeIndex();
			_resolutionDropdown.value = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
			_qualityDropdown.index = QualitySettings.GetQualityLevel();
			_themeDropdown.index = KoboldThemeManager.Instance?.GetCurrentThemeIndex() ?? 0;
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
			_originalFullscreenMode = _fullscreenDropdown.index;
			_originalResolution = _resolutionDropdown.value;
			_originalQuality = _qualityDropdown.index;
			_originalTheme = _themeDropdown.index;
		}

		private void OnApplyClicked()
		{
			PlayerPrefs.SetFloat("MasterVolume", _masterVolumeSlider.value);
			PlayerPrefs.SetFloat("MusicVolume", _musicVolumeSlider.value);
			PlayerPrefs.SetFloat("SFXVolume", _sfxVolumeSlider.value);
			PlayerPrefs.SetFloat("FootstepsVolume", _footstepsVolumeSlider.value);

			ApplyVideoSettings();
			KoboldThemeManager.Instance?.SetThemeByIndex(_themeDropdown.index);
			PlayerPrefs.Save();

			StoreOriginalValues();
			KoboldUISystem.Instance?.PlayUISound(UISoundType.Success);
			if (_closeOnApply) Hide();
		}

		private void OnResetClicked()
		{
			_masterVolumeSlider?.SetValueWithoutNotify(100f);
			_musicVolumeSlider?.SetValueWithoutNotify(80f);
			_sfxVolumeSlider?.SetValueWithoutNotify(100f);
			_footstepsVolumeSlider?.SetValueWithoutNotify(70f);

			_fullscreenDropdown.index = 0;
			_resolutionDropdown.value = "1920x1080";
			_qualityDropdown.index = 2;
			_themeDropdown.index = 0;

			KoboldUISystem.Instance?.PlayUISound(UISoundType.Click);
		}

		private void OnBackClicked()
		{
			KoboldUISystem.Instance?.PlayUISound(UISoundType.Back);
			Hide();
		}

		private void ApplyVideoSettings()
		{
			var mode = _fullscreenDropdown.index switch
			{
				0 => FullScreenMode.Windowed,
				1 => FullScreenMode.FullScreenWindow,
				2 => FullScreenMode.MaximizedWindow,
				_ => FullScreenMode.Windowed
			};

			var parts = _resolutionDropdown.value.Split('x');
			if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
				Screen.SetResolution(width, height, mode);

			QualitySettings.SetQualityLevel(_qualityDropdown.index);
		}

		private void PreviewAudio(string type, float value)
		{
			Debug.Log($"[KoboldSettingsView] Preview {type}: {value}%");
		}
	}*/
}
