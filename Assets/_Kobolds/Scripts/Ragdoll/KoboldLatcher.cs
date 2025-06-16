using System;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kobold
{
	public class KoboldLatcher : MonoBehaviour
	{
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
		private readonly Collider[] _overlapBuffer = new Collider[6];
		private Collider _currentTarget;

		private bool _isGripToggled;

		private void Update()
		{
			if (!_isGripToggled || JawMagnet.HasTargetAttached)
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

		public void ToggleJawGrip()
		{
			_isGripToggled = !_isGripToggled;
			AnimationController.SetBool(GripJawAnimParam, _isGripToggled);

			if (!_isGripToggled && _latch.IsLatched)
			{
				_latch.Detach(RagdollAnimator, AnimationController, StateManager, GameplayEvents);
				_currentTarget = null;
			}
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

			for (var i = 0; i < hits; i++)
			{
				var col = _overlapBuffer[i];

				// Optional: skip self or previous target if needed
				if (col.attachedRigidbody == bone.rigidbody || col == _currentTarget) continue;

				Vector3 attachPos;

				var dir = (col.bounds.center - origin).normalized;
				if (col.Raycast(new Ray(origin, dir), out var rayHit, LatchRadius * 2f))
					attachPos = rayHit.point;
				else if (col is BoxCollider or SphereCollider or CapsuleCollider ||
						(col is MeshCollider mesh && mesh.convex))
					attachPos = col.ClosestPoint(origin); // safe
				else
					continue; // skip this collider, no safe point

				_latch.Latch(bone, col, attachPos, RagdollAnimator, AnimationController, StateManager, GameplayEvents);

				_currentTarget = col;
				return;
			}
		}


		[Serializable]
		private class LatchInfo
		{
			private KoboldGameplayEvents _gameplayEvents;
			private RagdollBoneProcessor _bone;
			private Vector3 _localPos;
			private Quaternion _localRot;
			private Rigidbody _rb;
			public Collider Target { get; private set; }

			public Vector3 WorldAttachPosition => Target ? Target.transform.TransformPoint(_localPos) : Vector3.zero;

			public bool IsLatched { get; private set; }

			public void Latch(
				RagdollBoneProcessor bone, Collider target, Vector3 worldPos,
				RagdollAnimator2 animator, Animator animationController, KoboldStateManager stateManager, KoboldGameplayEvents events)
			{
				_bone = bone;
				Target = target;
				_rb = bone.rigidbody;

				_localPos = target.transform.InverseTransformPoint(worldPos);
				_localRot = Quaternion.Inverse(target.transform.rotation) * _rb.rotation;

				_rb.isKinematic = true;
				_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
				IsLatched = true;
				animationController.enabled = false;
				stateManager.SetState(KoboldState.Climbing);
				var autoGetUp = animator.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
				if (autoGetUp != null)
					autoGetUp.Enabled = false;

				// Fully limp body and disable stand mode
				animator.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
				animator.User_FadeMusclesPowerMultiplicator(0.05f, 0.05f);
				
				events.NotifyLatch(target, _localPos, _localRot);
			}

			public void UpdateLatch()
			{
				if (!IsLatched || Target == null || _rb == null) return;

				_rb.position = Target.transform.TransformPoint(_localPos);
				_rb.rotation = Target.transform.rotation * _localRot;
			}

			public void Detach(RagdollAnimator2 animator, Animator animationController, KoboldStateManager stateManager, KoboldGameplayEvents events)
			{
				if (_rb != null)
					_rb.isKinematic = false;

				// Re-enable ragdoll standing
				animator.User_TransitionToStandingMode(0.2f, 0f);
				animator.User_FadeMusclesPowerMultiplicator(1f, 0.2f);

				_bone = null;
				_rb = null;
				Target = null;
				IsLatched = false;
				animationController.enabled = true;
				stateManager.SetState(KoboldState.Active);
				var autoGetUp = animator.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
				if (autoGetUp != null)
					autoGetUp.Enabled = true;
				
				events.NotifyDetach();
			}
		}
	}
}
