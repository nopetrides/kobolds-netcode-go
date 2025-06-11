using System;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

namespace Kobolds
{
	public class KoboldGrabber : MonoBehaviour
	{
		[SerializeField] private RagdollAnimator2 characterAnimator;
		public RagdollHandler Handler => characterAnimator?.Handler;
		
		[SerializeField] private KoboldInputs Inputs;
		[SerializeField] private KoboldStateManager StateManager;
		[SerializeField] private KoboldLatcher Latcher;

		[Header("Grip Anim Params")]
		[SerializeField] private Animator Animator;

		[SerializeField] private string GripLAnimParam = "Grip_L";
		[SerializeField] private string GripRAnimParam = "Grip_R";
		[SerializeField] private string GripJawAnimParam = "Grip_Jaw";

		[Header("Grip Points")]

		[SerializeField] private GripMagnetPoint RightGrip;
		[SerializeField] private GripMagnetPoint LeftGrip;
		[SerializeField] private GripMagnetPoint JawGrip;

		private bool _jawModeActive;

		private void Update()
		{
			if (!StateManager.CanGrip) return;

			GripRightCheck(Inputs.GripR);

			GripLeftCheck(Inputs.GripL);

			GripJawCheck(Inputs.GripJaw);

			if (!_jawModeActive || JawGrip.HasTargetAttached) return;

			if (JawGrip.TryAttachNearby()) Animator.SetBool(GripJawAnimParam, true);
		}

		private void GripRightCheck(bool value)
		{
			if (!value)
			{
				return;
			}
			
			Debug.Log("Grip Right");
			if (RightGrip.HasTargetAttached)
			{
				RightGrip.ReleaseGrip();
				Animator.SetBool(GripRAnimParam, false);
			}
			else if (RightGrip.TryAttachNearby())
			{
				Animator.SetBool(GripRAnimParam, true);
			}

			Inputs.GripR = false;
		}

		private void GripLeftCheck(bool value)
		{
			if (!value)
				return;

			Debug.Log("Grip Left");
			if (LeftGrip.HasTargetAttached)
			{
				LeftGrip.ReleaseGrip();
				Animator.SetBool(GripLAnimParam, false);
			}
			else if (LeftGrip.TryAttachNearby())
			{
				Animator.SetBool(GripLAnimParam, true);
			}
			
			Inputs.GripL = false;
		}

		private void GripJawCheck(bool value)
		{
			if (!value) 
				return;
			
			Debug.Log("Grip Jaw");
			Latcher.ToggleJawGrip();
			Animator.SetBool(GripJawAnimParam, _jawModeActive);

			if (!_jawModeActive)
				if (JawGrip.HasTargetAttached)
					JawGrip.ReleaseGrip();
			
			Inputs.GripJaw = false;
		}
	}
}
