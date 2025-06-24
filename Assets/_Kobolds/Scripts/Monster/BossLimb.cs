using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	public class BossLimb : NetworkBehaviour
	{
		[SerializeField] private MonsterBossController _controller;

		[Header("Limb Settings")]
		[SerializeField] private float _maxHealth = 100f;

		[SerializeField] private Rigidbody _rigidbody;
		[SerializeField] private Collider _collider;
		[SerializeField] private Material _defaultMaterial;
		[SerializeField] private Material _brokenMaterial;

		private float _currentHealth;

		private readonly NetworkVariable<bool> _isBroken = new(
			false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

		public bool IsBroken => _isBroken.Value;

		private void Awake()
		{
			_currentHealth = _maxHealth;

			if (!_rigidbody || !_collider || !_controller || !_defaultMaterial || !_brokenMaterial)
				Debug.LogError($"[BossLimb] {name} has invalid setup!");

			// Subscribe to network state changes
			_isBroken.OnValueChanged += OnBrokenStateChanged;
		}

		public override void OnDestroy()
		{
			// Unsubscribe from network events to avoid potential memory leaks.
			_isBroken.OnValueChanged -= OnBrokenStateChanged;
			
			base.OnDestroy();
		}

		public void ApplyDamage(float amount)
		{
			if (IsBroken) return;

			_currentHealth -= amount;
			if (_currentHealth <= 0f) BreakLimb();

			_controller?.ApplyDamage(amount);
		}

		private void BreakLimb()
		{
			if (!HasAuthority)
			{
				Debug.LogError("[BossLimb] BreakLimb called on non-owner client!");
				return;
			}

			_isBroken.Value = true;
			Debug.Log($"[BossLimb] {name} has broken!");

			_controller?.ReportLimbBroken(this);

			// Optional: physics tweak to simulate collapse
			if (_rigidbody != null)
			{
				_rigidbody.constraints = RigidbodyConstraints.None;
				_rigidbody.mass *= 0.5f;
			}

			// Could also disable joint or visuals if needed
		}

		public void ResetLimb()
		{
			if (!HasAuthority)
			{
				Debug.LogError("[ResetLimb] BreakLimb called on non-owner client!");
				return;
			}

			_isBroken.Value = false;
			_currentHealth = _maxHealth;

			Debug.Log($"[BossLimb] {name} reset to full health");

			if (_rigidbody != null)
			{
				_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
				_rigidbody.mass = 1f;
				_rigidbody.linearVelocity = Vector3.zero;
				_rigidbody.angularVelocity = Vector3.zero;
			}
		}

		/// <summary>
		///     Callback for when the broken state changes.
		///     Controls material updates across all players.
		/// </summary>
		private void OnBrokenStateChanged(bool previousState, bool newState)
		{
			Debug.Log($"[BossLimb] {name} broken state changed to {newState}");
			UpdateMaterial(newState);
		}

		/// <summary>
		///     Updates the material of the limb based on its state.
		/// </summary>
		private void UpdateMaterial(bool isBroken)
		{
			foreach (var r in GetComponentsInChildren<Renderer>())
				r.material = isBroken ? _brokenMaterial : _defaultMaterial;
		}
	}
}
