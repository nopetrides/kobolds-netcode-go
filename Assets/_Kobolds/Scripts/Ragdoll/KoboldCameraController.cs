using Kobold.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kobolds
{
	/// <summary>
	///     Controls looking around
	/// </summary>
	public class KoboldCameraController : MonoBehaviour
	{
		private const float Threshold = 0.01f;
		[SerializeField] private PlayerInput PlayerInput;
		[SerializeField] private KoboldInputs Inputs;

		[Header("Cinemachine")]
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 70.0f;

		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -30.0f;

		[Tooltip("Additional degrees to override the camera. Useful for fine tuning camera position when locked")]
		[SerializeField] private float CameraAngleOverride;

		[Tooltip("For locking the camera position on all axis")]
		[SerializeField] private bool LockCameraPosition;
		
		private Camera _mainCamera;
		private Transform _cinemachineCameraTarget;
		
		// cinemachine
		private float _cinemachineTargetPitch;
		private float _cinemachineTargetYaw;
		
		/// <summary>
		/// Sets the camera this controller should manage.
		/// </summary>
		public void SetCamera(Camera cam, Transform target)
		{
			_mainCamera = cam;
			Debug.Log($"[{name}] Camera assigned: {cam?.name ?? "null"}");
			_cinemachineCameraTarget = target;
			_cinemachineTargetYaw = _cinemachineCameraTarget.rotation.eulerAngles.y;
			enabled = true;
		}

		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return PlayerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
			}
		}
		

		private void LateUpdate()
		{
			if (!_mainCamera)
			{
				enabled = false;
				return;
			}
			
			CameraRotation();
		}

		private void CameraRotation()
		{
			if (KoboldInputSystemManager.Instance == null ||
				KoboldInputSystemManager.Instance.IsInUIMode)
			{
				return;
			}
			
			// if there is an input and camera position is not fixed
			if (Inputs.Look.sqrMagnitude >= Threshold && !LockCameraPosition)
			{
				//Don't multiply mouse input by Time.deltaTime;
				var deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetYaw += Inputs.Look.x * deltaTimeMultiplier;
				_cinemachineTargetPitch += Inputs.Look.y * deltaTimeMultiplier;
			}

			// clamp our rotations so our values are limited 360 degrees
			_cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Cinemachine will follow this target
			_cinemachineCameraTarget.rotation = Quaternion.Euler(
				_cinemachineTargetPitch + CameraAngleOverride,
				_cinemachineTargetYaw, 0.0f);
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}
	}
}
