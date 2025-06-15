using UnityEngine;
using System;

namespace Unity.Multiplayer.Samples.SocialHub.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    class PhysicsPlayerController : MonoBehaviour
    {
        [SerializeField]
        Rigidbody m_Rigidbody;

        [SerializeField]
        PhysicsPlayerControllerSettings m_PhysicsPlayerControllerSettings;

        // cached grounded check
        internal bool Grounded { get; private set; }

        RaycastHit[] _mRaycastHits = new RaycastHit[1];
        Ray _mRay;

        Vector3 _mMovement;
        bool _mJump;
        bool _mSprint;

        internal event Action PlayerJumped;

        internal void OnFixedUpdate()
        {
            if (m_Rigidbody != null && m_Rigidbody.isKinematic)
            {
                return;
            }

            UpdateGroundedStatus();
            ApplyMovement();
            ApplyJump();
            ApplyDrag();
            ApplyCustomGravity();
        }

        void UpdateGroundedStatus()
        {
            Grounded = IsGrounded();
        }

        bool IsGrounded()
        {
            // Perform a raycast to check if the character is grounded
            _mRay.origin = m_Rigidbody.worldCenterOfMass;
            _mRay.direction = Vector3.down;
            return UnityEngine.Physics.RaycastNonAlloc(_mRay, _mRaycastHits, m_PhysicsPlayerControllerSettings.GroundCheckDistance) > 0;
        }

        void ApplyMovement()
        {
            if (Mathf.Approximately(_mMovement.magnitude, 0f))
            {
                return;
            }

            var velocity = m_Rigidbody.linearVelocity;
            var desiredVelocity = _mMovement * (_mSprint ? m_PhysicsPlayerControllerSettings.SprintSpeed : m_PhysicsPlayerControllerSettings.WalkSpeed);
            var targetVelocity = new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z);
            var velocityChange = targetVelocity - velocity;

            if (Grounded)
            {
                // Apply force proportional to acceleration while grounded
                var force = velocityChange * m_PhysicsPlayerControllerSettings.Acceleration;
                m_Rigidbody.AddForce(force, ForceMode.Force);
            }
            else
            {
                // Apply reduced force in the air for air control
                var force = velocityChange * (m_PhysicsPlayerControllerSettings.Acceleration * m_PhysicsPlayerControllerSettings.AirControlFactor);
                m_Rigidbody.AddForce(force, ForceMode.Force);
            }

            // maybe add magnitude check?
            var targetAngle = Mathf.Atan2(_mMovement.x, _mMovement.z) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(0, targetAngle, 0);
            var smoothRotation = Quaternion.Lerp(m_Rigidbody.rotation, targetRotation, Time.fixedDeltaTime * m_PhysicsPlayerControllerSettings.RotationSpeed);
            m_Rigidbody.MoveRotation(smoothRotation);

            _mMovement = Vector3.zero;
        }

        void ApplyJump()
        {
            if (_mJump && Grounded)
            {
                m_Rigidbody.AddForce(Vector3.up * m_PhysicsPlayerControllerSettings.JumpImpusle, ForceMode.Impulse);
                PlayerJumped?.Invoke();
            }
            _mJump = false;
        }

        void ApplyDrag()
        {
            var groundVelocity = m_Rigidbody.linearVelocity;
            groundVelocity.y = 0f;
            if (groundVelocity.magnitude > 0f)
            {
                // Apply deceleration force to stop movement
                var dragForce = -m_PhysicsPlayerControllerSettings.DragCoefficient * groundVelocity.magnitude * groundVelocity;
                m_Rigidbody.AddForce(dragForce, ForceMode.Acceleration);
            }
        }

        void ApplyCustomGravity()
        {
            var customGravity = UnityEngine.Physics.gravity * (m_PhysicsPlayerControllerSettings.CustomGravityMultiplier - 1);
            m_Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
        }

        public void SetMovement(Vector3 movement)
        {
            _mMovement = movement;
        }

        public void SetJump(bool jump)
        {
            if (jump)
            {
                _mJump = true;
            }
        }

        public void SetSprint(bool sprint)
        {
            _mSprint = sprint;
        }
    }
}
