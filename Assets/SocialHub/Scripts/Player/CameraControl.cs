using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Multiplayer.Samples.SocialHub.Input;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    class CameraControl : MonoBehaviour
    {
        [SerializeField]
        CinemachineCamera m_FreeLookVCamera;
        const float KMouseLookMultiplier = 15f;
        const float KGamepadLookMultiplier = 100f;
        const float KVerticalScaling = 0.01f;

        Transform _mFollowTransform;
        bool _mCameraMovementLock;
        bool _mIsRotatePressed;
        CinemachineOrbitalFollow _mOrbitalFollow;

        void Awake()
        {
            GameInput.Actions.Player.Rotate.started += OnRotateStarted;
            GameInput.Actions.Player.Rotate.canceled += OnRotateCanceled;

            _mOrbitalFollow = m_FreeLookVCamera.GetComponent<CinemachineOrbitalFollow>();
            m_FreeLookVCamera.Follow = null;
        }

        void OnDestroy()
        {
            GameInput.Actions.Player.Rotate.started -= OnRotateStarted;
            GameInput.Actions.Player.Rotate.canceled -= OnRotateCanceled;

            StopAllCoroutines();
        }

        internal void SetTransform(Transform newTransform)
        {
            _mFollowTransform = newTransform;
            SetupProtagonistVirtualCamera();
        }

        void OnRotateStarted(InputAction.CallbackContext _)
        {
            _mIsRotatePressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DisableMouseControlForFrame());
        }

        void OnRotateCanceled(InputAction.CallbackContext _)
        {
            _mIsRotatePressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        IEnumerator DisableMouseControlForFrame()
        {
            _mCameraMovementLock = true;
            yield return new WaitForEndOfFrame();
            _mCameraMovementLock = false;
        }

        void Update()
        {
            if (GameInput.Actions.Player.Look.activeControl == null)
            {
                return;
            }

            var device = GameInput.Actions.Player.Look.activeControl.device;
            switch (device)
            {
                case Mouse:
                    HandleRotateMouse();
                    break;
                case Touchscreen:
                    HandleRotateTouchscreen();
                    break;
                case Gamepad:
                    HandleRotateGamepad();
                    break;
            }
        }

        void HandleRotateMouse()
        {
            if (_mCameraMovementLock || !_mIsRotatePressed)
            {
                return;
            }

            var cameraMovement = GameInput.Actions.Player.Look.ReadValue<Vector2>();
            var deviceScaling = KMouseLookMultiplier * Time.deltaTime;
            _mOrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            _mOrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * KVerticalScaling;
        }

        void HandleRotateTouchscreen()
        {
            var cameraMovement = GameInput.Actions.Player.Look.ReadValue<Vector2>();
            var deviceScaling = KMouseLookMultiplier * Time.deltaTime;
            _mOrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            _mOrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * KVerticalScaling;
        }

        void HandleRotateGamepad()
        {
            var cameraMovement = GameInput.Actions.Player.Look.ReadValue<Vector2>();
            var deviceScaling = KGamepadLookMultiplier * Time.deltaTime;
            _mOrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            _mOrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * KVerticalScaling;
        }

        void SetupProtagonistVirtualCamera()
        {
            if (_mFollowTransform == null)
            {
                return;
            }

            m_FreeLookVCamera.Follow = _mFollowTransform;
            CinemachineCore.ResetCameraState(); // snap to new position
        }
    }
}
