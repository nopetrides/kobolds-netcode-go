using System;
using FIMSpace.FProceduralAnimation;
using Kobold.Bosses;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;

namespace Kobold
{
	/// <summary>
	/// Represents the current state of the kobold's latch system.
	/// </summary>
	public enum LatchState
	{
		/// <summary>
		/// Mouth closed, no interaction.
		/// </summary>
		None,
		
		/// <summary>
		/// Mouth open, searching for something to bite.
		/// </summary>
		Open,
		
		/// <summary>
		/// Actively biting a target.
		/// </summary>
		Gnawing
	}

	public class KoboldLatcher : MonoBehaviour
	{
		public event Action<bool> OnLatchableTargetChanged;
		public event Action<LatchState> OnLatchStateChanged;

		[Header("References")]
		[SerializeField] private KoboldStateManager StateManager;

		[SerializeField] private KoboldGameplayEvents GameplayEvents;
		[SerializeField] private GripMagnetPoint JawMagnet;
		[SerializeField] private RagdollAnimator2 RagdollAnimator;

		[FormerlySerializedAs("Animator")] [SerializeField]
		private Animator AnimationController;

		[SerializeField] private string GripJawAnimParam = "Grip_Jaw";

		[Header("Latch Settings")]
		[SerializeField] private LayerMask LatchableLayers;

		[SerializeField] private float LatchRadius = 0.2f;
		private readonly LatchInfo _latch = new();
		private readonly Collider[] _overlapBuffer = new Collider[16];
		private Collider _currentTarget;

		// Replace bool with LatchState enum
		private LatchState _currentLatchState = LatchState.None;
		private bool _isLatchableTargetInRange;

		private PlayerInput Input { get; set; }
		private Audio.KoboldLatchAudioManager _audioManager;

		public GripMagnetPoint JawLatchMagnet => JawMagnet;
		public bool IsLatched => _latch.IsLatched;
		public LatchState CurrentLatchState => _currentLatchState;

		private void Update()
		{
			// Only try to latch if we're in Open state and not already latched
			if (_currentLatchState != LatchState.Open || JawMagnet.HasTargetAttached)
				return;

			TryLatchToSurface();
		}

		private void FixedUpdate()
		{
			_latch.UpdateLatch();
		}

		private void OnDrawGizmosSelected()
		{
			if (!_latch.IsLatched) return;

			Gizmos.color = Color.green;
			Gizmos.DrawSphere(_latch.WorldAttachPosition, 0.05f);

			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(JawMagnet.transform.position, LatchRadius);
		}

		/// <summary>
		/// Toggles the jaw grip state and updates latch state accordingly.
		/// </summary>
		public void ToggleJawGrip()
		{
			// Toggle between None and Open states
			var newState = _currentLatchState == LatchState.None ? LatchState.Open : LatchState.None;
			SetLatchState(newState);

			// If we're closing our mouth and we're latched, detach
			if (newState == LatchState.None && _latch.IsLatched)
			{
				_latch.Detach(RagdollAnimator, AnimationController, StateManager, GameplayEvents, _audioManager);
				_currentTarget = null;
			}
		}

		/// <summary>
		/// Sets the latch state and fires events.
		/// </summary>
		private void SetLatchState(LatchState newState)
		{
			if (_currentLatchState == newState) return;
			
			var previousState = _currentLatchState;
			_currentLatchState = newState;
			
			// Update animator
			AnimationController?.SetBool(GripJawAnimParam, newState != LatchState.None);
			
			// Fire state change event
			OnLatchStateChanged?.Invoke(newState);
			
			// Notify gameplay events system
			GameplayEvents?.NotifyLatchStateChanged(newState);
			GameplayEvents?.NotifyLatchStateTransitioned(previousState, newState);
			
			// Update network state if we have authority
			var networkController = GetComponentInParent<Kobold.Net.KoboldNetworkController>();
			if (networkController != null && networkController.IsOwner)
			{
				networkController.SetLatchState(newState);
			}
			
			Debug.Log($"[KoboldLatcher] Latch state changed from {previousState} to {newState}");
		}

