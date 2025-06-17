using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    public enum MovementState
    {
        Idle = 0,
        PathFollowing = 1,
        Charging = 2,
        Knockback = 3,
    }

    /// <summary>
    /// Component responsible for moving a character on the server side based on inputs.
    /// </summary>
    /*[RequireComponent(typeof(NetworkCharacterState), typeof(NavMeshAgent), typeof(ServerCharacter)), RequireComponent(typeof(Rigidbody))]*/
    public class ServerCharacterMovement : NetworkBehaviour
    {
        [SerializeField]
        NavMeshAgent m_NavMeshAgent;

        [SerializeField]
        Rigidbody m_Rigidbody;

        private NavigationSystem _mNavigationSystem;

        private DynamicNavPath _mNavPath;

        private MovementState _mMovementState;

        MovementStatus _mPreviousState;

        [SerializeField]
        private ServerCharacter m_CharLogic;

        // when we are in charging and knockback mode, we use these additional variables
        private float _mForcedSpeed;
        private float _mSpecialModeDurationRemaining;

        // this one is specific to knockback mode
        private Vector3 _mKnockbackVector;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TeleportModeActivated { get; set; }

        const float KCheatSpeed = 20;

        public bool SpeedCheatActivated { get; set; }
#endif

        void Awake()
        {
            // disable this NetworkBehavior until it is spawned
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Only enable server component on servers
                enabled = true;

                // On the server enable navMeshAgent and initialize
                m_NavMeshAgent.enabled = true;
                _mNavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
                _mNavPath = new DynamicNavPath(m_NavMeshAgent, _mNavigationSystem);
            }
        }

        /// <summary>
        /// Sets a movement target. We will path to this position, avoiding static obstacles.
        /// </summary>
        /// <param name="position">Position in world space to path to. </param>
        public void SetMovementTarget(Vector3 position)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TeleportModeActivated)
            {
                Teleport(position);
                return;
            }
#endif
            _mMovementState = MovementState.PathFollowing;
            _mNavPath.SetTargetPosition(position);
        }

        public void StartForwardCharge(float speed, float duration)
        {
            _mNavPath.Clear();
            _mMovementState = MovementState.Charging;
            _mForcedSpeed = speed;
            _mSpecialModeDurationRemaining = duration;
        }

        public void StartKnockback(Vector3 knocker, float speed, float duration)
        {
            _mNavPath.Clear();
            _mMovementState = MovementState.Knockback;
            _mKnockbackVector = transform.position - knocker;
            _mForcedSpeed = speed;
            _mSpecialModeDurationRemaining = duration;
        }

        /// <summary>
        /// Follow the given transform until it is reached.
        /// </summary>
        /// <param name="followTransform">The transform to follow</param>
        public void FollowTransform(Transform followTransform)
        {
            _mMovementState = MovementState.PathFollowing;
            _mNavPath.FollowTransform(followTransform);
        }

        /// <summary>
        /// Returns true if the current movement-mode is unabortable (e.g. a knockback effect)
        /// </summary>
        /// <returns></returns>
        public bool IsPerformingForcedMovement()
        {
            return _mMovementState == MovementState.Knockback || _mMovementState == MovementState.Charging;
        }

        /// <summary>
        /// Returns true if the character is actively moving, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsMoving()
        {
            return _mMovementState != MovementState.Idle;
        }

        /// <summary>
        /// Cancels any moves that are currently in progress.
        /// </summary>
        public void CancelMove()
        {
            if (_mNavPath != null)
            {
                _mNavPath.Clear();
            }
            _mMovementState = MovementState.Idle;
        }

        /// <summary>
        /// Instantly moves the character to a new position. NOTE: this cancels any active movement operation!
        /// This does not notify the client that the movement occurred due to teleportation, so that needs to
        /// happen in some other way, such as with the custom action visualization in DashAttackActionFX. (Without
        /// this, the clients will animate the character moving to the new destination spot, rather than instantly
        /// appearing in the new spot.)
        /// </summary>
        /// <param name="newPosition">new coordinates the character should be at</param>
        public void Teleport(Vector3 newPosition)
        {
            CancelMove();
            if (!m_NavMeshAgent.Warp(newPosition))
            {
                // warping failed! We're off the navmesh somehow. Weird... but we can still teleport
                Debug.LogWarning($"NavMeshAgent.Warp({newPosition}) failed!", gameObject);
                transform.position = newPosition;
            }

            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            PerformMovement();

            var currentState = GetMovementStatus(_mMovementState);
            if (_mPreviousState != currentState)
            {
                m_CharLogic.MovementStatus.Value = currentState;
                _mPreviousState = currentState;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_mNavPath != null)
            {
                _mNavPath.Dispose();
            }
            if (IsServer)
            {
                // Disable server components when despawning
                enabled = false;
                m_NavMeshAgent.enabled = false;
            }
        }

        private void PerformMovement()
        {
            if (_mMovementState == MovementState.Idle)
                return;

            Vector3 movementVector;

            if (_mMovementState == MovementState.Charging)
            {
                // if we're done charging, stop moving
                _mSpecialModeDurationRemaining -= Time.fixedDeltaTime;
                if (_mSpecialModeDurationRemaining <= 0)
                {
                    _mMovementState = MovementState.Idle;
                    return;
                }

                var desiredMovementAmount = _mForcedSpeed * Time.fixedDeltaTime;
                movementVector = transform.forward * desiredMovementAmount;
            }
            else if (_mMovementState == MovementState.Knockback)
            {
                _mSpecialModeDurationRemaining -= Time.fixedDeltaTime;
                if (_mSpecialModeDurationRemaining <= 0)
                {
                    _mMovementState = MovementState.Idle;
                    return;
                }

                var desiredMovementAmount = _mForcedSpeed * Time.fixedDeltaTime;
                movementVector = _mKnockbackVector * desiredMovementAmount;
            }
            else
            {
                var desiredMovementAmount = GetBaseMovementSpeed() * Time.fixedDeltaTime;
                movementVector = _mNavPath.MoveAlongPath(desiredMovementAmount);

                // If we didn't move stop moving.
                if (movementVector == Vector3.zero)
                {
                    _mMovementState = MovementState.Idle;
                    return;
                }
            }

            m_NavMeshAgent.Move(movementVector);
            transform.rotation = Quaternion.LookRotation(movementVector);

            // After moving adjust the position of the dynamic rigidbody.
            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;
        }

        /// <summary>
        /// Retrieves the speed for this character's class.
        /// </summary>
        private float GetBaseMovementSpeed()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (SpeedCheatActivated)
            {
                return KCheatSpeed;
            }
#endif
            CharacterClass characterClass = GameDataSource.Instance.CharacterDataByType[m_CharLogic.CharacterType];
            Assert.IsNotNull(characterClass, $"No CharacterClass data for character type {m_CharLogic.CharacterType}");
            return characterClass.Speed;
        }

        /// <summary>
        /// Determines the appropriate MovementStatus for the character. The
        /// MovementStatus is used by the client code when animating the character.
        /// </summary>
        private MovementStatus GetMovementStatus(MovementState movementState)
        {
            switch (movementState)
            {
                case MovementState.Idle:
                    return MovementStatus.Idle;
                case MovementState.Knockback:
                    return MovementStatus.Uncontrolled;
                default:
                    return MovementStatus.Normal;
            }
        }
    }
}
