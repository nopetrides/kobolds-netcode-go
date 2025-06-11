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
		[SerializeField] private GameObject Cam;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		[SerializeField] private GameObject CinemachineCameraTarget;

		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 70.0f;

		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -30.0f;

		[Tooltip("Additional degrees to override the camera. Useful for fine tuning camera position when locked")]
		[SerializeField] private float CameraAngleOverride;

		[Tooltip("For locking the camera position on all axis")]
		[SerializeField] private bool LockCameraPosition;

		// cinemachine
		private float _cinemachineTargetPitch;
		private float _cinemachineTargetYaw;

		private void Start()
		{
			_cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
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
			CameraRotation();
		}

		private void CameraRotation()
		{
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
			CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
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
