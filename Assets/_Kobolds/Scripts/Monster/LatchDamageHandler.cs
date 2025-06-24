using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	public class LatchDamageHandler : NetworkBehaviour
	{
		[SerializeField] private MonsterBossController _controller;
		[SerializeField] private MonsterBossRPCHandler _rpcHandler; // RPC handler for boss visual effects
		[SerializeField] private BossLimb _limb;
		[SerializeField] private NetworkObject _networkObject;

		[Header("Latch Damage Settings")]
		[SerializeField] private float _damageOnLatch = 10f; // G
		[SerializeField] private float _damagePerSecond = 5f; // H

		[Header("Damage Routing")]
		[SerializeField] private bool _isWeakSpot;
		[SerializeField] private bool _isCore;

		private readonly HashSet<Transform> _latchedSources = new(); // Each kobold transform
		private float _damageTimer = 0f; // Timer to track elapsed time

		private void Update()
		{
			if (_latchedSources.Count == 0 || _controller == null) return;

			// Accumulate time
			_damageTimer += Time.deltaTime;

			if (_damageTimer >= 1f) // Check if 1 second has passed
			{
				// Calculate total damage for the past second
				var totalDamage = _damagePerSecond;

				if (!_controller.HasAuthority)
					ApplyOnGnawRpc(); // Only the boss owner can apply damage, so send an RPC to apply damage
				else
					// Apply the accumulated damage
					ApplyDamage(totalDamage);

				// Reset the timer
				_damageTimer = 0f;
			}
		}


		/// <summary>
		///     Called by the latcher when the latch begins.
		/// </summary>
		public void OnLatched(Transform source)
		{
			Debug.Log($"[LatchDamageHandler] OnLatched called with source: {(source != null ? source.name : "NULL")}");
			
			if (source == null)
			{
				Debug.LogError("[LatchDamageHandler] OnLatched called with null source transform!");
				return;
			}

			if (_latchedSources.Contains(source))
			{
				Debug.Log($"[LatchDamageHandler] Source {source.name} already in latched sources, ignoring duplicate");
				return;
			}

			_latchedSources.Add(source);
			Debug.Log($"[LatchDamageHandler] Added {source.name} to latched sources. Total sources: {_latchedSources.Count}");
			
			// Only apply initial damage on the boss owner (HasAuthority)
			if (_controller == null)
			{
				Debug.LogError("[LatchDamageHandler] _controller is null! Cannot apply damage.");
				return;
			}

			if (!_controller.HasAuthority)
			{
				Debug.Log($"[LatchDamageHandler] Not boss owner (HasAuthority: {_controller.HasAuthority}), sending rpc");
				ApplyOnLatchRpc();
				return;
			}

			Debug.Log($"[LatchDamageHandler] Applying initial damage of {_damageOnLatch} to boss");
			ApplyDamage(_damageOnLatch);
		}

		/// <summary>
		///     Called by the latcher when the latch ends.
		/// </summary>
		public void OnUnlatched(Transform source)
		{
			Debug.Log($"[LatchDamageHandler] OnUnlatched called with source: {(source != null ? source.name : "NULL")}");
			
			if (source == null)
			{
				Debug.LogError("[LatchDamageHandler] OnUnlatched called with null source transform!");
				return;
			}

			_latchedSources.Remove(source);
			Debug.Log($"[LatchDamageHandler] Removed {source.name} from latched sources. Total sources: {_latchedSources.Count}");
		}

		/// <summary>
		/// Applies damage to the boss, routing it appropriately via the RPC handler.
		/// </summary>
		/// <param name="amount">Amount of damage to apply</param>
		private void ApplyDamage(float amount)
		{
			Debug.Log($"[LatchDamageHandler] ApplyDamage called with amount: {amount}");

			if (_controller == null)
			{
				Debug.LogError("[LatchDamageHandler] _controller is null in ApplyDamage!");
				return;
			}

			if (_rpcHandler == null)
			{
				Debug.LogError("[LatchDamageHandler] _rpcHandler is not assigned!");
				return;
			}

			// Route the damage based on its type
			if (_isCore)
			{
				Debug.Log($"[LatchDamageHandler] Applying core damage: {amount}");
				_rpcHandler.TriggerApplyDamageRpc(amount, false, true);
			}
			else if (_isWeakSpot)
			{
				Debug.Log($"[LatchDamageHandler] Applying weak spot damage: {amount}");
				_rpcHandler.TriggerApplyDamageRpc(amount, true, false);
			}
			else if (_limb != null)
			{
				Debug.Log($"[LatchDamageHandler] Applying limb damage: {amount}");
				_rpcHandler.TriggerApplyDamageRpc(amount, false, false, _limb.name);
			}
			else
			{
				Debug.Log($"[LatchDamageHandler] Applying default damage: {amount}");
				_rpcHandler.TriggerApplyDamageRpc(amount);  // Default damage
			}
		}

		[Rpc(SendTo.Owner)]
		private void ApplyOnLatchRpc()
		{
			ApplyDamage(_damageOnLatch);
		}
		
		[Rpc(SendTo.Owner)]
		private void ApplyOnGnawRpc()
		{
			ApplyDamage(_damagePerSecond);
		}
	}
}
