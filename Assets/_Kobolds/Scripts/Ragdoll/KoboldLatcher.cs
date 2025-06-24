using System;
using FIMSpace.FProceduralAnimation;
using Kobold.Bosses;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using Unity.Netcode;

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

		[Header("Debug")]
		public bool enableLatchDebugLogging = false;

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
			AnimationController?.SetBool(GripJawAnimParam, newState != LatchState.Open);
			
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
				if (col.attachedRigidbody == bone.rigidbody || col == _currentTarget || col.transform.root == transform.root) continue;

				Vector3 attachPos;

				var dir = (col.bounds.center - origin).normalized;
				if (col.Raycast(new Ray(origin, dir), out var rayHit, LatchRadius * 2f))
				{
					attachPos = rayHit.point;
					foundTarget = true;
					if (enableLatchDebugLogging)
					{
						Debug.Log($"[LATCH-LOCAL] Found collider: {col.name} (ID: {col.GetInstanceID()}) at attachPos: {attachPos}, localPos: {col.transform.InverseTransformPoint(attachPos)}, localRot: {Quaternion.Inverse(col.transform.rotation) * bone.rigidbody.rotation}");
					}
					if (_currentLatchState == LatchState.Open)
					{
						_latch.Latch(bone, col, attachPos, RagdollAnimator, AnimationController, StateManager, GameplayEvents, this);
						SetLatchState(LatchState.Gnawing);
					}
					break;
				}
				else if (col is BoxCollider or SphereCollider or CapsuleCollider ||
						(col is MeshCollider mesh && mesh.convex))
					attachPos = col.ClosestPoint(origin); // safe
				else
					continue; // skip this collider, no safe point

				if (enableLatchDebugLogging)
				{
					Debug.Log($"[LATCH-LOCAL] Found collider (fallback): {col.name} (ID: {col.GetInstanceID()}) at attachPos: {attachPos}, localPos: {col.transform.InverseTransformPoint(attachPos)}, localRot: {Quaternion.Inverse(col.transform.rotation) * bone.rigidbody.rotation}");
				}
				_latch.Latch(bone, col, attachPos, RagdollAnimator, AnimationController, StateManager, GameplayEvents, this);
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

			private Vector3 _worldPos;
			private Quaternion _worldRot;

			public Vector3 WorldAttachPosition => Target ? Target.transform.TransformPoint(_localPos) : Vector3.zero;

			public bool IsLatched { get; private set; }

			public void Latch(
				RagdollBoneProcessor bone, Collider target, Vector3 worldPos,
				RagdollAnimator2 animator, Animator animationController, KoboldStateManager stateManager,
				KoboldGameplayEvents events, KoboldLatcher latcher)
			{
				_bone = bone;
				Target = target;
				_rb = bone.rigidbody;

				_localPos = target.transform.InverseTransformPoint(worldPos);
				_localRot = Quaternion.Inverse(target.transform.rotation) * _rb.rotation;

				if (latcher.enableLatchDebugLogging)
				{
					Debug.Log($"[LATCH-NETWORK] Latching to collider: {target.name} (ID: {target.GetInstanceID()}) on object: {target.gameObject.name} (NetObj: {target.GetComponentInParent<NetworkObject>()?.NetworkObjectId})");
					Debug.Log($"[LATCH-NETWORK] localPos: {_localPos}, localRot: {_localRot}, worldPos: {worldPos}, rb.rotation: {_rb.rotation}");
				}

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

				// Hybrid networking logic
				var networkController = latcher.GetComponentInParent<Kobold.Net.KoboldNetworkController>();
				if (networkController != null && networkController.IsOwner)
				{
					var netObj = target.GetComponentInParent<NetworkObject>();
					var indexer = netObj != null ? netObj.GetComponent<LatchableColliderIndexer>() : null;
					if (netObj != null && indexer != null)
					{
						int idx = indexer.GetColliderIndex(target);
						if (latcher.enableLatchDebugLogging)
						{
							Debug.Log($"[LATCH-NETWORK] Sending SetLatchTarget: NetObj={netObj.NetworkObjectId}, idx={idx}, localPos={_localPos}, localRot={_localRot}");
						}
						networkController.SetHybridLatchTarget(netObj, idx, _localPos, _localRot, true, Vector3.zero, Quaternion.identity);
					}
					else
					{
						if (latcher.enableLatchDebugLogging)
						{
							Debug.Log($"[LATCH-NETWORK] Sending SetLatchTarget: Static world, worldPos={worldPos}, worldRot={_rb.rotation}");
						}
						networkController.SetHybridLatchTarget(null, -1, Vector3.zero, Quaternion.identity, false, worldPos, _rb.rotation);
					}
				}
			}

			public void SetNetworkedLatch(
				NetworkObject targetObj, int colliderIndex, Vector3 localPos, Quaternion localRot,
				bool isNetworked, Vector3 worldPos, Quaternion worldRot, KoboldLatcher latcher)
			{
				Debug.Log($"[LATCH-REMOTE] SetNetworkedLatch called - isNetworked: {isNetworked}, targetObj: {(targetObj != null ? targetObj.name : "NULL")}, colliderIndex: {colliderIndex}");
				
				if (latcher == null)
				{
					Debug.LogError("[LATCH-REMOTE] SetNetworkedLatch called with null latcher!");
					return;
				}

				if (isNetworked)
				{
					if (targetObj == null)
					{
						Debug.LogError("[LATCH-REMOTE] SetNetworkedLatch: Expected networked object, but targetObj is null! Falling back to static geometry.");
						GotoStaticFallback();
						return;
					}
					
					var indexer = targetObj.GetComponent<LatchableColliderIndexer>();
					if (indexer == null)
					{
						Debug.LogError($"[LATCH-REMOTE] SetNetworkedLatch: NetObj={targetObj.NetworkObjectId} has no LatchableColliderIndexer! Falling back to static geometry.");
						GotoStaticFallback();
						return;
					}
					
					Target = indexer.GetColliderByIndex(colliderIndex);
					if (Target == null)
					{
						Debug.LogError($"[LATCH-REMOTE] SetNetworkedLatch: NetObj={targetObj.NetworkObjectId}, idx={colliderIndex}, Target is null! Falling back to static geometry.");
						GotoStaticFallback();
						return;
					}
					
					Debug.Log($"[LATCH-REMOTE] Successfully found target collider: {Target.name} (ID: {Target.GetInstanceID()})");
					
					_localPos = localPos;
					_localRot = localRot;
					IsLatched = true;

					// Get the rigidbody from the latcher's ragdoll animator for remote clients
					if (_rb == null)
					{
						Debug.Log("[LATCH-REMOTE] _rb is null, attempting to get rigidbody from ragdoll animator");
						
						if (latcher.RagdollAnimator == null)
						{
							Debug.LogError("[LATCH-REMOTE] latcher.RagdollAnimator is null!");
							return;
						}
						
						var ragdollHandler = latcher.RagdollAnimator.Handler;
						if (ragdollHandler == null)
						{
							Debug.LogError("[LATCH-REMOTE] ragdollHandler is null!");
							return;
						}
						
						if (latcher.JawMagnet == null)
						{
							Debug.LogError("[LATCH-REMOTE] latcher.JawMagnet is null!");
							return;
						}
						
						if (latcher.JawMagnet.MagnetPoint == null)
						{
							Debug.LogError("[LATCH-REMOTE] latcher.JawMagnet.MagnetPoint is null!");
							return;
						}
						
						var chainBone = ragdollHandler.User_GetBoneSetupBySourceAnimatorBone(latcher.JawMagnet.MagnetPoint.transform);
						if (chainBone == null)
						{
							Debug.LogError("[LATCH-REMOTE] chainBone is null!");
							return;
						}
						
						var boneProcessor = chainBone.BoneProcessor;
						if (boneProcessor == null)
						{
							Debug.LogError("[LATCH-REMOTE] boneProcessor is null!");
							return;
						}
						
						_rb = boneProcessor.rigidbody;
						if (_rb == null)
						{
							Debug.LogError("[LATCH-REMOTE] boneProcessor.rigidbody is null!");
							return;
						}
						
						Debug.Log($"[LATCH-REMOTE] Successfully got rigidbody: {_rb.name}");
					}

					// Notify damage handler on remote clients too
					var damageHandler = Target.GetComponentInParent<LatchDamageHandler>();
					if (damageHandler == null)
					{
						Debug.LogError($"[LATCH-REMOTE] No LatchDamageHandler found in parent of target: {Target.name}");
						return;
					}
					
					if (_rb == null)
					{
						Debug.LogError("[LATCH-REMOTE] _rb is still null, cannot notify damage handler!");
						return;
					}
					
					Debug.Log($"[LATCH-REMOTE] Notifying damage handler for target: {Target.name}");
					damageHandler.OnLatched(_rb.transform);

					if (latcher.enableLatchDebugLogging)
					{
						Debug.Log($"[LATCH-REMOTE] SetNetworkedLatch: NetObj={targetObj.NetworkObjectId}, idx={colliderIndex}, Target={Target.name} (ID: {Target.GetInstanceID()}), localPos={localPos}, localRot={localRot}, worldPos={Target.transform.TransformPoint(localPos)}, worldRot={Target.transform.rotation * localRot}");
					}
					return;
				}
				// Static geometry fallback
				Debug.Log("[LATCH-REMOTE] Using static geometry fallback");
				GotoStaticFallback();
				return;

				void GotoStaticFallback()
				{
					Target = null;
					_localPos = Vector3.zero;
					_localRot = Quaternion.identity;
					_worldPos = worldPos;
					_worldRot = worldRot;
					IsLatched = true;
					Debug.Log($"[LATCH-REMOTE] SetNetworkedLatch: Static world, worldPos={worldPos}, worldRot={worldRot}");
				}
			}

			public void UpdateLatch()
			{
				if (!IsLatched || _rb == null) return;

				if (Target != null)
				{
					var worldPos = Target.transform.TransformPoint(_localPos);
					var worldRot = Target.transform.rotation * _localRot;
					if (((KoboldLatcher)Target.GetComponentInParent<KoboldLatcher>())?.enableLatchDebugLogging ?? false)
					{
						Debug.Log($"[LATCH-UPDATE] Target={Target.name} (ID: {Target.GetInstanceID()}), worldPos={worldPos}, worldRot={worldRot}");
					}
					_rb.position = worldPos;
					_rb.rotation = worldRot;
				}
				else
				{
					// Static geometry: use _worldPos/_worldRot directly
					if (((KoboldLatcher)FindObjectOfType(typeof(KoboldLatcher)))?.enableLatchDebugLogging ?? false)
					{
						Debug.Log($"[LATCH-UPDATE] Static world, using direct _worldPos/_worldRot");
					}
					_rb.position = _worldPos;
					_rb.rotation = _worldRot;
				}
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