		private void TryLatchToSurface()
		{
			var handler = RagdollAnimator.Handler;
			var chainBone = handler.User_GetBoneSetupBySourceAnimatorBone(JawMagnet.MagnetPoint.transform);
			var bone = chainBone?.BoneProcessor;

			if (bone == null || bone.rigidbody == null || _currentTarget != null)
				return;

			var origin = bone.rigidbody.position;
			var hits = Physics.OverlapSphereNonAlloc(origin, LatchRadius, _overlapBuffer, LatchableLayers);

			bool foundTarget = false;
			for (var i = 0; i < hits; i++)
			{
				var col = _overlapBuffer[i];

				// Optional: skip self or previous target if needed
				if (col.attachedRigidbody == bone.rigidbody || col == _currentTarget) continue;

				Vector3 attachPos;

				var dir = (col.bounds.center - origin).normalized;
				if (col.Raycast(new Ray(origin, dir), out var rayHit, LatchRadius * 2f))
				{
					attachPos = rayHit.point;
					foundTarget = true;
					// Try Latch
					if (_currentLatchState == LatchState.Open)
					{
						_latch.Latch(bone, col, attachPos, RagdollAnimator, AnimationController, StateManager, GameplayEvents);
						SetLatchState(LatchState.Gnawing);
					}
					break;
				}
				else if (col is BoxCollider or SphereCollider or CapsuleCollider ||
						(col is MeshCollider mesh && mesh.convex))
					attachPos = col.ClosestPoint(origin); // safe
				else
					continue; // skip this collider, no safe point

				_latch.Latch(bone, col, attachPos, RagdollAnimator, AnimationController, StateManager, GameplayEvents);
				SetLatchState(LatchState.Gnawing);

				_currentTarget = col;
				return;
			}
			
			if (foundTarget != _isLatchableTargetInRange)
			{
				_isLatchableTargetInRange = foundTarget;
				OnLatchableTargetChanged?.Invoke(_isLatchableTargetInRange);
			}
		}

		private void OnEnable()
		{
			// Get audio manager reference
			_audioManager = GetComponent<Audio.KoboldLatchAudioManager>();
		}

		[Serializable]
		private class LatchInfo
		{
			private RagdollBoneProcessor _bone;
			private KoboldGameplayEvents _gameplayEvents;
			private Vector3 _localPos;
			private Quaternion _localRot;
			private Rigidbody _rb;
			public Collider Target { get; private set; }

			public Vector3 WorldAttachPosition => Target ? Target.transform.TransformPoint(_localPos) : Vector3.zero;

			public bool IsLatched { get; private set; }

			public void Latch(
				RagdollBoneProcessor bone, Collider target, Vector3 worldPos,
				RagdollAnimator2 animator, Animator animationController, KoboldStateManager stateManager,
				KoboldGameplayEvents events)
			{
				_bone = bone;
				Target = target;
				_rb = bone.rigidbody;

				_localPos = target.transform.InverseTransformPoint(worldPos);
				_localRot = Quaternion.Inverse(target.transform.rotation) * _rb.rotation;

				_rb.isKinematic = true;
				_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
				IsLatched = true;

				//animationController.enabled = false;
				stateManager.SetState(KoboldState.Climbing);

				var autoGetUp = animator.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
				if (autoGetUp != null)
					autoGetUp.Enabled = false;

				animator.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
				animator.User_FadeMusclesPowerMultiplicator(0.05f, 0.05f);

				events.NotifyLatch(target, _localPos, _localRot);

				// Notify damage handler
				var handler = target.GetComponentInParent<LatchDamageHandler>();
				if (handler) handler.OnLatched(_rb.transform);
			}


			public void UpdateLatch()
			{
				if (!IsLatched || Target == null || _rb == null) return;

				// Check if target has been destroyed
				if (Target == null)
				{
					// Target was destroyed, clean up
					IsLatched = false;
					return;
				}

				_rb.position = Target.transform.TransformPoint(_localPos);
				_rb.rotation = Target.transform.rotation * _localRot;
			}

			public void Detach(
				RagdollAnimator2 animator, Animator animationController, KoboldStateManager stateManager,
				KoboldGameplayEvents events, Audio.KoboldLatchAudioManager audioManager = null)
			{
				if (_rb != null)
					_rb.isKinematic = false;
				
				//animator.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Standing;
				animator.User_TransitionToStandingMode(0.2f, 0f);
				animator.User_FadeMusclesPowerMultiplicator(1f, 0.2f);

				// Notify unlatched BEFORE clearing Target
				if (Target)
				{
					var handler = Target.GetComponent<LatchDamageHandler>();
					if (handler != null) handler.OnUnlatched(_rb?.transform);
				}

				_bone = null;
				_rb = null;
				Target = null;
				IsLatched = false;

				//animationController.enabled = true;
				stateManager.SetState(KoboldState.Active);

				var autoGetUp = animator.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
				if (autoGetUp != null)
					autoGetUp.Enabled = true;

				events.NotifyDetach();
				
				// Play latch end sound
				audioManager?.PlayLatchEndSound();
			}
		}
	}
}
