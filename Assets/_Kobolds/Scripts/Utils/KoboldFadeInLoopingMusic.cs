using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KoboldFadeInLoopingMusic : MonoBehaviour
{
	[SerializeField] private AudioSource _musicSource;
	[SerializeField] private float fadeInDuration = 2f;

	private AudioSource _audioSource;
	private float _fadeTimer = 0f;

	void Awake()
	{
		_audioSource = GetComponent<AudioSource>();
		_audioSource.loop = true;
		_audioSource.volume = 0f;
		_audioSource.Play();
	}

	void Update()
	{
		if (_fadeTimer < fadeInDuration)
		{
			_fadeTimer += Time.deltaTime;
			_audioSource.volume = Mathf.Clamp01(_fadeTimer / fadeInDuration);
		}
	}
}
