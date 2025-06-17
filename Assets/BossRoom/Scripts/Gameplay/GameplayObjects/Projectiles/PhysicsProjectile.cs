using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Utils;
using Unity.BossRoom.VisualEffects;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Logic that handles a physics-based projectile with a collider
    /// </summary>
    public class PhysicsProjectile : NetworkBehaviour
    {
        bool _mStarted;

        [SerializeField]
        SphereCollider m_OurCollider;

        /// <summary>
        /// The character that created us. Can be 0 to signal that we were created generically by the server.
        /// </summary>
        ulong _mSpawnerId;

        /// <summary>
        /// The data for our projectile. Indicates speed, damage, etc.
        /// </summary>
        ProjectileInfo _mProjectileInfo;

        const int KMaxCollisions = 4;
        const float KWallLingerSec = 2f; //time in seconds that arrows linger after hitting a target.
        const float KEnemyLingerSec = 0.2f; //time after hitting an enemy that we persist.
        Collider[] _mCollisionCache = new Collider[KMaxCollisions];

        /// <summary>
        /// Time when we should destroy this arrow, in Time.time seconds.
        /// </summary>
        float _mDestroyAtSec;

        int _mCollisionMask;  //mask containing everything we test for while moving
        int _mBlockerMask;    //physics mask for things that block the arrow's flight.
        int _mNpcLayer;

        /// <summary>
        /// List of everyone we've hit and dealt damage to.
        /// </summary>
        /// <remarks>
        /// Note that it's possible for entries in this list to become null if they're Destroyed post-impact.
        /// But that's fine by us! We use <c>m_HitTargets.Count</c> to tell us how many total enemies we've hit,
        /// so those nulls still count as hits.
        /// </remarks>
        List<GameObject> _mHitTargets = new List<GameObject>();

        /// <summary>
        /// Are we done moving?
        /// </summary>
        bool _mIsDead;

        [SerializeField]
        [Tooltip("Explosion prefab used when projectile hits enemy. This should have a fixed duration.")]
        SpecialFXGraphic m_OnHitParticlePrefab;

        [SerializeField]
        TrailRenderer m_TrailRenderer;

        [SerializeField]
        Transform m_Visualization;

        const float KLerpTime = 0.1f;

        PositionLerper _mPositionLerper;

        /// <summary>
        /// Set everything up based on provided projectile information.
        /// (Note that this is called before OnNetworkSpawn(), so don't try to do any network stuff here.)
        /// </summary>
        public void Initialize(ulong creatorsNetworkObjectId, in ProjectileInfo projectileInfo)
        {
            _mSpawnerId = creatorsNetworkObjectId;
            _mProjectileInfo = projectileInfo;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _mStarted = true;

                _mHitTargets = new List<GameObject>();
                _mIsDead = false;

                _mDestroyAtSec = Time.fixedTime + (_mProjectileInfo.Range / _mProjectileInfo.Speed_m_s);

                _mCollisionMask = LayerMask.GetMask(new[] { "NPCs", "Default", "Environment" });
                _mBlockerMask = LayerMask.GetMask(new[] { "Default", "Environment" });
                _mNpcLayer = LayerMask.NameToLayer("NPCs");
            }

            if (IsClient)
            {
                m_TrailRenderer.Clear();

                m_Visualization.parent = null;

                _mPositionLerper = new PositionLerper(transform.position, KLerpTime);
                m_Visualization.transform.rotation = transform.rotation;
            }

        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _mStarted = false;
            }


            if (IsClient)
            {
                m_TrailRenderer.Clear();
                m_Visualization.parent = transform;
            }
        }

        void FixedUpdate()
        {
            if (!_mStarted || !IsServer)
            {
                return; //don't do anything before OnNetworkSpawn has run.
            }

            if (_mDestroyAtSec < Time.fixedTime)
            {
                // Time to return to the pool from whence it came.
                var networkObject = gameObject.GetComponent<NetworkObject>();
                networkObject.Despawn();
                return;
            }

            var displacement = transform.forward * (_mProjectileInfo.Speed_m_s * Time.fixedDeltaTime);
            transform.position += displacement;

            if (!_mIsDead)
            {
                DetectCollisions();
            }
        }

        void Update()
        {
            if (IsClient)
            {
                // One thing to note: this graphics GameObject is detached from its parent on OnNetworkSpawn. On the host,
                // the m_Parent Transform is translated via PhysicsProjectile's FixedUpdate method. On all other
                // clients, m_Parent's NetworkTransform handles syncing and interpolating the m_Parent Transform. Thus, to
                // eliminate any visual jitter on the host, this GameObject is positionally smoothed over time. On all other
                // clients, no positional smoothing is required, since m_Parent's NetworkTransform will perform
                // positional interpolation on its Update method, and so this position is simply matched 1:1 with m_Parent.

                if (IsHost)
                {
                    m_Visualization.position = _mPositionLerper.LerpPosition(m_Visualization.position,
                        transform.position);
                }
                else
                {
                    m_Visualization.position = transform.position;
                }
            }

        }

        void DetectCollisions()
        {
            var position = transform.localToWorldMatrix.MultiplyPoint(m_OurCollider.center);
            var numCollisions = Physics.OverlapSphereNonAlloc(position, m_OurCollider.radius, _mCollisionCache, _mCollisionMask);
            for (int i = 0; i < numCollisions; i++)
            {
                int layerTest = 1 << _mCollisionCache[i].gameObject.layer;
                if ((layerTest & _mBlockerMask) != 0)
                {
                    //hit a wall; leave it for a couple of seconds.
                    _mProjectileInfo.Speed_m_s = 0;
                    _mIsDead = true;
                    _mDestroyAtSec = Time.fixedTime + KWallLingerSec;
                    return;
                }

                if (_mCollisionCache[i].gameObject.layer == _mNpcLayer && !_mHitTargets.Contains(_mCollisionCache[i].gameObject))
                {
                    _mHitTargets.Add(_mCollisionCache[i].gameObject);

                    if (_mHitTargets.Count >= _mProjectileInfo.MaxVictims)
                    {
                        // we've hit all the enemies we're allowed to! So we're done
                        _mDestroyAtSec = Time.fixedTime + KEnemyLingerSec;
                        _mIsDead = true;
                    }

                    //all NPC layer entities should have one of these.
                    var targetNetObj = _mCollisionCache[i].GetComponentInParent<NetworkObject>();
                    if (targetNetObj)
                    {
                        ClientHitEnemyRpc(targetNetObj.NetworkObjectId);

                        //retrieve the person that created us, if he's still around.
                        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_mSpawnerId, out var spawnerNet);
                        var spawnerObj = spawnerNet != null ? spawnerNet.GetComponent<ServerCharacter>() : null;

                        if (_mCollisionCache[i].TryGetComponent(out IDamageable damageable))
                        {
                            damageable.ReceiveHp(spawnerObj, -_mProjectileInfo.Damage);
                        }
                    }

                    if (_mIsDead)
                    {
                        return; // don't keep examining collisions since we can't damage anybody else
                    }
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ClientHitEnemyRpc(ulong enemyId)
        {
            //in the future we could do quite fancy things, like deparenting the Graphics Arrow and parenting it to the target.
            //For the moment we play some particles (optionally), and cause the target to animate a hit-react.

            NetworkObject targetNetObject;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemyId, out targetNetObject))
            {
                if (m_OnHitParticlePrefab)
                {
                    // show an impact graphic
                    Instantiate(m_OnHitParticlePrefab.gameObject, transform.position, transform.rotation);
                }
            }
        }
    }
}
