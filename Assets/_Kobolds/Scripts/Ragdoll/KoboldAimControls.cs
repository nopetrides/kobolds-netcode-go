using Kobold.Input;
using Kobolds.Cam;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kobolds
{
	public class AimControls : MonoBehaviour
	{
		[SerializeField] private bool Aiming;

		private void Update()
		{
			if (KoboldInputSystemManager.Instance == null ||
				KoboldInputSystemManager.Instance.IsInUIMode)
			{
				return;
			}

			var cameraManager = KoboldCameraManager.Instance;
			if (cameraManager != null && Aiming)
				cameraManager.SetCameraMode(CameraMode.Aiming);
			else if (cameraManager != null && !Aiming) 
				cameraManager.SetCameraMode(CameraMode.ThirdPerson);
		}

		public void OnAim(InputValue value)
		{
			AimInput(value.isPressed);
		}

		private void AimInput(bool aiming)
		{
			Aiming = aiming;
		}
	}
}
