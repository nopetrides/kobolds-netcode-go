using Kobold.Cam;
using UnityEngine;

namespace Kobold
{
	public class AimControls : MonoBehaviour
	{
		[SerializeField] private bool Aiming;

		private KoboldInputs Inputs { get; set; }

		private void Start()
		{
			Inputs = KoboldInputSystemManager.Instance.Inputs;
		}

		private void Update()
		{
			if (KoboldInputSystemManager.Instance == null ||
				KoboldInputSystemManager.Instance.IsInUIMode)
				return;

			Aiming = Inputs.Aim;

			var cameraManager = KoboldCameraManager.Instance;
			if (cameraManager != null && Aiming)
				cameraManager.SetCameraMode(CameraMode.Aiming);
			else if (cameraManager != null && !Aiming)
				cameraManager.SetCameraMode(CameraMode.ThirdPerson);
		}
	}
}
