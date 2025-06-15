using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Action for dropping "Heavy" items.
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Drop Action")]
    public class DropAction : Action
    {
        float _mActionStartTime;

        NetworkObject _mHeldNetworkObject;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            _mActionStartTime = Time.time;

            // play animation of dropping a heavy object, if one is already held
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    serverCharacter.HeldNetworkObject.Value, out var heldObject))
            {
                _mHeldNetworkObject = heldObject;

                Data.TargetIds = null;

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
            _mHeldNetworkObject = null;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (Time.time > _mActionStartTime + Config.ExecTimeSeconds)
            {
                // drop the pot in space
                _mHeldNetworkObject.transform.SetParent(null);
                clientCharacter.HeldNetworkObject.Value = 0;

                return ActionConclusion.Stop;
            }

            return ActionConclusion.Continue;
        }
    }
}
