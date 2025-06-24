using System;
using System.Collections.Generic;
using Kobold.Monster;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	public class MonsterBossController : NetworkBehaviour
	{
		public enum BossState
		{
			Active,
			Toppled,
			Dead
		}

		[Header("Boss Parameters")]
		[SerializeField] private float _maxHealth = 500f;

		[SerializeField] private float _weakSpotMultiplier = 3f;
		[SerializeField] private float _toppleDuration = 5f;
		[SerializeField] private float _aoeChargeDuration = 2f; // Duration of the warning before the pulse
		[SerializeField] private float _coreKillThreshold = 5;

		[Header("Related Components")]
		[SerializeField] private BossMover _bossMover;
		[SerializeField] private MonsterBossRPCHandler _rpcHandler;
		[SerializeField] private List<AoePulseOnRecover> _aoePulseHandlers; // Add references to AOE pulse handlers in the scene

		[Header("Hierarchy References")]
		[SerializeField] private GameObject _weakSpotObject;

		[SerializeField] private GameObject _coreObject;
		[SerializeField] private List<BossLimb> _limbs;
		
		// Network variables
		private NetworkVariable<float> _currentHealth = new(
			500f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		
		private float _coreDamageTaken; // Tracks damage dealt to the core
		private float _aoeChargeTimer; // Used to track the charge duration for the pulse
		private bool _isChargingPulse;
		private float _toppleTimer;

		public bool IsToppled { get; private set; }
		public BossState CurrentState { get; private set; } = BossState.Active;
		public float MaxHealth => _maxHealth;
		public float CurrentHealth => _currentHealth.Value;

		public event Action<float, float> OnHealthChanged; // Broadcast health updates
		public event Action<BossState> OnStateChanged; // Notify listeners of state changes
		
		private void Awake()
		{
			_currentHealth.OnValueChanged += SyncHealthLocal;
		}
		
		private void Update()
		{
			if (HasAuthority && CurrentState == BossState.Toppled)
			{
				if (!_isChargingPulse)
				{
					// Topple timer logic
					_toppleTimer -= Time.deltaTime;
					if (_toppleTimer <= 0f)
					{
						TriggerPulseCharge(); // Start the charge timer for AOE pulse
					}
				}
				else
				{
					// AOE pulse charge timer logic
					_aoeChargeTimer -= Time.deltaTime;
					if (_aoeChargeTimer <= 0f)
					{
						TriggerAoePulse(); // Trigger AOE pulse after charge timer ends
						ExitToppleState();
					}
				}
			}
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			
			// Initialize boss health and states only on the server
			if (HasAuthority)
			{
				_currentHealth.Value = _maxHealth;
				OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);

				foreach (var rb in GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
			}

			BossManager.Instance?.RegisterBoss(this);
		}

		public override void OnNetworkDespawn()
		{
			base.OnNetworkDespawn();
			if (HasAuthority)
				BossManager.Instance?.UnregisterBoss(this);
		}

#region State Transitions

		private void ChangeState(BossState newState)
		{
			Debug.Log($"[MonsterBossController] ChangeState({newState})");
			if (CurrentState == newState) return;

			CurrentState = newState;
			OnStateChanged?.Invoke(newState);

			// Notify the RPC handler to propagate effects as needed
			switch (newState)
			{
				case BossState.Active:
					_rpcHandler.TriggerEffectRpc(BossEffectType.Recovery);
					break;

				case BossState.Toppled:
					_rpcHandler.TriggerEffectRpc(BossEffectType.Topple);
					break;

				case BossState.Dead:
					_rpcHandler.TriggerEffectRpc(BossEffectType.Death);
					break;
			}
		}
		
		private void SyncHealthLocal(float previousValue, float newValue)
		{
			OnHealthChanged?.Invoke(newValue, _maxHealth); // Update player HUD
		}

		private void EnterToppleState()
		{
			Debug.Log($"[MonsterBossController] EnterToppleState");
			ChangeState(BossState.Toppled);

			IsToppled = true;
			_toppleTimer = _toppleDuration;

			if (_weakSpotObject != null) _weakSpotObject.SetActive(true);

			_bossMover.PlayToppleMotion();
			
			if (_currentHealth.Value <= 0f)
			{
				if (_coreObject != null) _coreObject.SetActive(true);
				_bossMover.PlayCoreRevealMotion();
				_rpcHandler.TriggerEffectRpc(BossEffectType.CoreReveal); // Core reveal feedback
			}
		}

		private void ExitToppleState()
		{
			Debug.Log($"[MonsterBossController] ExitToppleState");
			ChangeState(BossState.Active);

			IsToppled = false;

			if (_coreObject != null) _coreObject.SetActive(false);

			foreach (var limb in _limbs) limb.ResetLimb();

			_bossMover.PlayRecoveryEffectMotion();
		}
		
		private void TriggerPulseCharge()
		{
			Debug.Log($"[MonsterBossController] TriggerPulseCharge");
			_isChargingPulse = true;
			_aoeChargeTimer = _aoeChargeDuration;

			// Notify clients to start the AOE pulse charge effect
			_rpcHandler.TriggerEffectRpc(BossEffectType.AoePulseCharge);

			// Start visualizing the growing spheres in each AOE pulse handler
			foreach (var handler in _aoePulseHandlers)
			{
				handler.StartChargeVisual(_aoeChargeDuration);
			}
		}

		private void TriggerAoePulse()
		{
			Debug.Log($"[MonsterBossController] TriggerAoePulse");
			_isChargingPulse = false;

			// Notify AOE pulse handlers to process the actual damage
			foreach (var handler in _aoePulseHandlers)
			{
				handler.TriggerPulse();
			}

			// Notify clients of the AOE pulse effect
			_rpcHandler.TriggerEffectRpc(BossEffectType.AoePulseAttack);
		}

		private void KillBoss()
		{
			Debug.Log($"[MonsterBossController] KillBoss");
			ChangeState(BossState.Dead);

			if (_coreObject != null) _coreObject.SetActive(false);
			if (_weakSpotObject != null) _weakSpotObject.SetActive(false);

			_bossMover.PlayDeathMotion();
		}

#endregion

#region Gameplay Logic

		public void ReportLimbBroken(BossLimb limb)
		{
			if (CurrentState != BossState.Active) return;

			var brokenCount = 0;
			foreach (var l in _limbs)
				if (l.IsBroken)
					brokenCount++;

			if (brokenCount >= _limbs.Count) EnterToppleState();
		}

		public void ApplyDamage(float amount, bool isWeakSpot = false, bool isCore = false, string limbName = null)
		{
			if (!HasAuthority || CurrentState == BossState.Dead) return;

			var damageToApply = isWeakSpot ? amount * _weakSpotMultiplier : amount;

			if (isCore)
			{
				_coreDamageTaken += damageToApply;
				if (_coreDamageTaken >= _coreKillThreshold)
				{
					KillBoss();
				}
				return;
			}

			if (limbName != null)
			{
				var limb = _limbs.Find(l => l.name == limbName);
				if (limb != null)
				{
					limb.ApplyDamage(damageToApply);
					return;
				}
			}

			_currentHealth.Value -= damageToApply;
			OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);

			if (_currentHealth.Value <= 0f && CurrentState != BossState.Toppled)
			{
				EnterToppleState();
			}
		}


#endregion
	}
}
