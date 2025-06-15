using System;
using Unity.Collections;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Multiplayer.Samples.SocialHub.UI;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(Rigidbody))]
    class AvatarTransform : PhysicsObjectMotion, INetworkUpdateSystem
    {
        [SerializeField]
        PlayerInput m_PlayerInput;
        [SerializeField]
        AvatarInteractions m_AvatarInteractions;
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        Camera _mMainCamera;

        PlayersTopUIController _mTopUIController;

        NetworkVariable<FixedString32Bytes> _mPlayerName = new NetworkVariable<FixedString32Bytes>(string.Empty, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
        NetworkVariable<FixedString32Bytes> _mPlayerId = new NetworkVariable<FixedString32Bytes>(string.Empty, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);


        public override void OnNetworkSpawn()
        {
            gameObject.name = $"[Client-{OwnerClientId}]{name}";

            _mTopUIController = FindFirstObjectByType<PlayersTopUIController>();
            _mPlayerName.OnValueChanged += OnPlayerNameChanged;
            _mPlayerId.OnValueChanged += OnPlayerIdChanged;
            OnPlayerNameChanged(string.Empty, _mPlayerName.Value);

            if (!HasAuthority)
            {
                base.OnNetworkSpawn();
                return;
            }

            _mPlayerId.Value = new FixedString32Bytes(AuthenticationService.Instance.PlayerId);
            _mPlayerName.Value = new FixedString32Bytes(UIUtils.ExtractPlayerNameFromAuthUserName(AuthenticationService.Instance.PlayerName));
            m_PlayerInput.enabled = true;
            GameInput.Actions.Player.Jump.performed += OnJumped;
            m_AvatarInteractions.enabled = true;
            m_PhysicsPlayerController.enabled = true;
            Rigidbody.isKinematic = false;
            Rigidbody.freezeRotation = true;
            Rigidbody.linearVelocity = Vector3.zero;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(transform);
                _mMainCamera = Camera.main;
            }
            else
            {
                Debug.LogError("CameraControl not found on the Main Camera or Main Camera is missing.");
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            GameInput.Actions.Player.Jump.performed -= OnJumped;

            this.UnregisterAllNetworkUpdates();

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(null);
            }

            _mTopUIController?.RemovePlayer(gameObject);
        }

        void OnJumped(InputAction.CallbackContext _)
        {
            m_PhysicsPlayerController.SetJump(true);
        }

        void OnTransformUpdate()
        {
            if (_mMainCamera != null)
            {
                var forward = _mMainCamera.transform.forward;
                var right = _mMainCamera.transform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
                var movement = forward * moveInput.y + right * moveInput.x;
                m_PhysicsPlayerController.SetMovement(movement);
                var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
                m_PhysicsPlayerController.SetSprint(isSprinting);
            }
        }

        void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
        {
            _mTopUIController.AddOrUpdatePlayer(gameObject, newValue.Value,_mPlayerId.Value.Value);
        }

        void OnPlayerIdChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
        {
            _mTopUIController.AddOrUpdatePlayer(gameObject, _mPlayerName.Value.Value,newValue.Value);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    OnTransformUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    m_PhysicsPlayerController.OnFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
    }
}
