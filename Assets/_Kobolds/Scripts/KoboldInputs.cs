using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kobolds
{
	public class KoboldInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 Move;
		public Vector2 Look;
		public bool Jump;
		public bool Sprint;
		public bool Walk;
		public bool Aim;
		public bool Fire;
		public bool GripR;
		public bool GripL;
		public bool GripJaw;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		public void OnWalk(InputValue value)
		{
			WalkInput(value.isPressed);
		}
		
		public void OnAim(InputValue value)
        { 
            AimInput(value.isPressed);
        }
		
		public void OnFire(InputValue value)
		{ 
			FireInput(value.isPressed);
		}
		
		public void OnGripRight(InputValue value)
		{
			GripRInput(value.isPressed);
		}
		
		public void OnGripLeft(InputValue value)
		{
			GripLInput(value.isPressed);
		}

		public void OnGripJaw(InputValue value)
		{
			GripJawInput(value.isPressed);
		}
#endif


		private void MoveInput(Vector2 newMoveDirection)
		{
			Move = newMoveDirection;
		} 

		private void LookInput(Vector2 newLookDirection)
		{
			Look = newLookDirection;
		}

		private void JumpInput(bool newJumpState)
		{
			Jump = newJumpState;
		}

		private void SprintInput(bool newSprintState)
		{
			Sprint = newSprintState;
		}
		
		private void WalkInput(bool newWalkState)
		{
			Walk = newWalkState;
		}
		
		private void AimInput(bool newAimState)
		{
			Aim = newAimState;
		}
		
		private void FireInput(bool newFireState)
		{
			Fire = newFireState;
		}
		
		private void GripRInput(bool newSprintState)
		{
			GripR = newSprintState;
		}
		
		private void GripLInput(bool newAimState)
		{
			GripL = newAimState;
		}

		private void GripJawInput(bool newFireState)
		{
			GripJaw = newFireState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}