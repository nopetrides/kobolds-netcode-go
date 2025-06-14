using FIMSpace.FProceduralAnimation;
using Unity.Netcode;
using UnityEngine;

namespace Kobolds.Net
{
	/// <summary>
	///     Extension of KoboldNetworkController with state-specific RPCs.
	///     This partial class adds RPCs for immediate state feedback.
	/// </summary>
	public partial class KoboldNetworkController
	{
		private static readonly int GripL = Animator.StringToHash("Grip_L");
		private static readonly int GripR = Animator.StringToHash("Grip_R");
		private static readonly int GripJaw = Animator.StringToHash("Grip_Jaw");

		[Header("Ragdoll Components")]
		[SerializeField] private RagdollAnimator2 _ragdollAnimator;

		[SerializeField] private Animator _animationController;

		[Header("Interaction Components")]
		[SerializeField] private KoboldGrabber _koboldGrabber;

		[SerializeField] private KoboldLatcher _koboldLatcher;

		/// <summary>
		///     Handles remote ragdoll state changes based on KoboldState.
		/// </summary>
		private void ApplyRemoteRagdollState(KoboldState state)
		{
			if (_ragdollAnimator == null) return;

			switch (state)
			{
				case KoboldState.Unburying:
					// Full ragdoll, limp state
					_ragdollAnimator.User_SwitchFallState();
					_ragdollAnimator.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
					_ragdollAnimator.User_FadeMusclesPowerMultiplicator(0.05f, 0.05f);
					if (_animationController != null)
						_animationController.enabled = false;
					break;

				case KoboldState.Active:
					// Standing mode
					_ragdollAnimator.User_TransitionToStandingMode(0.2f, 0f);
					_ragdollAnimator.User_FadeMusclesPowerMultiplicator(1f, 0.2f);
					if (_animationController != null)
						_animationController.enabled = true;
					break;

				case KoboldState.Climbing:
					// Partial ragdoll for climbing
					_ragdollAnimator.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
					_ragdollAnimator.User_FadeMusclesPowerMultiplicator(0.05f, 0.05f);
					if (_animationController != null)
						_animationController.enabled = false;
					break;
			}
		}

		/// <summary>
		///     RPC to notify all clients of state changes immediately.
		///     This provides faster state propagation than NetworkVariable sync.
		/// </summary>
		[Rpc(SendTo.NotOwner)]
		private void NotifyStateChangeRpc(KoboldState newState)
		{
			// The state is already being set by NetworkVariable callback
			// This RPC just ensures immediate propagation
			Debug.Log($"[{name}] State change RPC received: {newState}");
		}

		/// <summary>
		///     RPC to notify when a grab occurs.
		/// </summary>
		[Rpc(SendTo.NotOwner)]
		public void OnGrabObjectRpc(NetworkObjectReference grabbedObject, GripType gripType)
		{
			if (grabbedObject.TryGet(out var obj))
			{
				Debug.Log($"[{name}] Remote player grabbed {obj.name} with {gripType}");

				// Update animator for grip animations
				if (_animationController != null)
					switch (gripType)
					{
						case GripType.LeftHand:
							_animationController.SetBool(GripL, true);
							break;
						case GripType.RightHand:
							_animationController.SetBool(GripR, true);
							break;
						case GripType.Jaw:
							_animationController.SetBool(GripJaw, true);
							break;
					}
			}
		}

		/// <summary>
		///     RPC to notify when a release occurs.
		/// </summary>
		[Rpc(SendTo.NotOwner)]
		public void OnReleaseObjectRpc(GripType gripType)
		{
			Debug.Log($"[{name}] Remote player released {gripType} grip");

			// Update animator for grip animations
			if (_animationController != null)
				switch (gripType)
				{
					case GripType.LeftHand:
						_animationController.SetBool(GripL, false);
						break;
					case GripType.RightHand:
						_animationController.SetBool(GripR, false);
						break;
					case GripType.Jaw:
						_animationController.SetBool(GripJaw, false);
						break;
				}
		}

		/// <summary>
		///     RPC to notify when latching occurs.
		/// </summary>
		[Rpc(SendTo.NotOwner)]
		public void OnLatchRpc(NetworkObjectReference latchTarget, Vector3 worldAttachPos)
		{
			if (_stateManager != null) _stateManager.SetState(KoboldState.Climbing);

			// Apply climbing ragdoll state
			ApplyRemoteRagdollState(KoboldState.Climbing);

			if (latchTarget.TryGet(out var target))
				Debug.Log($"[{name}] Remote player latched to {target.name} at {worldAttachPos}");
		}

		/// <summary>
		///     RPC to notify when detaching occurs.
		/// </summary>
		[Rpc(SendTo.NotOwner)]
		public void OnDetachRpc()
		{
			if (_stateManager != null) _stateManager.SetState(KoboldState.Active);

			// Return to active ragdoll state
			ApplyRemoteRagdollState(KoboldState.Active);

			Debug.Log($"[{name}] Remote player detached from latch");
		}
	}
}
