using Kobolds.Net;
using Unity.Cinemachine;
using UnityEngine;

namespace Kobolds.Cam
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
				_thirdPersonCamera.Priority = 10;

			if (_aimingCamera != null)
				_aimingCamera.Priority = 0;
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
				_thirdPersonCamera.UpdateTargetCache();
				// don't want the look at set, we handle it in KoboldCameraController
				//_aimingCamera.LookAt = followTarget;
			}

			// Get input reference for aim detection
			KoboldCameraController cameraController = kobold.CurrentCameraController;

			// Assign camera target to the Kobold's camera controller
			if (cameraController != null && _mainCamera != null) 
				cameraController.SetCamera(_mainCamera, kobold.GetCameraFollowTarget());
			else
				Debug.LogError("No camera controller or main camera!");

			if (_thirdPersonCamera.Follow == _placeholderFollowTarget ||
				_aimingCamera.Follow == _placeholderFollowTarget)
			{
				Debug.LogError("Camera follow targets were not reassigned!");
			}

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
			var isAiming = _koboldInputs.Aim;

			if (_thirdPersonCamera != null)
				_thirdPersonCamera.Priority = isAiming ? 0 : 10;

			if (_aimingCamera != null)
				_aimingCamera.Priority = isAiming ? 10 : 0;
		}

		/// <summary>
		///     Switches to a specific camera.
		/// </summary>
		public void SetCameraMode(CameraMode mode)
		{
			switch (mode)
			{
				case CameraMode.ThirdPerson:
					if (_thirdPersonCamera != null) _thirdPersonCamera.Priority = 10;
					if (_aimingCamera != null) _aimingCamera.Priority = 0;
					break;

				case CameraMode.Aiming:
					if (_thirdPersonCamera != null) _thirdPersonCamera.Priority = 0;
					if (_aimingCamera != null) _aimingCamera.Priority = 10;
					break;
			}
		}
	}

	public enum CameraMode
	{
		ThirdPerson,
		Aiming
	}
}
