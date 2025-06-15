using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Action for picking up "Heavy" items. For simplicity, this class will perform both the pickup (reparenting) of a
    /// NetworkObject, as well as the drop (deparenting).
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Pick Up Action")]
    public class PickUpAction : Action
    {
        const string KHeavyTag = "Heavy";
        const string KNpcLayer = "NPCs";
        const string KFailedPickupTrigger = "PickUpFailed";

        static RaycastHitComparer _sRaycastHitComparer = new RaycastHitComparer();

        RaycastHit[] _mRaycastHits = new RaycastHit[8];
        float _mActionStartTime;
        bool _mAttemptedPickup;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            _mActionStartTime = Time.time;

            // play pickup animation based if a heavy object is not already held
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    serverCharacter.HeldNetworkObject.Value, out var heldObject))
            {
                if (!string.IsNullOrEmpty(Config.Anim))
                {
                    serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
                }
            }

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            _mActionStartTime = 0;
            _mAttemptedPickup = false;
        }

        bool TryPickUp(ServerCharacter parent)
        {
            var numResults = Physics.RaycastNonAlloc(parent.PhysicsWrapper.Transform.position,
                parent.PhysicsWrapper.Transform.forward,
                _mRaycastHits,
                Config.Range,
                1 << LayerMask.NameToLayer(KNpcLayer));

            Array.Sort(_mRaycastHits, 0, numResults, _sRaycastHitComparer);

            // collider must contain "Heavy" tag, the heavy object must not be parented to another NetworkObject, and
            // parenting attempt must be successful
            if (numResults == 0 || !_mRaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject) ||
                !_mRaycastHits[0].collider.gameObject.CompareTag(KHeavyTag) ||
                (heavyNetworkObject.transform.parent != null &&
                    heavyNetworkObject.transform.parent.TryGetComponent(out NetworkObject parentNetworkObject)) ||
                !heavyNetworkObject.TrySetParent(parent.transform))
            {
                parent.ServerAnimationHandler.NetworkAnimator.SetTrigger(KFailedPickupTrigger);
                return false;
            }

            parent.HeldNetworkObject.Value = heavyNetworkObject.NetworkObjectId;

            Data.TargetIds = new ulong[] { heavyNetworkObject.NetworkObjectId };

            // clear current target on successful parenting attempt
            parent.TargetId.Value = 0;

            // snap to face the right direction
            if (Data.Direction != Vector3.zero)
            {
                parent.transform.forward = Data.Direction;
            }

            // try to set the heavy object follow the hand bone transform, through PositionConstraint component
            var positionConstraint = heavyNetworkObject.GetComponent<PositionConstraint>();
            if (positionConstraint)
            {
                if (parent.TryGetComponent(out ServerCharacter serverCharacter))
                {
                    var constraintSource = new ConstraintSource()
                    {
                        sourceTransform = serverCharacter.ClientCharacter.CharacterSwap.CharacterModel.handSocket.transform,
                        weight = 1
                    };
                    positionConstraint.AddSource(constraintSource);
                    positionConstraint.constraintActive = true;
                }
            }

            return true;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (!_mAttemptedPickup && Time.time > _mActionStartTime + Config.ExecTimeSeconds)
            {
                _mAttemptedPickup = true;
                if (!TryPickUp(clientCharacter))
                {
                    // pickup attempt unsuccessful; action can be terminated
                    return ActionConclusion.Stop;
                }
            }

            return ActionConclusion.Continue;
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (serverCharacter.LifeState == LifeState.Fainted)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(serverCharacter.HeldNetworkObject.Value, out var heavyNetworkObject))
                {
                    heavyNetworkObject.transform.SetParent(null);
                }
                serverCharacter.HeldNetworkObject.Value = 0;
            }
        }
    }
}
