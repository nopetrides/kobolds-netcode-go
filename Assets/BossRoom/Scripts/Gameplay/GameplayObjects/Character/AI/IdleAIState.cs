using System;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character.AI
{
    public class IdleAIState : AIState
    {
        private AIBrain _mBrain;

        public IdleAIState(AIBrain brain)
        {
            _mBrain = brain;
        }

        public override bool IsEligible()
        {
            return _mBrain.GetHatedEnemies().Count == 0;
        }

        public override void Initialize()
        {
        }

        public override void Update()
        {
            // while idle, we are scanning for jerks to hate
            DetectFoes();
        }

        protected void DetectFoes()
        {
            float detectionRange = _mBrain.DetectRange;
            // we are doing this check every Update, so we'll use square-magnitude distance to avoid the expensive sqrt (that's implicit in Vector3.magnitude)
            float detectionRangeSqr = detectionRange * detectionRange;
            Vector3 position = _mBrain.GetMyServerCharacter().PhysicsWrapper.Transform.position;

            // in this game, NPCs only attack players (and never other NPCs), so we can just iterate over the players to see if any are nearby
            foreach (var character in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (_mBrain.IsAppropriateFoe(character) && (character.PhysicsWrapper.Transform.position - position).sqrMagnitude <= detectionRangeSqr)
                {
                    _mBrain.Hate(character);
                }
            }
        }
    }
}
