using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	public class LatchDamageHandler : MonoBehaviour
	{
		[SerializeField] private MonsterBossController _controller;
		[SerializeField] private BossLimb _limb;
		[SerializeField] private NetworkObject _networkObject;

		[Header("Latch Damage Settings")]
		[SerializeField] private float _damageOnLatch = 10f; // G

		[SerializeField] private float _damagePerSecond = 5f; // H

		[Header("Damage Routing")]
		[SerializeField] private bool _isWeakSpot;

		[SerializeField] private bool _isCore;

		private readonly HashSet<Transform> _latchedSources = new(); // Each kobold transform

		private void Update()
		{
			if (_latchedSources.Count == 0) return;

			// Only apply damage on the boss owner (HasAuthority)
			if (_controller != null && _controller.HasAuthority)
			{
				var damage = _damagePerSecond * Time.deltaTime;
				foreach (var source in _latchedSources) ApplyDamage(damage);
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
				Debug.Log($"[LatchDamageHandler] Not boss owner (HasAuthority: {_controller.HasAuthority}), skipping damage application");
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

		private void ApplyDamage(float amount)
		{
			Debug.Log($"[LatchDamageHandler] ApplyDamage called with amount: {amount}");
			
			if (_controller == null)
			{
				Debug.LogError("[LatchDamageHandler] _controller is null in ApplyDamage!");
				return;
			}

			if (_isCore)
			{
				Debug.Log($"[LatchDamageHandler] Applying core damage: {amount}");
				_controller.ApplyDamageServerRpc(amount, false, true);
			}
			else if (_isWeakSpot)
			{
				Debug.Log($"[LatchDamageHandler] Applying weak spot damage: {amount}");
				_controller.ApplyDamageServerRpc(amount, true);
			}
			else if (_limb != null)
			{
				Debug.Log($"[LatchDamageHandler] Applying limb damage: {amount}");
				_limb.ApplyDamage(amount);
			}
			else
			{
				Debug.Log($"[LatchDamageHandler] Applying default damage: {amount}");
				_controller.ApplyDamageServerRpc(amount);
			}
		}
	}
}
