using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimControls : MonoBehaviour
{
	[SerializeField] private CinemachineVirtualCameraBase AimCamera;
	[SerializeField] private bool Aiming;
	
	public void OnAim(InputValue value)
	{ 
		AimInput(value.isPressed);
	}
	
	private void AimInput(bool aiming)
	{
		Aiming = aiming;
	}

	private void Update()
	{
		if (AimCamera.enabled != Aiming)
		{
			AimCamera.enabled = Aiming;
		}
	}
}
