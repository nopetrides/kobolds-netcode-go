using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// This represents a "charge-across-the-screen" attack. The character deals damage to every enemy hit.
    /// </summary>
    /// <remarks>
    /// It's called "Trample" instead of "Charge" because we already use the word "charge"
    /// to describe "charging up" an attack.
    /// </remarks>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Trample Action")]
    public partial class TrampleAction : Action
    {
        public StunnedAction StunnedActionPrototype;

        /// <summary>
        /// This is an internal indicator of which stage of the Action we're in.
        /// </summary>
        private enum ActionStage
        {
            Windup,     // performing animations prior to actually moving
            Charging,   // running across the screen and hitting characters
            Complete,   // ending action
        }

        /// <summary>
        /// When we begin our charge-attack, anyone within this range is treated as having already been touching us.
        /// </summary>
        private const float KPhysicalTouchDistance = 1;

        /// <summary>
        /// Our ActionStage, as of last Update
        /// </summary>
        private ActionStage _mPreviousStage;

        /// <summary>
        /// Keeps track of which Colliders we've already hit, so that our attack doesn't hit the same character twice.
        /// </summary>
        private HashSet<Collider> _mCollidedAlready = new HashSet<Collider>();

        /// <summary>
        /// Set to true in the special-case scenario where we are stunned by one of the characters we tried to trample
        /// </summary>
        private bool _mWasStunned;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            _mPreviousStage = ActionStage.Windup;

            if (MData.TargetIds != null && MData.TargetIds.Length > 0)
            {
                NetworkObject initialTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[MData.TargetIds[0]];
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

            // reset our "stop" trigger (in case the previous run of the trample action was aborted due to e.g. being stunned)
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.ResetTrigger(Config.Anim2);
            }
            // start the animation sequence!
            if (!string.IsNullOrEmpty(Config.Anim))
            {
                serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            }

            serverCharacter.ClientCharacter.ClientPlayActionRpc(Data);
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            _mPreviousStage = default;
            _mCollidedAlready.Clear();
            _mSpawnedGraphics = null;
            _mWasStunned = false;
        }

        private ActionStage GetCurrentStage()
        {
            float timeSoFar = Time.time - TimeStarted;
            if (timeSoFar < Config.ExecTimeSeconds)
            {
                return ActionStage.Windup;
            }
            if (timeSoFar < Config.DurationSeconds)
            {
                return ActionStage.Charging;
            }
            return ActionStage.Complete;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            ActionStage newState = GetCurrentStage();
            if (newState != _mPreviousStage && newState == ActionStage.Charging)
            {
                // we've just started to charge across the screen! Anyone currently touching us gets hit
                SimulateCollisionWithNearbyFoes(clientCharacter);
                clientCharacter.Movement.StartForwardCharge(Config.MoveSpeed, Config.DurationSeconds - Config.ExecTimeSeconds);
            }

            _mPreviousStage = newState;
            return newState != ActionStage.Complete && !_mWasStunned;
        }

        /// <summary>
        /// We've crashed into a victim! This function determines what happens to them... and to us!
        /// It's possible for us to be stunned by our victim if they have a special power that allows that.
        /// This function checks for that special case; if we become stunned, the victim is entirely unharmed,
        /// and further collisions with other victims will also have no effect.
        /// </summary>
        /// <param name="victim">The character we've collided with</param>
        private void CollideWithVictim(ServerCharacter parent, ServerCharacter victim)
        {
            if (victim == parent)
            {
                // can't collide with ourselves!
                return;
            }

            if (_mWasStunned)
            {
                // someone already stunned us, so no further damage can happen
                return;
            }

            // if we collide with allies, we don't want to hurt them (but we do knock them back, see below)
            if (parent.IsNpc != victim.IsNpc)
            {
                // first see if this victim has the special ability to stun us!
                float chanceToStun = victim.GetBuffedValue(BuffableValue.ChanceToStunTramplers);
                if (chanceToStun > 0 && Random.Range(0, 1) < chanceToStun)
                {
                    // we're stunned! No collision behavior for the victim. Stun ourselves and abort.
                    StunSelf(parent);
                    return;
                }

                // We deal a certain amount of damage to our "initial" target and a different amount to all other victims.
                int damage;
                if (MData.TargetIds != null && MData.TargetIds.Length > 0 && MData.TargetIds[0] == victim.NetworkObjectId)
                {
                    damage = Config.Amount;
                }
                else
                {
                    damage = Config.SplashDamage;
                }

                if (victim.gameObject.TryGetComponent(out IDamageable damageable))
                {
                    damageable.ReceiveHp(parent, -damage);
                }
            }

            var victimMovement = victim.Movement;
            victimMovement.StartKnockback(parent.PhysicsWrapper.Transform.position, Config.KnockbackSpeed, Config.KnockbackDuration);
        }

        // called by owning class when parent's Collider collides with stuff
        public override void CollisionEntered(ServerCharacter serverCharacter, Collision collision)
        {
            // we only detect other possible victims when we start charging
            if (GetCurrentStage() != ActionStage.Charging)
                return;

            Collide(serverCharacter, collision.collider);
        }

        // here we handle colliding with anything (whether a victim or not)
        private void Collide(ServerCharacter parent, Collider collider)
        {
            if (_mCollidedAlready.Contains(collider))
                return; // already hit them!

            _mCollidedAlready.Add(collider);

            var victim = collider.gameObject.GetComponentInParent<ServerCharacter>();
            if (victim)
            {
                CollideWithVictim(parent, victim);
            }
            else if (!_mWasStunned)
            {
                // they aren't a living, breathing victim, but they might still be destructible...
                var damageable = collider.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.ReceiveHp(parent, -Config.SplashDamage);

                    // lastly, a special case: if the trampler runs into certain breakables, they are stunned!
                    if ((damageable.GetSpecialDamageFlags() & IDamageable.SpecialDamageFlags.StunOnTrample) == IDamageable.SpecialDamageFlags.StunOnTrample)
                    {
                        StunSelf(parent);
                    }
                }
            }
        }

        private void SimulateCollisionWithNearbyFoes(ServerCharacter parent)
        {
            // We don't get OnCollisionEnter() calls for things that are already collided with us!
            // So when we start charging across the screen, we check to see what's already touching us
            // (or close enough) and treat that like a collision.
            RaycastHit[] results;
            int numResults = ActionUtils.DetectNearbyEntities(true, true, parent.PhysicsWrapper.DamageCollider, KPhysicalTouchDistance, out results);
            for (int i = 0; i < numResults; i++)
            {
                Collide(parent, results[i].collider);
            }
        }

        private void StunSelf(ServerCharacter parent)
        {
            if (!_mWasStunned)
            {
                parent.Movement.CancelMove();
                parent.ClientCharacter.ClientCancelAllActionsRpc();
            }
            _mWasStunned = true;
        }

        public override bool ChainIntoNewAction(ref ActionRequestData newAction)
        {
            if (_mWasStunned)
            {
                newAction = ActionRequestData.Create(StunnedActionPrototype);
                newAction.ShouldQueue = false;
                return true;
            }
            return false;
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
