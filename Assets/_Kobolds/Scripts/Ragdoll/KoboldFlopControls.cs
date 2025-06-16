using System;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

namespace Kobold
{
	public class KoboldFlopControls : MonoBehaviour
	{
		[SerializeField] private RagdollAnimator2 TargetRagdoll;
		private KoboldInputs Inputs { get; set; }
		[SerializeField] private KoboldStateManager StateManager;

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
					TargetRagdoll.User_SwitchFallState(!TargetRagdoll.Handler.IsInStandingMode);
				}
			}
		}
	}
}