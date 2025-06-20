using UnityEngine;

namespace Kobold.Bosses
{
    public class BossLimb : MonoBehaviour
    {
		[SerializeField] private MonsterBossController _controller;
		[Header("Limb Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _collider;

        private float _currentHealth;
        private bool _isBroken;
        public bool IsBroken => _isBroken;

        private void Awake()
        {
            _currentHealth = _maxHealth;
			if (!_rigidbody || !_collider || !_controller)
			{
				Debug.LogError($"[BossLimb] Invalid setup {name}");
			}
        }

        public void ApplyDamage(float amount)
        {
            if (_isBroken) return;

            _currentHealth -= amount;
            if (_currentHealth <= 0f)
            {
                BreakLimb();
            }

            _controller?.ApplyDamage(amount);
        }

        private void BreakLimb()
        {
            _isBroken = true;
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
            _isBroken = false;
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
    }
}
