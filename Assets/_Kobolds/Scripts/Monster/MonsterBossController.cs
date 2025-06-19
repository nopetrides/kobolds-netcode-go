using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Kobolds.Bosses
{
    public class MonsterBossController : NetworkBehaviour
    {
        [Header("Boss Parameters")]
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float weakSpotMultiplier = 3f;
        [SerializeField] private float toppleDuration = 5f;
        [SerializeField] private float coreKillThreshold = 100f;

        [Header("Hierarchy References")]
        [SerializeField] private GameObject weakSpotObject;
        [SerializeField] private GameObject coreObject;
        [SerializeField] private List<BossLimb> _limbs;

        private float _currentHealth;
        private float _coreDamageTaken;
        private float _toppleTimer;

        private bool _isToppled;
        private bool _coreExposed;

        public enum BossState { Active, Toppled, Dead }
        private BossState _state = BossState.Active;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _currentHealth = maxHealth;
				
				foreach (var netObj in gameObject.GetComponentsInChildren<Rigidbody>(includeInactive: true))
				{
					netObj.isKinematic = true;
				}
            }

            BossManager.Instance?.RegisterBoss(this);
            BossManager.Instance?.ConfigureAuthority(this);
        }

        protected override void OnOwnershipChanged(ulong previousOwner,ulong currentOwner)
        {
            base.OnOwnershipChanged(previousOwner, currentOwner);
            BossManager.Instance?.ConfigureAuthority(this);
        }

        public override void OnNetworkDespawn()
        {
            BossManager.Instance?.UnregisterBoss(this);
        }

        private void Update()
        {
            if (!IsOwner || _state != BossState.Toppled) return;

            _toppleTimer -= Time.deltaTime;
            if (_toppleTimer <= 0f)
            {
                ExitToppleState();
            }
        }

        public void ReportLimbBroken(BossLimb limb)
        {
            if (_state != BossState.Active) return;

            int brokenCount = 0;
            foreach (var l in _limbs)
                if (l.IsBroken) brokenCount++;

            if (brokenCount >= 2) // Threshold A — hardcoded
            {
                EnterToppleState();
            }
        }

        public void ApplyDamage(float amount, bool isWeakSpot = false, bool isCore = false)
        {
            if (!IsOwner || _state == BossState.Dead) return;

            float scaled = isWeakSpot ? amount * weakSpotMultiplier : amount;

            if (isCore)
            {
                _coreDamageTaken += scaled;
                if (_coreDamageTaken >= coreKillThreshold)
                {
                    KillBoss();
                }
                return;
            }

            _currentHealth -= scaled;
            if (_currentHealth <= 0f && _state != BossState.Toppled)
            {
                EnterToppleState();
            }
        }

        private void EnterToppleState()
        {
            _state = BossState.Toppled;
            _isToppled = true;
            _coreExposed = _currentHealth <= 0f;
            _toppleTimer = toppleDuration;
            _coreDamageTaken = 0f;

            if (weakSpotObject) weakSpotObject.SetActive(true);
            if (_coreExposed && coreObject) coreObject.SetActive(true);
        }

        private void ExitToppleState()
        {
            _state = BossState.Active;
            _isToppled = false;
            _coreExposed = false;

            if (weakSpotObject) weakSpotObject.SetActive(false);
            if (coreObject) coreObject.SetActive(false);

            foreach (var limb in _limbs)
                limb.ResetLimb();
        }

        private void KillBoss()
        {
            _state = BossState.Dead;
            if (coreObject) coreObject.SetActive(false);
            if (weakSpotObject) weakSpotObject.SetActive(false);

            // TODO: trigger explosion, particles, disable colliders, etc.
        }

        public bool IsToppled => _isToppled;
        public BossState State => _state;
    }
}
