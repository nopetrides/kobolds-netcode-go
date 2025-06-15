using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class MeleeAction
    {
        //have we actually played an impact? This won't necessarily happen for all swings. Sometimes you're just swinging at space.
        private bool _mImpactPlayed;

        /// <summary>
        /// When we detect if our original target is still around, we use a bit of padding on the range check.
        /// </summary>
        private const float KRangePadding = 3f;

        /// <summary>
        /// List of active special graphics playing on the target.
        /// </summary>
        private List<SpecialFXGraphic> _mSpawnedGraphics = null;

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            base.OnStartClient(clientCharacter);

            // we can optionally have special particles that should play on the target. If so, add them now.
            // (don't wait until impact, because the particles need to start sooner!)
            if (Data.TargetIds != null
                && Data.TargetIds.Length > 0
                && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = Config.Range + KRangePadding;

                Vector3 targetPosition;
                if (PhysicsWrapper.TryGetPhysicsWrapper(Data.TargetIds[0], out var physicsWrapper))
                {
                    targetPosition = physicsWrapper.Transform.position;
                }
                else
                {
                    targetPosition = targetNetworkObj.transform.position;
                }

                if ((clientCharacter.transform.position - targetPosition).sqrMagnitude < (padRange * padRange))
                {
                    // target is in range! Play the graphics
                    _mSpawnedGraphics = InstantiateSpecialFXGraphics(physicsWrapper ? physicsWrapper.Transform : targetNetworkObj.transform, true);
                }
            }

            return true;
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }

        public override void OnAnimEventClient(ClientCharacter clientCharacter, string id)
        {
            if (id == "impact" && !_mImpactPlayed)
            {
                PlayHitReact(clientCharacter);
            }
        }

        public override void EndClient(ClientCharacter clientCharacter)
        {
            //if this didn't already happen, make sure it gets a chance to run. This could have failed to run because
            //our animationclip didn't have the "impact" event properly configured (as one possibility).
            PlayHitReact(clientCharacter);
            base.EndClient(clientCharacter);
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            // if we had any special target graphics, tell them we're done
            if (_mSpawnedGraphics != null)
            {
                foreach (var spawnedGraphic in _mSpawnedGraphics)
                {
                    if (spawnedGraphic)
                    {
                        spawnedGraphic.Shutdown();
                    }
                }
            }
        }

        void PlayHitReact(ClientCharacter parent)
        {
            if (_mImpactPlayed) { return; }

            _mImpactPlayed = true;

            if (NetworkManager.Singleton.IsServer)
            {
                return;
            }

            //Is my original target still in range? Then definitely get him!
            if (Data.TargetIds != null &&
                Data.TargetIds.Length > 0 &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out var targetNetworkObj)
                && targetNetworkObj != null)
            {
                float padRange = Config.Range + KRangePadding;

                Vector3 targetPosition;
                if (PhysicsWrapper.TryGetPhysicsWrapper(Data.TargetIds[0], out var movementContainer))
                {
                    targetPosition = movementContainer.Transform.position;
                }
                else
                {
                    targetPosition = targetNetworkObj.transform.position;
                }

                if ((parent.transform.position - targetPosition).sqrMagnitude < (padRange * padRange))
                {
                    if (targetNetworkObj.NetworkObjectId != parent.NetworkObjectId)
                    {
                        string hitAnim = Config.ReactAnim;
                        if (string.IsNullOrEmpty(hitAnim)) { hitAnim = KDefaultHitReact; }

                        if (targetNetworkObj.TryGetComponent<ServerCharacter>(out var serverCharacter)
                            && serverCharacter.ClientCharacter != null
                            && serverCharacter.ClientCharacter.OurAnimator)
                        {
                            serverCharacter.ClientCharacter.OurAnimator.SetTrigger(hitAnim);
                        }
                    }
                }
            }

            //in the future we may do another physics check to handle the case where a target "ran under our weapon".
            //But for now, if the original target is no longer present, then we just don't play our hit react on anything.
        }
    }
}
