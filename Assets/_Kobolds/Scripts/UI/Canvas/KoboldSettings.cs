using System;
using System.Collections.Generic;
using Kobold.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kobold.UI
{
	public class KoboldSettings : MonoBehaviour
	{
		[SerializeField] private Button _closeButton;
		
		[Header("Audio")]
		[SerializeField] private Toggle _muteAllToggle;
		[SerializeField] private Slider _masterVolumeSlider;
		[SerializeField] private Slider _musicVolumeSlider;
		[SerializeField] private Slider _sfxVolumeSlider;
		//[SerializeField] private Slider _footstepsVolumeSlider;

		[Header("Graphics")]
		[SerializeField] private TMP_Dropdown _fullscreenDropdown;
		[SerializeField] private TMP_Dropdown _resolutionDropdown;
		[SerializeField] private TMP_Dropdown _qualityDropdown;

		public Action OnClose;

		private void Awake()
		{
			_qualityDropdown.onValueChanged.AddListener(QualitySettings.SetQualityLevel);
			_fullscreenDropdown.onValueChanged.AddListener(index =>
			{
				var mode = (FullScreenMode) index;
				Screen.fullScreenMode = mode;
			});

			_resolutionDropdown.onValueChanged.AddListener(_ =>
			{
				var res = Screen.resolutions[_resolutionDropdown.value];
				Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
			});

			PopulateDropdowns();
		}

		private void OnEnable()
		{
			_closeButton.onClick.AddListener(OnExitButton);
			
			// Restore saved values
			_masterVolumeSlider.value = KoboldPrefs.GetMasterVolume();
			_musicVolumeSlider.value = KoboldPrefs.GetMusicVolume();
			_sfxVolumeSlider.value = KoboldPrefs.GetSfxVolume();
			//_footstepsVolumeSlider.value = KoboldPrefs.GetFootstepsVolume();
			_muteAllToggle.isOn = KoboldPrefs.IsMuted();
			
			_masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);
			_musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
			_sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);
			//_footstepsVolumeSlider.onValueChanged.AddListener(OnFootstepsVolumeSliderChanged);
			_muteAllToggle.onValueChanged.AddListener(OnMuteAllToggled);
			
			_closeButton.Select();
			UISelectionIndicator.LastValidSelectable = _closeButton.gameObject;
		}

		private void OnDisable()
		{
			_muteAllToggle.onValueChanged.RemoveListener(OnMuteAllToggled);
			_masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);
			_musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
			_sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
			//_footstepsVolumeSlider.onValueChanged.RemoveListener(OnFootstepsVolumeSliderChanged);
			_closeButton.onClick.RemoveListener(OnExitButton);
		}

		private void PopulateDropdowns()
		{
			// --- Fullscreen Modes ---
			_fullscreenDropdown.ClearOptions();
			var fullscreenOptions = new List<string>();
			foreach (FullScreenMode mode in Enum.GetValues(typeof(FullScreenMode)))
				fullscreenOptions.Add(mode.ToString());
			_fullscreenDropdown.AddOptions(fullscreenOptions);

			// Match current fullscreen mode
			_fullscreenDropdown.value = fullscreenOptions.IndexOf(Screen.fullScreenMode.ToString());
			_fullscreenDropdown.RefreshShownValue();

			// --- Quality Settings ---
			_qualityDropdown.ClearOptions();
			var qualityOptions = new List<string>(QualitySettings.names);
			_qualityDropdown.AddOptions(qualityOptions);
			_qualityDropdown.value = QualitySettings.GetQualityLevel();
			_qualityDropdown.RefreshShownValue();

			// --- Resolutions ---
			_resolutionDropdown.ClearOptions();
			var resolutionOptions = new List<string>();
			var resolutions = Screen.resolutions;

			foreach (var res in resolutions)
			{
				var hz = (int) Math.Round(res.refreshRateRatio.value);
				resolutionOptions.Add($"{res.width} x {res.height} @ {hz}Hz");
			}

			_resolutionDropdown.AddOptions(resolutionOptions);

			// Select current resolution
			var currentIndex = Array.FindIndex(
				resolutions, r =>
					r.width == Screen.currentResolution.width &&
					r.height == Screen.currentResolution.height &&
					Mathf.Approximately(
						(float) r.refreshRateRatio.value, (float) Screen.currentResolution.refreshRateRatio.value));

			_resolutionDropdown.value = Mathf.Clamp(currentIndex, 0, resolutionOptions.Count - 1);
			_resolutionDropdown.RefreshShownValue();
		}
		
		private void OnMuteAllToggled(bool isMuted)
		{
			KoboldPrefs.SetMuted(isMuted); // Implement this in KoboldPrefs if not already
			KoboldAudio.Instance.Configure();
		}

		private void OnMasterVolumeSliderChanged(float newValue)
		{
			KoboldPrefs.SetMasterVolume(newValue);
			KoboldAudio.Instance.Configure();
		}

		private void OnMusicVolumeSliderChanged(float newValue)
		{
			KoboldPrefs.SetMusicVolume(newValue);
			KoboldAudio.Instance.Configure();
		}
		
		private void OnSfxVolumeSliderChanged(float newValue)
		{
			KoboldPrefs.SetSfxVolume(newValue);
			KoboldAudio.Instance.Configure();
		}

		private void OnExitButton()
		{
			OnClose?.Invoke();
		}
	}
}
