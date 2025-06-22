using UnityEngine;
using UnityEngine.Audio;

namespace Kobold.Utils
{
	/// <summary>
	///     Initializes the game's AudioMixer to use volumes stored in preferences. Provides
	///     a public function that can be called when these values change.
	/// </summary>
	public class KoboldAudio : MonoBehaviour
	{
		/// <summary>
		///     The audio sliders use a value between 0.0001 and 1, but the mixer works in decibels -- by default, -80 to 0.
		///     To convert, we use log10(slider) multiplied by 20. Why 20? because log10(.0001)*20=-80, which is the
		///     bottom range for our mixer, meaning it's disabled.
		/// </summary>
		private const float KVolumeLog10Multiplier = 20;

		/// <summary>
		/// Manual curve for volume tuning
		/// </summary>
		[SerializeField] private AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0f, -80f, 1f, 0f);
		
		[SerializeField]
		private AudioMixer m_Mixer;

		[SerializeField]
		private string m_MixerVarMainVolume = "OverallVolume";

		[SerializeField]
		private string m_MixerVarMusicVolume = "MusicVolume";

		[SerializeField]
		private string m_MixerVarSfxVolume = "SfxVolume";

		[SerializeField]
		private string m_MixerVarFootstepsVolume = "FootstepsVolume";
		

		public static KoboldAudio Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		private void Start()
		{
			// note that trying to configure the AudioMixer during Awake does not work, must be initialized in Start
			Configure();
		}
		
		public void Configure()
		{
			bool isMuted = KoboldPrefs.IsMuted();
			m_Mixer.SetFloat(m_MixerVarMainVolume, isMuted ? -80f : GetVolumeInDecibels(KoboldPrefs.GetMasterVolume()));
			m_Mixer.SetFloat(m_MixerVarMusicVolume, GetVolumeInDecibels(KoboldPrefs.GetMusicVolume()));
			m_Mixer.SetFloat(m_MixerVarSfxVolume, GetVolumeInDecibels(KoboldPrefs.GetSfxVolume()));
			m_Mixer.SetFloat(m_MixerVarFootstepsVolume, GetVolumeInDecibels(KoboldPrefs.GetFootstepsVolume()));
		}

		private float GetVolumeInDecibels(float volume)
		{
			if (volume <= 0) // sanity-check in case we have bad prefs data
				volume = 0.0001f;
			return volumeCurve.Evaluate(volume);
		}
		
		[SerializeField] private AudioClip m_UINavigateClip;
		[SerializeField] private AudioClip m_UIClickClip;

		[SerializeField] private AudioMixerGroup m_SfxOutputGroup;

		[SerializeField] private AudioSource _uiOneShotSource;

		public static void PlayUISound(AudioClip clip)
		{
			if (clip == null || Instance == null) return;

			if (Instance._uiOneShotSource == null)
			{
				Instance._uiOneShotSource = Instance.gameObject.AddComponent<AudioSource>();
				Instance._uiOneShotSource.outputAudioMixerGroup = Instance.m_SfxOutputGroup;
				Instance._uiOneShotSource.playOnAwake = false;
			}

			Instance._uiOneShotSource.PlayOneShot(clip);
		}

		// Optional helpers
		public static void PlayUINavigateSound() => PlayUISound(Instance?.m_UINavigateClip);
		public static void PlayUIClickSound() => PlayUISound(Instance?.m_UIClickClip);
	}
}
