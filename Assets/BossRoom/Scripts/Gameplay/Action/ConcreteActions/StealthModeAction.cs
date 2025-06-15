using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Causes the character to become hidden to enemies and other players. Notes:
    /// - Stealth starts after the ExecTimeSeconds has elapsed. If they are attacked during the Exec time, stealth is aborted.
    /// - Stealth ends when the player attacks or is damaged.
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Stealth Mode Action")]
    public class StealthModeAction : Action
    {
        private bool _mIsStealthStarted = false;
        private bool _mIsStealthEnded = false;

        /// <summary>
        /// When non-null, a list of all graphics spawned.
        /// (If null, means we haven't been running long enough yet, or we aren't using any graphics because we're invisible on this client)
        /// These are created from the Description.Spawns list. Each prefab in that list should have a SpecialFXGraphic component.
        /// </summary>
        private List<SpecialFXGraphic> _mSpawnedGraphics = null;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            serverCharacter.ClientCharacter.ClientPlayActionRpc(Data);

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            _mIsStealthEnded = false;
            _mIsStealthStarted = false;
            _mSpawnedGraphics = null;
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return TimeRunning >= Config.ExecTimeSeconds;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && !_mIsStealthStarted && !_mIsStealthEnded)
            {
                // start actual stealth-mode... NOW!
                _mIsStealthStarted = true;
                clientCharacter.IsStealthy.Value = true;
            }
            return !_mIsStealthEnded;
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }

            EndStealth(serverCharacter);
        }

        public override void OnGameplayActivity(ServerCharacter serverCharacter, GameplayActivity activityType)
        {
            // we break stealth after using an attack. (Or after being hit, which could happen during exec time before we're stealthed, or even afterwards, such as from an AoE attack)
            if (activityType == GameplayActivity.UsingAttackAction || activityType == GameplayActivity.AttackedByEnemy)
            {
                EndStealth(serverCharacter);
            }
        }

        private void EndStealth(ServerCharacter parent)
        {
            if (!_mIsStealthEnded)
            {
                _mIsStealthEnded = true;
                if (_mIsStealthStarted)
                {
                    parent.IsStealthy.Value = false;
                }

                // note that we cancel the ActionFX here, and NOT in Cancel(). That's to handle the case where someone
                // presses the Stealth button twice in a row: "end this Stealth action and start a new one". If we cancelled
                // all actions of this type in Cancel(), we'd end up cancelling both the old AND the new one, because
                // the new one would already be in the clients' actionFX queue.
                parent.ClientCharacter.ClientCancelActionsByPrototypeIDRpc(ActionID);
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && _mSpawnedGraphics == null && clientCharacter.IsOwner)
            {
                _mSpawnedGraphics = InstantiateSpecialFXGraphics(clientCharacter.transform, true);
            }

            return ActionConclusion.Continue;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            if (_mSpawnedGraphics != null)
            {
                foreach (var graphic in _mSpawnedGraphics)
                {
                    if (graphic)
                    {
                        graphic.transform.SetParent(null);
                        graphic.Shutdown();
                    }
                }
            }
        }

    }
}
