﻿using Kobold.Net;
using Unity.Cinemachine;
using UnityEngine;

namespace Kobold.Cam
{
	/// <summary>
	///     Manages camera assignment and switching for local Kobold players.
	///     Place this on a persistent GameObject in the scene with your Cinemachine cameras.
	/// </summary>
	public class KoboldCameraManager : MonoBehaviour
	{
		[Header("Camera Configuration")]
		[SerializeField] private Camera _mainCamera;

		[SerializeField] private CinemachineCamera _thirdPersonCamera;
		[SerializeField] private CinemachineCamera _aimingCamera;
		[SerializeField] private CinemachineCamera _ragdollCamera;
		[SerializeField] private Transform _placeholderFollowTarget;

		private KoboldNetworkController _currentLocalKobold;
		private KoboldInputs _koboldInputs;

		/// <summary>
		///     Gets the singleton instance of the camera manager.
		/// </summary>
		public static KoboldCameraManager Instance { get; private set; }

		public Camera Cam => _mainCamera;

		private void Awake()
		{
			// Simple singleton pattern
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;

			ValidateCameras();
		}

		private void Update()
		{
			if (_currentLocalKobold == null || _koboldInputs == null)
				return;

			UpdateCameraState();
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		private void ValidateCameras()
		{
			if (_thirdPersonCamera == null)
				Debug.LogError($"[{name}] Third Person Camera is not assigned!");

			if (_aimingCamera == null)
				Debug.LogError($"[{name}] Aiming Camera is not assigned!");

			// Ensure cameras start in the correct state
			if (_thirdPersonCamera != null)
			{
				_thirdPersonCamera.enabled = true;
				_thirdPersonCamera.Priority = 10;
			}

			if (_aimingCamera != null)
			{
				_aimingCamera.Priority = 0;
				_aimingCamera.enabled = false;
			}
		}

		/// <summary>
		///     Assigns cameras to the local player's Kobold.
		///     Called by KoboldNetworkController when a local player spawns.
		/// </summary>
		public void AssignToLocalPlayer(KoboldNetworkController kobold)
		{
			if (kobold == null || !kobold.IsOwner)
			{
				Debug.LogWarning("Attempted to assign cameras to non-local player!");
				return;
			}

			_currentLocalKobold = kobold;
			_koboldInputs = KoboldInputSystemManager.Instance.Inputs;

			// Get the follow target from the Kobold (could be the main transform or a specific bone)
			var followTarget = kobold.GetCameraFollowTarget();

			if (followTarget == null)
			{
				Debug.LogError("Kobold has no camera follow target!");
				return;
			}

			// Set up camera follows
			if (_thirdPersonCamera != null)
			{
				_thirdPersonCamera.Follow = followTarget;
				_thirdPersonCamera.UpdateTargetCache();
				// don't want the look at set, we handle it in KoboldCameraController
				//_thirdPersonCamera.LookAt = followTarget;
			}

			if (_aimingCamera != null)
			{
				_aimingCamera.Follow = followTarget;
				_aimingCamera.UpdateTargetCache();
				// don't want the look at set, we handle it in KoboldCameraController
				//_aimingCamera.LookAt = followTarget;
			}

			if (_ragdollCamera != null)
			{
				_ragdollCamera.Follow = kobold.GetRagdollFollowTarget();
				_ragdollCamera.UpdateTargetCache();
			}

			// Get input reference for aim detection
			var cameraController = kobold.CurrentCameraController;

			// Assign camera target to the Kobold's camera controller
			if (cameraController != null && _mainCamera != null)
				cameraController.SetCamera(_mainCamera, kobold.GetCameraFollowTarget());
			else
				Debug.LogError("No camera controller or main camera!");

			if (_thirdPersonCamera.Follow == _placeholderFollowTarget ||
				_aimingCamera.Follow == _placeholderFollowTarget)
				Debug.LogError("Camera follow targets were not reassigned!");

			Debug.Log($"[{name}] Cameras assigned to local Kobold: {kobold.name}");
		}

		/// <summary>
		///     Removes camera assignment when local player despawns.
		/// </summary>
		public void RemoveLocalPlayer()
		{
			_currentLocalKobold = null;
			_koboldInputs = null;

			// Clear camera follows
			if (_thirdPersonCamera != null)
			{
				_thirdPersonCamera.Follow = null;
				_thirdPersonCamera.LookAt = null;
			}

			if (_aimingCamera != null)
			{
				_aimingCamera.Follow = null;
				_aimingCamera.LookAt = null;
			}
		}

		private void UpdateCameraState()
		{
			// Switch cameras based on aim input
			if (_currentLocalKobold.CurrentNetworkState.State >= KoboldState.Climbing)
			{
				SetCameraMode(CameraMode.Ragdoll);
				return;
			}
			var isAiming = _koboldInputs.Aim;

			CameraMode mode = isAiming ? CameraMode.Aiming : CameraMode.ThirdPerson;
			
			SetCameraMode(mode);
		}

		/// <summary>
		///     Switches to a specific camera.
		/// </summary>
		public void SetCameraMode(CameraMode mode)
		{
			_thirdPersonCamera.enabled = mode == CameraMode.ThirdPerson;
			_aimingCamera.enabled = mode == CameraMode.Aiming;
			_ragdollCamera.enabled = mode == CameraMode.Ragdoll;
		}
	}

	public enum CameraMode
	{
		ThirdPerson,
		Aiming,
		Ragdoll
	}
}
