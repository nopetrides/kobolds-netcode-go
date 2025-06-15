using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.Actions;
using UnityEngine;
using Action = Unity.BossRoom.Gameplay.Actions.Action;
using Random = UnityEngine.Random;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character.AI
{
    public class AttackAIState : AIState
    {
        private AIBrain _mBrain;
        private ServerActionPlayer _mServerActionPlayer;
        private ServerCharacter _mFoe;
        private Action _mCurAttackAction;

        List<Action> _mAttackActions;

        public AttackAIState(AIBrain brain, ServerActionPlayer serverActionPlayer)
        {
            _mBrain = brain;
            _mServerActionPlayer = serverActionPlayer;
        }

        public override bool IsEligible()
        {
            return _mFoe != null || ChooseFoe() != null;
        }

        public override void Initialize()
        {
            _mAttackActions = new List<Action>();
            if (_mBrain.CharacterData.Skill1 != null)
            {
                _mAttackActions.Add(_mBrain.CharacterData.Skill1);
            }
            if (_mBrain.CharacterData.Skill2 != null)
            {
                _mAttackActions.Add(_mBrain.CharacterData.Skill2);
            }
            if (_mBrain.CharacterData.Skill3 != null)
            {
                _mAttackActions.Add(_mBrain.CharacterData.Skill3);
            }

            // pick a starting attack action from the possible
            _mCurAttackAction = _mAttackActions[Random.Range(0, _mAttackActions.Count)];

            // clear any old foe info; we'll choose a new one in Update()
            _mFoe = null;
        }

        public override void Update()
        {
            if (!_mBrain.IsAppropriateFoe(_mFoe))
            {
                // time for a new foe!
                _mFoe = ChooseFoe();
                // whatever we used to be doing, stop that. New plan is coming!
                _mServerActionPlayer.ClearActions(true);
            }

            // if we're out of foes, stop! IsEligible() will now return false so we'll soon switch to a new state
            if (!_mFoe)
            {
                return;
            }

            // see if we're already chasing or attacking our active foe!
            if (_mServerActionPlayer.GetActiveActionInfo(out var info))
            {
                if (GameDataSource.Instance.GetActionPrototypeByID(info.ActionID).IsChaseAction)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == _mFoe.NetworkObjectId)
                    {
                        // yep we're chasing our foe; all set! (The attack is enqueued after it)
                        return;
                    }
                }
                else if (info.ActionID == _mCurAttackAction.ActionID)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == _mFoe.NetworkObjectId)
                    {
                        // yep we're attacking our foe; all set!
                        return;
                    }
                }
                else if (GameDataSource.Instance.GetActionPrototypeByID(info.ActionID).IsStunAction)
                {
                    // we can't do anything right now. We're stunned!
                    return;
                }
            }

            // choose the attack to use
            _mCurAttackAction = ChooseAttack();
            if (_mCurAttackAction == null)
            {
                // no actions are usable right now
                return;
            }

            // attack!
            var attackData = new ActionRequestData
            {
                ActionID = _mCurAttackAction.ActionID,
                TargetIds = new ulong[] { _mFoe.NetworkObjectId },
                ShouldClose = true,
                Direction = _mBrain.GetMyServerCharacter().PhysicsWrapper.Transform.forward
            };
            _mServerActionPlayer.PlayAction(ref attackData);
        }

        /// <summary>
        /// Picks the most appropriate foe for us to attack right now, or null if none are appropriate
        /// (Currently just chooses the foe closest to us in distance)
        /// </summary>
        /// <returns></returns>
        private ServerCharacter ChooseFoe()
        {
            Vector3 myPosition = _mBrain.GetMyServerCharacter().PhysicsWrapper.Transform.position;

            float closestDistanceSqr = int.MaxValue;
            ServerCharacter closestFoe = null;
            foreach (var foe in _mBrain.GetHatedEnemies())
            {
                float distanceSqr = (myPosition - foe.PhysicsWrapper.Transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestFoe = foe;
                }
            }
            return closestFoe;
        }

        /// <summary>
        /// Randomly picks a usable attack. If no actions are usable right now, returns null.
        /// </summary>
        /// <returns>Action to attack with, or null</returns>
        private Action ChooseAttack()
        {
            // make a random choice
            int idx = Random.Range(0, _mAttackActions.Count);

            // now iterate through our options to find one that's currently usable
            bool anyUsable;
            do
            {
                anyUsable = false;
                foreach (var attack in _mAttackActions)
                {
                    if (_mServerActionPlayer.IsReuseTimeElapsed(attack.ActionID))
                    {
                        anyUsable = true;
                        if (idx == 0)
                        {
                            return attack;
                        }
                        --idx;
                    }
                }
            } while (anyUsable);

            // none of our actions are available now
            return null;
        }
    }
}
