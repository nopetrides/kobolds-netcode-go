using System.Collections.Generic;
using UnityEngine;

namespace Kobold.Bosses
{
	public class BossVFXManager : MonoBehaviour
	{
		[Header("Particles")]
		[SerializeField] private List<ParticleSystem> _toppleEffect;
		[SerializeField] private List<ParticleSystem> _recoveryEffect;
		[SerializeField] private List<ParticleSystem> _deathEffect;
		[SerializeField] private List<ParticleSystem> _coreRevealEffect;
		[SerializeField] private List<ParticleSystem> _aoePulseEffect;

		[Header("Audio")]
		[SerializeField] private AudioSource _toppleAudio;
		[SerializeField] private AudioSource _recoveryAudio;
		[SerializeField] private AudioSource _deathAudio;
		[SerializeField] private AudioSource _coreRevealAudio;
		[SerializeField] private AudioSource _aoePulseAudio;

		public void PlayToppleVFX()
		{
			foreach (var effect in _toppleEffect) effect?.Play();
			_toppleAudio?.Play();
		}

		public void PlayRecoveryVFX()
		{
			foreach (var effect in _recoveryEffect) effect?.Play();
			_recoveryAudio?.Play();
		}

		public void PlayDeathVFX()
		{
			foreach (var effect in _deathEffect) effect?.Play();
			_deathAudio?.Play();
		}

		public void PlayCoreRevealVFX()
		{
			foreach (var effect in _coreRevealEffect) effect?.Play();
			_coreRevealAudio?.Play();
		}

		public void PlayAoePulseVFX()
		{
			foreach (var effect in _aoePulseEffect) effect?.Play();
			_aoePulseAudio?.Play();
		}
	}
}
