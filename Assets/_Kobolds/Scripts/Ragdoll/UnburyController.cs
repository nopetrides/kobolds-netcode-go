using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Kobolds
{
	/// <summary>
	/// Controls the unburying process, handles struggle timing, and provides the percentage completion of the struggle.
	/// </summary>
	[RequireComponent(typeof(RagdollAnimator2))]
	public class UnburyController : MonoBehaviour
	{
		[SerializeField] private KoboldStateManager StateManager;
		
		/// <summary>
		/// Reference to the RagdollAnimator2 component used for handling procedural
		/// animations and ragdoll physics associated with the character.
		/// </summary>
		[FormerlySerializedAs("ragdoll")]
		[Header("References")]
		[SerializeField] private RagdollAnimator2 Ragdoll;

		/// <summary>
		/// Represents the primary Rigidbody component associated with this object.
		/// </summary>
		/// <remarks>
		/// Used for managing the physics and movement of the character or object.
		/// </remarks>
		[FormerlySerializedAs("mainRigidbody")] [SerializeField] private Rigidbody MainRigidbody;

		/// <summary>
		/// Manages the movement system of a character or entity.
		/// </summary>
		[FormerlySerializedAs("movementController")] [SerializeField] private RagdollMover MovementController;

		/// <summary>
		/// Represents input data provided to a system or method.
		/// </summary>
		[FormerlySerializedAs("playerInput")] [SerializeField] private PlayerInput Input;

		[SerializeField]
		private List<string> AllowedStruggleActions = new List<string> { "Move", "Jump", "Sprint", "Aim", "Fire" };

		private List<InputAction> _subscribedActions = new List<InputAction>();

		
		/// <summary>
		/// Duration of the struggle phase before completion, measured in seconds.
		/// </summary>
		[FormerlySerializedAs("struggleDuration")]
		[Header("Struggle Settings")]
		[SerializeField] private float StruggleDuration = 3f;

		/// <summary>
		/// Defines the animation curve controlling the changes in muscle power over time
		/// during the struggle phase of the unburial process.
		/// </summary>
		[FormerlySerializedAs("musclePowerCurve")] [SerializeField] private AnimationCurve MusclePowerCurve = AnimationCurve.Linear(0, 0, 1, 1);

		/// <summary>
		/// Represents the initial muscle power applied to the ragdoll for overriding muscle strength.
		/// Determines the starting strength level of animated muscles in the ragdoll system.
		/// Configured in the inspector to influence how rigid or limp the ragdoll begins.
		/// Used to set the override muscle power during the ragdoll's initialization phase.
		/// Impacts the responsiveness of the ragdoll's limbs when switching to "Falling" mode.
		/// </summary>
		[FormerlySerializedAs("startingMusclePower")] [SerializeField] private float StartingMusclePower;

		/// <summary>
		/// Represents the final muscle power value applied during the struggle or recovery process.
		/// </summary>
		[FormerlySerializedAs("finalMusclePower")] [SerializeField] private float FinalMusclePower = 1f;

		/// <summary>
		/// Specifies the minimum force applied to a random limb during the struggle process.
		/// </summary>
		[FormerlySerializedAs("minFlailForce")] [SerializeField] private float MinFlailForce = 0.5f;

		/// <summary>
		/// Specifies the maximum force applied during flailing movements in the UnburyController.
		/// </summary>
		[FormerlySerializedAs("maxFlailForce")] [SerializeField] private float MaxFlailForce = 2f;

		/// <summary>
		/// Mask used to identify ground layers for collision checks.
		/// </summary>
		[FormerlySerializedAs("groundMask")]
		[Header("Get Up Settings")]
		[SerializeField] private LayerMask GroundMask = ~0;

		/// <summary>
		/// Duration (in seconds) of the transition for the ragdoll to stand up.
		/// </summary>
		[FormerlySerializedAs("transitionDuration")] [SerializeField] private float TransitionDuration = 0.85f;

		/// <summary>
		/// Name of the animation clip used when the ragdoll transitions from a facedown position to a standing posture.
		/// </summary>
		[FormerlySerializedAs("getUpFaceAnim")] [SerializeField] private string GetUpFaceAnim = "Get Up Face";

		/// <summary>
		/// Defines the animation name for the "Get Up Back" action.
		/// Used when the character transitions from a lying on the back position to standing.
		/// Assigned in the Unity Inspector and passed to the ragdoll system during get-up logic.
		/// </summary>
		[FormerlySerializedAs("getUpBackAnim")] [SerializeField] private string GetUpBackAnim = "Get Up Back";

		/// Represents a private InputAction associated with the UnburyController.
		/// Likely used to detect or handle any input interaction within the class.
		private InputAction _anyAction;

		/// <summary>
		/// Indicates whether the struggle phase is complete.
		/// </summary>
		private bool _isComplete;

		/// Tracks the elapsed time of a struggle process.
		/// Used to compute struggle progress and trigger related functionalities when the timer reaches the defined duration.
		/// Value increases based on struggle input actions and is clamped to the total struggle duration.
		private float _struggleTimer;
		
		/// Represents the progress of the struggle as a percentage, ranging from 0 to 1.
		/// A value of 0 indicates no progress, while 1 indicates the struggle is complete.
		public float StrugglePercentComplete => Mathf.Clamp01(_struggleTimer / StruggleDuration);
		
		private RagdollAnimatorFeatureHelper _autoGetUp;

		/// <summary>
		/// Initializes component references and configures ragdoll settings.
		/// </summary>
		private void Start()
		{
			if (!Ragdoll) Ragdoll = GetComponent<RagdollAnimator2>();
			if (!MainRigidbody) MainRigidbody = GetComponent<Rigidbody>();
			if (!Input) Input = GetComponent<PlayerInput>();
			
			if (StateManager != null)
			{
				StateManager.SetState(KoboldState.Unburying);
			}

			// Set initial limp state
			Ragdoll.User_UpdateRigidbodyParametersForAllBones();
			Ragdoll.User_SwitchFallState();
			Ragdoll.Handler.User_OverrideMusclesPower = StartingMusclePower;
			Ragdoll.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
			_autoGetUp = Ragdoll.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
			if (_autoGetUp != null)
				_autoGetUp.Enabled = false;
			else
				Debug.LogError("RagdollAnimator2 component does not have RAF_AutoGetUp feature enabled");
			Ragdoll.User_UpdateRigidbodyParametersForAllBones();
			
			if (MovementController) MovementController.enabled = false;

			// Bind all inputs to struggle
			foreach (var map in Input.actions.actionMaps)
			{
				foreach (var action in map.actions)
				{
					if (!AllowedStruggleActions.Contains(action.name)) continue;

					InputAction a = action; // Capture local copy for closure
					a.performed += ctx =>
					{
						if (a.name == "Move")
						{
							Vector2 moveVal = ctx.ReadValue<Vector2>();
							if (moveVal == Vector2.zero) return;
						}

						OnStruggleInput(ctx);
					};

					_subscribedActions.Add(a);
				}
			}
		}

		/// <summary>
		/// Invoked when the object is destroyed.
		/// Removes all input listeners to ensure proper cleanup and prevent memory leaks.
		/// </summary>
		private void OnDestroy()
		{
			// Clean up input listeners
			foreach (var action in _subscribedActions)
			{
				action.performed -= OnStruggleInput;
			}

			_subscribedActions.Clear();
		}

		/// <summary>
		/// Handles player input for struggle mechanics during the unburying process.
		/// <param name="context">The input context containing data about the current input event.</param>
		/// </summary>
		private void OnStruggleInput(InputAction.CallbackContext context)
		{
			if (_isComplete) return;

			// Advance progress
			_struggleTimer += Time.deltaTime * 3f; // Input triggers can happen fast
			_struggleTimer = Mathf.Min(_struggleTimer, StruggleDuration);

			// Evaluate curve
			var progress = StrugglePercentComplete;
			var muscle = Mathf.Lerp(StartingMusclePower, FinalMusclePower, MusclePowerCurve.Evaluate(progress));
			Ragdoll.Handler.User_OverrideMusclesPower = muscle;

			// Add twitchy impulse to a random limb
			var coreChain = Ragdoll.Handler.GetChain(ERagdollChainType.Core);
			if (coreChain.BoneSetups.Count > 0)
			{
				var randomBone = coreChain.BoneSetups[Random.Range(0, coreChain.BoneSetups.Count)];
				var force = Random.onUnitSphere * Random.Range(MinFlailForce, MaxFlailForce);
				randomBone.GameRigidbody.AddForce(force, ForceMode.Impulse);
			}

			// If done, transition
			if (progress >= 1f)
			{
				TriggerGetUp();
				_isComplete = true;
			}
			
			Ragdoll.User_UpdateRigidbodyParametersForAllBones();
		}

		/// <summary>
		/// Initiates the process for the character to transition from a ragdoll state to a standing state.
		/// </summary>
		private void TriggerGetUp()
		{
			// Check ground below
			var hips = Ragdoll.Handler.GetAnchorBoneController;
			var hit = Ragdoll.Handler.ProbeGroundBelowHips(
				GroundMask, hips.MainBoneCollider.bounds.size.magnitude + 0.01f);
			if (!hit.transform)
			{
				Debug.Log("Unbury failed: no ground detected under hips. Forcing fallback get-up.");
				
				// Use current transform position to avoid hanging
				hit.point = transform.position;
			}

			transform.position = hit.point;
			transform.rotation = Ragdoll.User_GetMappedRotationHipsToLegsMiddle();

			if (MainRigidbody)
			{
				MainRigidbody.position = transform.position;
				MainRigidbody.rotation = transform.rotation;
			}
			
			if (StateManager != null)
			{
				StateManager.OnUnburyComplete();
			}

			if (MovementController) MovementController.enabled = true;

			var type = Ragdoll.User_CanGetUpByRotation();
			var anim = type == ERagdollGetUpType.FromFacedown ? GetUpFaceAnim : GetUpBackAnim;

			Ragdoll.Handler.Mecanim.CrossFadeInFixedTime(anim, 0.2f);
			Ragdoll.Handler.User_TransitionToStandingMode(TransitionDuration, 0.6f, 0.1f, 0.125f);
			Ragdoll.Handler.User_FadeMusclesPowerMultiplicator(FinalMusclePower, TransitionDuration);
			Ragdoll.Handler.User_SwitchFallState(true);
			
			if (_autoGetUp != null)
				_autoGetUp.Enabled = true;
			else
				Debug.LogError("RagdollAnimator2 component does not have RAF_AutoGetUp feature enabled");
		}
	}
}
