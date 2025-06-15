using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(PhysicsPlayerController))]
    class AvatarNetworkAnimator : NetworkAnimator
    {
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        static readonly int KGroundedId = Animator.StringToHash("Grounded");
        static readonly int KMoveId = Animator.StringToHash("Move");
        static readonly int KJumpId = Animator.StringToHash("Jump");

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_PhysicsPlayerController.PlayerJumped += OnPlayerJumped;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_PhysicsPlayerController)
            {
                m_PhysicsPlayerController.PlayerJumped -= OnPlayerJumped;
            }
        }

        void OnPlayerJumped()
        {
            SetTrigger(KJumpId);
        }

        void LateUpdate()
        {
            if (!HasAuthority)
            {
                return;
            }

            Animator.SetBool(KGroundedId, m_PhysicsPlayerController.Grounded);
            var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
            Animator.SetFloat(KMoveId, moveInput.magnitude * (isSprinting ? 2f : 1f));
        }
    }
}
