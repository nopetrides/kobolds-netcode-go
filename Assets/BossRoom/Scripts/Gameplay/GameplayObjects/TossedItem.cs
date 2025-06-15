using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class TossedItem : NetworkBehaviour
    {
        [Header("Server")]

        [SerializeField]
        int m_DamagePoints;

        [SerializeField]
        float m_HitRadius = 5f;

        [SerializeField]
        float m_KnockbackSpeed;

        [SerializeField]
        float m_KnockbackDuration;

        [SerializeField]
        LayerMask m_LayerMask;

        bool _mStarted;

        const int KMaxCollisions = 16;

        Collider[] _mCollisionCache = new Collider[KMaxCollisions];

        [SerializeField]
        float m_DetonateAfterSeconds = 5f;

        float _mDetonateTimer;

        [SerializeField]
        float m_DestroyAfterSeconds = 6f;

        float _mDestroyTimer;

        bool _mDetonated;

        public UnityEvent detonatedCallback;

        [Header("Client")]

        [SerializeField]
        Transform m_TossedItemVisualTransform;

        const float KDisplayHeight = 0.1f;

        readonly Quaternion _kTossAttackRadiusDisplayRotation = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField]
        GameObject m_TossedObjectGraphics;

        [SerializeField]
        AudioSource m_FallingSound;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _mStarted = true;
                _mDetonated = false;

                _mDetonateTimer = Time.fixedTime + m_DetonateAfterSeconds;
                _mDestroyTimer = Time.fixedTime + m_DestroyAfterSeconds;
            }

            if (IsClient)
            {
                m_TossedItemVisualTransform.gameObject.SetActive(true);
                m_TossedObjectGraphics.SetActive(true);
                m_FallingSound.Play();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _mStarted = false;
                _mDetonated = false;
            }

            if (IsClient)
            {
                m_TossedItemVisualTransform.gameObject.SetActive(false);
            }

        }

        void Detonate()
        {
            var hits = Physics.OverlapSphereNonAlloc(transform.position, m_HitRadius, _mCollisionCache, m_LayerMask);

            for (int i = 0; i < hits; i++)
            {
                if (_mCollisionCache[i].gameObject.TryGetComponent(out IDamageable damageReceiver))
                {
                    damageReceiver.ReceiveHp(null, -m_DamagePoints);

                    var serverCharacter = _mCollisionCache[i].gameObject.GetComponentInParent<ServerCharacter>();
                    if (serverCharacter)
                    {
                        serverCharacter.Movement.StartKnockback(transform.position, m_KnockbackSpeed, m_KnockbackDuration);
                    }
                }
            }

            // send client RPC to detonate on clients
            ClientDetonateRpc();

            _mDetonated = true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientDetonateRpc()
        {
            detonatedCallback?.Invoke();
        }

        void FixedUpdate()
        {
            if (IsServer)
            {
                if (!_mStarted)
                {
                    return; //don't do anything before OnNetworkSpawn has run.
                }

                if (!_mDetonated && _mDetonateTimer < Time.fixedTime)
                {
                    Detonate();
                }

                if (_mDetonated && _mDestroyTimer < Time.fixedTime)
                {
                    // despawn after sending detonate RPC
                    var networkObject = gameObject.GetComponent<NetworkObject>();
                    networkObject.Despawn();
                }
            }
        }

        void LateUpdate()
        {
            if (IsClient)
            {
                var tossedItemPosition = transform.position;
                m_TossedItemVisualTransform.SetPositionAndRotation(
                    new Vector3(tossedItemPosition.x, KDisplayHeight, tossedItemPosition.z),
                    _kTossAttackRadiusDisplayRotation);
            }
        }
    }
}
