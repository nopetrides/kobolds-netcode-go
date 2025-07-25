using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Causes the attacker to teleport near a target spot, then perform a melee attack. The client
    /// visualization moves the character locally beforehand, making the character appear to dash to the
    /// destination spot.
    ///
    /// After the ExecTime has elapsed, the character is immune to damage until the action ends.
    ///
    /// Since the "Range" field means "range when we can teleport to our target", we need another
    /// field to mean "range of our melee attack after dashing". We'll use the "Radius" field of the
    /// ActionDescription for that.
    /// </summary>
    /// <remarks>
    /// See MeleeAction for relevant discussion about targeting; we use the same concept here: preferring
    /// the chosen target, but using whatever is actually within striking distance at time of attack.
    /// </remarks>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Dash Attack Action")]
    public class DashAttackAction : Action
    {
        private Vector3 _mTargetSpot;

        private bool _mDashed;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            // remember the exact spot we'll stop.
            _mTargetSpot = ActionUtils.GetDashDestination(serverCharacter.PhysicsWrapper.Transform, Data.Position, true, Config.Range, Config.Range);

            // snap to face our destination. This ensures the client visualization faces the right way while "pretending" to dash
            serverCharacter.PhysicsWrapper.Transform.LookAt(_mTargetSpot);

            serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            // tell clients to visualize this action
            serverCharacter.ClientCharacter.ClientPlayActionRpc(Data);

            return ActionConclusion.Continue;
        }

        public override void Reset()
        {
            base.Reset();
            _mTargetSpot = default;
            _mDashed = false;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }

        public override void End(ServerCharacter serverCharacter)
        {
            // Anim2 contains the name of the end-loop-sequence trigger
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }

            // we're done, time to teleport!
            serverCharacter.Movement.Teleport(_mTargetSpot);

            // and then swing!
            PerformMeleeAttack(serverCharacter);
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            // OtherAnimatorVariable contains the name of the cancellation trigger
            if (!string.IsNullOrEmpty(Config.OtherAnimatorVariable))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.OtherAnimatorVariable);
            }

            // because the client-side visualization of the action moves the character visualization around,
            // we need to explicitly end the client-side visuals when we abort
            serverCharacter.ClientCharacter.ClientCancelActionsByPrototypeIDRpc(ActionID);

        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && buffType == BuffableValue.PercentDamageReceived)
            {
                // we suffer no damage during the "dash" (client-side pretend movement)
                buffedValue = 0;
            }
        }

        private void PerformMeleeAttack(ServerCharacter parent)
        {
            // perform a typical melee-hit. But note that we are using the Radius field for range, not the Range field!
            IDamageable foe = MeleeAction.GetIdealMeleeFoe(Config.IsFriendly ^ parent.IsNpc,
                parent.PhysicsWrapper.DamageCollider,
                                                            Config.Radius,
                                                            (Data.TargetIds != null && Data.TargetIds.Length > 0 ? Data.TargetIds[0] : 0));

            if (foe != null)
            {
                foe.ReceiveHp(parent, -Config.Amount);
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (_mDashed) { return ActionConclusion.Stop; } // we're done!

            return ActionConclusion.Continue;
        }
    }
}
