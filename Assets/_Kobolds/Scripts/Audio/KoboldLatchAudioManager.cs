using UnityEngine;

namespace Kobold.Audio
{
	/// <summary>
	/// Handles audio feedback for latch state changes.
	/// </summary>
	public class KoboldLatchAudioManager : MonoBehaviour
	{
		[Header("Audio Sources")]
		[SerializeField] private AudioSource _audioSource;
		
		[Header("Latch State Audio")]
		[SerializeField] private AudioClip _mouthOpenSound;
		[SerializeField] private AudioClip _mouthCloseSound;
		[SerializeField] private AudioClip _latchStartSound;
		[SerializeField] private AudioClip _latchEndSound;
		
		[Header("Volume Settings")]
		[SerializeField] private float _mouthOpenVolume = 0.5f;
		[SerializeField] private float _mouthCloseVolume = 0.3f;
		[SerializeField] private float _latchStartVolume = 0.7f;
		[SerializeField] private float _latchEndVolume = 0.5f;

		private KoboldLatcher _latcher;

		private void Start()
		{
			// Get or create audio source
			if (_audioSource == null)
				_audioSource = GetComponent<AudioSource>();
			
			if (_audioSource == null)
				_audioSource = gameObject.AddComponent<AudioSource>();

			// Find latcher component
			_latcher = GetComponent<KoboldLatcher>();
			
			if (_latcher != null)
				_latcher.OnLatchStateChanged += OnLatchStateChanged;
		}

		private void OnDestroy()
		{
			if (_latcher != null)
				_latcher.OnLatchStateChanged -= OnLatchStateChanged;
		}

		private void OnLatchStateChanged(LatchState newState)
		{
			switch (newState)
			{
				case LatchState.Open:
					PlaySound(_mouthOpenSound, _mouthOpenVolume);
					break;
					
				case LatchState.None:
					PlaySound(_mouthCloseSound, _mouthCloseVolume);
					break;
					
				case LatchState.Gnawing:
					PlaySound(_latchStartSound, _latchStartVolume);
					break;
			}
		}

		/// <summary>
		/// Plays a sound with the specified volume.
		/// </summary>
		private void PlaySound(AudioClip clip, float volume)
		{
			if (clip == null || _audioSource == null) return;
			
			_audioSource.PlayOneShot(clip, volume);
		}

		/// <summary>
		/// Manually trigger latch end sound (called when detaching).
		/// </summary>
		public void PlayLatchEndSound()
		{
			PlaySound(_latchEndSound, _latchEndVolume);
		}
	}
}