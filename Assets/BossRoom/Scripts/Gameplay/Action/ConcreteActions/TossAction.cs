using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Action responsible for creating a physics-based thrown object.
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Toss Action")]
    public class TossAction : Action
    {
        bool _mLaunched;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            // snap to face the direction we're firing

            if (MData.TargetIds != null && MData.TargetIds.Length > 0)
            {
                var initialTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[MData.TargetIds[0]];
                if (initialTarget)
                {
                    Vector3 lookAtPosition;
                    if (PhysicsWrapper.TryGetPhysicsWrapper(initialTarget.NetworkObjectId, out var physicsWrapper))
                    {
                        lookAtPosition = physicsWrapper.Transform.position;
                    }
                    else
                    {
                        lookAtPosition = initialTarget.transform.position;
                    }

                    // snap to face our target! This is the direction we'll attack in
                    serverCharacter.PhysicsWrapper.Transform.LookAt(lookAtPosition);
                }
            }

            serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            serverCharacter.ClientCharacter.ClientPlayActionRpc(Data);
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            _mLaunched = false;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && !_mLaunched)
            {
                Throw(clientCharacter);
            }

            return true;
        }

        /// <summary>
        /// Looks through the ProjectileInfo list and finds the appropriate one to instantiate.
        /// For the base class, this is always just the first entry with a valid prefab in it!
        /// </summary>
        /// <exception cref="System.Exception">thrown if no Projectiles are valid</exception>
        ProjectileInfo GetProjectileInfo()
        {
            foreach (var projectileInfo in Config.Projectiles)
            {
                if (projectileInfo.ProjectilePrefab)
                {
                    return projectileInfo;
                }
            }
            throw new System.Exception($"Action {this.name} has no usable Projectiles!");
        }

        /// <summary>
        /// Instantiates and configures the thrown object. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks>
        /// This calls GetProjectilePrefab() to find the prefab it should instantiate.
        /// </remarks>
        void Throw(ServerCharacter parent)
        {
            if (!_mLaunched)
            {
                _mLaunched = true;

                var projectileInfo = GetProjectileInfo();

                var no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);

                var networkObjectTransform = no.transform;

                // point the thrown object the same way we're facing
                networkObjectTransform.forward = parent.PhysicsWrapper.Transform.forward;

                networkObjectTransform.position = parent.PhysicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(networkObjectTransform.position) +
                    networkObjectTransform.forward + (Vector3.up * 2f);

                no.Spawn(true);

                // important to add a force AFTER a NetworkObject is spawned, since IsKinematic is enabled on the
                // Rigidbody component after it is spawned
                var tossedItemRigidbody = no.GetComponent<Rigidbody>();

                tossedItemRigidbody.AddForce((networkObjectTransform.forward * 80f) + (networkObjectTransform.up * 150f), ForceMode.Impulse);
                tossedItemRigidbody.AddTorque((networkObjectTransform.forward * Random.Range(-15f, 15f)) + (networkObjectTransform.up * Random.Range(-15f, 15f)), ForceMode.Impulse);
            }
        }
    }
}
