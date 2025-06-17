using System;
using FIMSpace.FProceduralAnimation;
using Kobold.GameManagement;
using UnityEngine;

namespace Kobold
{
	public class KoboldFlopControls : MonoBehaviour
	{
		[SerializeField] private Rigidbody Rigidbody;
		[SerializeField] private Animator Animator;
		[SerializeField] private RagdollAnimator2 TargetRagdoll;
		[SerializeField] private KoboldStateManager StateManager;
		[SerializeField] private KoboldGameplayEvents GameplayEvents;
		private KoboldInputs Inputs { get; set; }

		private void Start()
		{
			Inputs = KoboldInputSystemManager.Instance.Inputs;
		}

		private void Update()
		{
			if (Inputs.Flop)
			{
				Inputs.Flop = false;
				if (StateManager.IsInState(KoboldState.Active) && KoboldInputSystemManager.Instance.IsInGameplayMode)
				{
					Inputs.Flop = false;
					Flop();
				}
			}
		}

		private void Flop()
		{
			Rigidbody.isKinematic = true;
			Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			Animator.enabled = false;
			StateManager.SetState(KoboldState.Flopping);
			
			// Disable stand mode
			TargetRagdoll.User_SwitchFallState();
			TargetRagdoll.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
			// Fully limp, maybe?
			//animator.User_FadeMusclesPowerMultiplicator(0.05f, 0.05f);
			
			GameplayEvents.NotifyFlop();
		}

		public void OnAutoGetUp()
		{
			if (!isActiveAndEnabled) return;
			if (!StateManager.IsInState(KoboldState.Flopping)) return;
			
			Rigidbody.isKinematic = false;
			Animator.enabled = true;
			StateManager.SetState(KoboldState.Active);
			
			GameplayEvents.NotifyGetUp();
		}
	}
}