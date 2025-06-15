using Unity.BossRoom.Audio;
using Unity.BossRoom.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
	public class UISettingsPanel : MonoBehaviour
	{
		[SerializeField]
		private Slider m_MasterVolumeSlider;

		[SerializeField]
		private Slider m_MusicVolumeSlider;

		[SerializeField]
		private Slider m_SfxVolumeSlider;

		[SerializeField]
		private Slider m_FootstepsVolumeSlider;

		private void OnEnable()
		{
			// Note that we initialize the slider BEFORE we listen for changes (so we don't get notified of our own change!)
			m_MasterVolumeSlider.value = ClientPrefs.GetMasterVolume();
			m_MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);

			// initialize music slider similarly.
			m_MusicVolumeSlider.value = ClientPrefs.GetMusicVolume();
			m_MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);

			m_SfxVolumeSlider.value = ClientPrefs.GetSfxVolume();
			m_MusicVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);

			m_FootstepsVolumeSlider.value = ClientPrefs.GetMusicVolume();
			m_FootstepsVolumeSlider.onValueChanged.AddListener(OnFootstepsVolumeSliderChanged);
		}

		private void OnDisable()
		{
			m_MasterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);
			m_MusicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
			m_SfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
			m_FootstepsVolumeSlider.onValueChanged.RemoveListener(OnFootstepsVolumeSliderChanged);
		}

		private void OnMasterVolumeSliderChanged(float newValue)
		{
			ClientPrefs.SetMasterVolume(newValue);
			AudioMixerConfigurator.Instance.Configure();
		}

		private void OnMusicVolumeSliderChanged(float newValue)
		{
			ClientPrefs.SetMusicVolume(newValue);
			AudioMixerConfigurator.Instance.Configure();
		}

		private void OnSfxVolumeSliderChanged(float newValue)
		{
			ClientPrefs.SetSfxVolume(newValue);
			AudioMixerConfigurator.Instance.Configure();
		}

		private void OnFootstepsVolumeSliderChanged(float newValue)
		{
			ClientPrefs.SetFootstepsVolume(newValue);
			AudioMixerConfigurator.Instance.Configure();
		}
	}
}
