using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Action responsible for creating a projectile object.
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Launch Projectile Action")]
    public class LaunchProjectileAction : Action
    {
        private bool _mLaunched = false;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            //snap to face the direction we're firing, and then broadcast the animation, which we do immediately.
            serverCharacter.PhysicsWrapper.Transform.forward = Data.Direction;

            serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            serverCharacter.ClientCharacter.ClientPlayActionRpc(Data);
            return true;
        }

        public override void Reset()
        {
            _mLaunched = false;
            base.Reset();
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && !_mLaunched)
            {
                LaunchProjectile(clientCharacter);
            }

            return true;
        }

        /// <summary>
        /// Looks through the ProjectileInfo list and finds the appropriate one to instantiate.
        /// For the base class, this is always just the first entry with a valid prefab in it!
        /// </summary>
        /// <exception cref="System.Exception">thrown if no Projectiles are valid</exception>
        protected virtual ProjectileInfo GetProjectileInfo()
        {
            foreach (var projectileInfo in Config.Projectiles)
            {
                if (projectileInfo.ProjectilePrefab && projectileInfo.ProjectilePrefab.GetComponent<PhysicsProjectile>())
                    return projectileInfo;
            }
            throw new System.Exception($"Action {name} has no usable Projectiles!");
        }

        /// <summary>
        /// Instantiates and configures the arrow. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks>
        /// This calls GetProjectilePrefab() to find the prefab it should instantiate.
        /// </remarks>
        protected void LaunchProjectile(ServerCharacter parent)
        {
            if (!_mLaunched)
            {
                _mLaunched = true;

                var projectileInfo = GetProjectileInfo();

                NetworkObject no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);
                // point the projectile the same way we're facing
                no.transform.forward = parent.PhysicsWrapper.Transform.forward;

                //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
                //where it appears next to the player.
                no.transform.position = parent.PhysicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(no.transform.position);

                no.GetComponent<PhysicsProjectile>().Initialize(parent.NetworkObjectId, projectileInfo);

                no.Spawn(true);
            }
        }

        public override void End(ServerCharacter serverCharacter)
        {
            //make sure this happens.
            LaunchProjectile(serverCharacter);
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }

    }
}
