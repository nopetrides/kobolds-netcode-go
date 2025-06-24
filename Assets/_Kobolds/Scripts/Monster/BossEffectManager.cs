using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
// For RPCs

namespace Kobold.Bosses
{
	public enum BossEffectType
	{
		None, // Fallback for undefined effects
		StepLeft, // Animation-driven
		StepRight, // Animation-driven
		Topple, // State-driven
		Recovery, // State-driven
		Death, // State-driven
		CoreReveal, // Specific effect
		AoePulseCharge, // Specific effect
		AoePulseAttack, // Specific effect
	}

	public class BossEffectManager : NetworkBehaviour
	{
		[Header("Effects Config")]
		[SerializeField] private List<BossEffect> _effects; // All available effects (configurable in Inspector)

		[SerializeField] private MonsterBossController _bossController;

		private void Awake()
		{
			if (_bossController != null)
				_bossController.OnStateChanged += HandleStateChanged; // Subscribe to state change event
		}

		private void OnDestroy()
		{
			if (_bossController != null) _bossController.OnStateChanged -= HandleStateChanged;
		}

		/// <summary>
		///     Automatically play effects on state changes (e.g., Topple, Recovery).
		/// </summary>
		private void HandleStateChanged(MonsterBossController.BossState newState)
		{
			var effectType = TranslateStateToEffect(newState);
			TriggerEffect(effectType);
		}

		/// <summary>
		///     Maps a BossState to its equivalent BossEffectType.
		/// </summary>
		private BossEffectType TranslateStateToEffect(MonsterBossController.BossState state)
		{
			return state switch
			{
				MonsterBossController.BossState.Active => BossEffectType.Recovery,
				MonsterBossController.BossState.Toppled => BossEffectType.Topple,
				MonsterBossController.BossState.Dead => BossEffectType.Death,
				_ => BossEffectType.None
			};
		}

		/// <summary>
		///     Trigger an effect locally and propagate it across the network.
		/// </summary>
		public void TriggerEffect(BossEffectType effectType)
		{
			PlayEffect(effectType); // Play locally
			TriggerEffectRpc(effectType); // Propagate to all clients
		}

		[Rpc(SendTo.NotOwner)]
		private void TriggerEffectRpc(BossEffectType effectType)
		{
			PlayEffect(effectType); // Play remotely
		}

		/// <summary>
		///     Locally plays the specified effect.
		/// </summary>
		private void PlayEffect(BossEffectType effectType)
		{
			foreach (var effect in _effects)
				if (effect.EffectType == effectType)
				{
					// Play particle effects
					foreach (var p in effect.ParticleEffects)
						p?.Play();

					// Play audio effect
					effect.AudioEffect?.Play();
				}
		}


		[Serializable]
		public class BossEffect
		{
			public BossEffectType EffectType; // Enum identifier for the effect
			public AudioSource AudioEffect; // SFX for the effect
			public List<ParticleSystem> ParticleEffects = new(); // VFX for the effect
		}
	}
}
