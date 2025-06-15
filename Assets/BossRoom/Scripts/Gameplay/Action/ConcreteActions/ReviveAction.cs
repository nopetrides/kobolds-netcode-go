using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Gameplay.Actions
{
    [CreateAssetMenu(menuName = "BossRoom/Actions/Revive Action")]
    public class ReviveAction : Action
    {
        private bool _mExecFired;
        private ServerCharacter _mTargetCharacter;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            if (MData.TargetIds == null || MData.TargetIds.Length == 0 || !NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(MData.TargetIds[0]))
            {
                Debug.Log("Failed to start ReviveAction. The target entity  wasn't submitted or doesn't exist anymore");
                return false;
            }

            var targetNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[MData.TargetIds[0]];
            _mTargetCharacter = targetNetworkObject.GetComponent<ServerCharacter>();

            serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            _mExecFired = false;
            _mTargetCharacter = null;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (!_mExecFired && Time.time - TimeStarted >= Config.ExecTimeSeconds)
            {
                _mExecFired = true;

                if (_mTargetCharacter.LifeState == LifeState.Fainted)
                {
                    Assert.IsTrue(Config.Amount > 0, "Revive amount must be greater than 0.");
                    _mTargetCharacter.Revive(clientCharacter, Config.Amount);
                }
                else
                {
                    //cancel the action if the target is alive!
                    Cancel(clientCharacter);
                    return false;
                }
            }

            return true;
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }
        }
    }
}
