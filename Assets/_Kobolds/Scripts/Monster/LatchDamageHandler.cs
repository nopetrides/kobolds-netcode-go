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
			if (NetworkManager.Singleton.LocalClientId != NetworkManager.Singleton.CurrentSessionOwner) return;

			if (_latchedSources.Count == 0) return;

			var damage = _damagePerSecond * Time.deltaTime;
			foreach (var source in _latchedSources) ApplyDamage(damage);
		}

		/// <summary>
		///     Called by the latcher when the latch begins.
		/// </summary>
		public void OnLatched(Transform source)
		{
			// This is the correct way to do something like IsOwner
			if (NetworkManager.Singleton.LocalClientId != NetworkManager.Singleton.CurrentSessionOwner) return;
			if (_latchedSources.Contains(source)) return;

			_latchedSources.Add(source);
			ApplyDamage(_damageOnLatch);
		}

		/// <summary>
		///     Called by the latcher when the latch ends.
		/// </summary>
		public void OnUnlatched(Transform source)
		{
			_latchedSources.Remove(source);
		}

		private void ApplyDamage(float amount)
		{
			if (_controller == null) return;

			if (_isCore)
				_controller.ApplyDamage(amount, false, true);
			else if (_isWeakSpot)
				_controller.ApplyDamage(amount, true);
			else if (_limb != null)
				_limb.ApplyDamage(amount);
			else
				_controller.ApplyDamage(amount);
		}
	}
}
