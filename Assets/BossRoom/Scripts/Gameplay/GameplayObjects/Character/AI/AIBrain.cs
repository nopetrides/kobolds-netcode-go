using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.Actions;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character.AI
{
    /// <summary>
    /// Handles enemy AI. Contains AIStateLogics that handle some of the details,
    /// and has various utility functions that are called by those AIStateLogics
    /// </summary>
    public class AIBrain
    {
        private enum AIStateType
        {
            Attack,
            //WANDER,
            Idle,
        }

        static readonly AIStateType[] KaiStates = (AIStateType[])Enum.GetValues(typeof(AIStateType));

        private ServerCharacter _mServerCharacter;
        private ServerActionPlayer _mServerActionPlayer;
        private AIStateType _mCurrentState;
        private Dictionary<AIStateType, AIState> _mLogics;
        private List<ServerCharacter> _mHatedEnemies;

        /// <summary>
        /// If we are created by a spawner, the spawner might override our detection radius
        /// -1 is a sentinel value meaning "no override"
        /// </summary>
        private float _mDetectRangeOverride = -1;

        public AIBrain(ServerCharacter me, ServerActionPlayer myServerActionPlayer)
        {
            _mServerCharacter = me;
            _mServerActionPlayer = myServerActionPlayer;

            _mLogics = new Dictionary<AIStateType, AIState>
            {
                [AIStateType.Idle] = new IdleAIState(this),
                //[ AIStateType.WANDER ] = new WanderAIState(this), // not written yet
                [AIStateType.Attack] = new AttackAIState(this, _mServerActionPlayer),
            };
            _mHatedEnemies = new List<ServerCharacter>();
            _mCurrentState = AIStateType.Idle;
        }

        /// <summary>
        /// Should be called by the AIBrain's owner each Update()
        /// </summary>
        public void Update()
        {
            AIStateType newState = FindBestEligibleAIState();
            if (_mCurrentState != newState)
            {
                _mLogics[newState].Initialize();
            }
            _mCurrentState = newState;
            _mLogics[_mCurrentState].Update();
        }

        /// <summary>
        /// Called when we received some HP. Positive HP is healing, negative is damage.
        /// </summary>
        /// <param name="inflicter">The person who hurt or healed us. May be null. </param>
        /// <param name="amount">The amount of HP received. Negative is damage. </param>
        public void ReceiveHp(ServerCharacter inflicter, int amount)
        {
            if (inflicter != null && amount < 0)
            {
                Hate(inflicter);
            }
        }

        private AIStateType FindBestEligibleAIState()
        {
            // for now we assume the AI states are in order of appropriateness,
            // which may be nonsensical when there are more states
            foreach (AIStateType aiStateType in KaiStates)
            {
                if (_mLogics[aiStateType].IsEligible())
                {
                    return aiStateType;
                }
            }

            Debug.LogError("No AI states are valid!?!");
            return AIStateType.Idle;
        }

        /// <summary>
        /// Returns true if it be appropriate for us to murder this character, starting right now!
        /// </summary>
        public bool IsAppropriateFoe(ServerCharacter potentialFoe)
        {
            if (potentialFoe == null ||
                potentialFoe.IsNpc ||
                potentialFoe.LifeState != LifeState.Alive ||
                potentialFoe.IsStealthy.Value)
            {
                return false;
            }

            // Also, we could use NavMesh.Raycast() to see if we have line of sight to foe?
            return true;
        }

        /// <summary>
        /// Notify the AIBrain that we should consider this character an enemy.
        /// </summary>
        /// <param name="character"></param>
        public void Hate(ServerCharacter character)
        {
            if (!_mHatedEnemies.Contains(character))
            {
                _mHatedEnemies.Add(character);
            }
        }

        /// <summary>
        /// Return the raw list of hated enemies -- treat as read-only!
        /// </summary>
        public List<ServerCharacter> GetHatedEnemies()
        {
            // first we clean the list -- remove any enemies that have disappeared (became null), are dead, etc.
            for (int i = _mHatedEnemies.Count - 1; i >= 0; i--)
            {
                if (!IsAppropriateFoe(_mHatedEnemies[i]))
                {
                    _mHatedEnemies.RemoveAt(i);
                }
            }
            return _mHatedEnemies;
        }

        /// <summary>
        /// Retrieve info about who we are. Treat as read-only!
        /// </summary>
        /// <returns></returns>
        public ServerCharacter GetMyServerCharacter()
        {
            return _mServerCharacter;
        }

        /// <summary>
        /// Convenience getter that returns the CharacterData associated with this creature.
        /// </summary>
        public CharacterClass CharacterData
        {
            get
            {
                return GameDataSource.Instance.CharacterDataByType[_mServerCharacter.CharacterType];
            }
        }

        /// <summary>
        /// The range at which this character can detect enemies, in meters.
        /// This is usually the same value as is indicated by our game data, but it
        /// can be dynamically overridden.
        /// </summary>
        public float DetectRange
        {
            get
            {
                return (_mDetectRangeOverride == -1) ? CharacterData.DetectRange : _mDetectRangeOverride;
            }

            set
            {
                _mDetectRangeOverride = value;
            }
        }

    }
}
