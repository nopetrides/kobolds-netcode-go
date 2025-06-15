using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class FXProjectileTargetedAction
    {
        // have we actually played an impact?
        private bool _mImpactPlayed;
        // the time the FX projectile spends in the air
        private float _mProjectileDuration;
        // the currently-live projectile. (Note that the projectile will normally destroy itself! We only care in case someone calls Cancel() on us)
        private FXProjectile _mProjectile;
        // the enemy we're aiming at
        private NetworkObject _mTarget;
        Transform _mTargetTransform;

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            base.OnStartClient(clientCharacter);
            _mTarget = GetTarget(clientCharacter);

            if (_mTarget && PhysicsWrapper.TryGetPhysicsWrapper(_mTarget.NetworkObjectId, out var physicsWrapper))
            {
                _mTargetTransform = physicsWrapper.Transform;
            }

            if (Config.Projectiles.Length < 1 || Config.Projectiles[0].ProjectilePrefab == null)
                throw new System.Exception($"Action {name} has no valid ProjectileInfo!");

            return true;
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && _mProjectile == null)
            {
                // figure out how long the pretend-projectile will be flying to the target
                var targetPos = _mTargetTransform ? _mTargetTransform.position : Data.Position;
                var initialDistance = Vector3.Distance(targetPos, clientCharacter.transform.position);
                _mProjectileDuration = initialDistance / Config.Projectiles[0].Speed_m_s;

                // create the projectile. It will control itself from here on out
                _mProjectile = SpawnAndInitializeProjectile(clientCharacter);
            }

            // we keep going until the projectile's duration ends
            return TimeRunning <= _mProjectileDuration + Config.ExecTimeSeconds;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            if (_mProjectile)
            {
                // we aborted post-projectile-launch (somehow)! Tell the graphics! (It will destroy itself, possibly after playing some more FX)
                _mProjectile.Cancel();
            }
        }

        public override void EndClient(ClientCharacter clientCharacter)
        {
            PlayHitReact();
        }

        void PlayHitReact()
        {
            if (_mImpactPlayed)
                return;
            _mImpactPlayed = true;

            if (NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (_mTarget && _mTarget.TryGetComponent(out ServerCharacter clientCharacter) && clientCharacter.ClientCharacter != null)
            {
                var hitReact = !string.IsNullOrEmpty(Config.ReactAnim) ? Config.ReactAnim : KDefaultHitReact;
                clientCharacter.ClientCharacter.OurAnimator.SetTrigger(hitReact);
            }
        }

        NetworkObject GetTarget(ClientCharacter parent)
        {
            if (Data.TargetIds == null || Data.TargetIds.Length == 0)
            {
                return null;
            }

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out NetworkObject targetObject) && targetObject != null)
            {
                // make sure this isn't a friend (or if it is, make sure this is a friendly-fire action)
                var targetable = targetObject.GetComponent<ITargetable>();
                if (targetable != null && targetable.IsNpc == (Config.IsFriendly ^ parent.ServerCharacter.IsNpc))
                {
                    // not a valid target
                    return null;
                }

                return targetObject;
            }
            else
            {
                // target could have legitimately disappeared in the time it took to queue this action... but that's pretty unlikely, so we'll log about it to ease debugging
                Debug.Log($"FXProjectileTargetedActionFX was targeted at ID {Data.TargetIds[0]}, but that target can't be found in spawned object list! (May have just been deleted?)");
                return null;
            }
        }

        FXProjectile SpawnAndInitializeProjectile(ClientCharacter parent)
        {
            var projectileGo = Object.Instantiate(Config.Projectiles[0].ProjectilePrefab, parent.transform.position, parent.transform.rotation, null);

            var projectile = projectileGo.GetComponent<FXProjectile>();
            if (!projectile)
            {
                throw new System.Exception($"FXProjectileTargetedAction tried to spawn projectile {projectileGo.name}, as dictated for action {name}, but the object doesn't have a FXProjectile component!");
            }

            // now that we have our projectile, initialize it so it'll fly at the target appropriately
            projectile.Initialize(parent.transform.position, _mTargetTransform, Data.Position, _mProjectileDuration);
            return projectile;
        }

        public override void AnticipateActionClient(ClientCharacter clientCharacter)
        {
            base.AnticipateActionClient(clientCharacter);

            // see if this is going to be a "miss" because the player tried to click through a wall. If so,
            // we change our data in the same way that the server will (changing our target point to the spot on the wall)
            Vector3 targetSpot = Data.Position;
            if (Data.TargetIds != null && Data.TargetIds.Length > 0)
            {
                var targetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.TargetIds[0]];
                if (targetObj)
                {
                    targetSpot = targetObj.transform.position;
                }
            }

            if (!ActionUtils.HasLineOfSight(clientCharacter.transform.position, targetSpot, out Vector3 collidePos))
            {
                // we do not have line of sight to the target point. So our target instead becomes the obstruction point
                Data.TargetIds = null;
                Data.Position = collidePos;
            }
        }
    }
}
