using FIMSpace.FProceduralAnimation;
using Kobold.Net;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace Kobold
{
	[RequireComponent(typeof(RA2AttachableObject))]
	public class AttachableObjectGrippable : MonoBehaviour, IGrippable
	{
		[SerializeField] private string Prompt = "Pick Up";

		[Header("Attachment Offsets")]
		[Tooltip("Local position offset when attached to grip point")]
		[SerializeField] private Vector3 _attachmentPositionOffset = Vector3.zero;

		[Tooltip("Local rotation offset when attached to grip point")]
		[SerializeField] private Vector3 _attachmentRotationOffset = Vector3.zero;

		[Tooltip("If true, ignores the object's current position and snaps to grip point + offset")]
		[SerializeField] private bool _snapToPoint;
		
		[SerializeField] private LayerMask _excludeWhenAttached;
		
		[Header("Physics")]
		[Tooltip("Multiplier for how much mass to add to the hand bone (0 = no mass transfer, 1 = full mass)")]
		[SerializeField] private float _massTransferMultiplier = 1f;

		private RA2AttachableObject _attachableObject;
		private RagdollHandler _lastHandlerAttachedTo;
		private NetworkObject _networkObject;
		private Transform _originalParent;
		
		private GripMagnetPoint _currentMagnet;
		private float _originalHandMass;
		
		private DestructibleObject _destructibleObject;

		private void Awake()
		{
			_attachableObject = GetComponent<RA2AttachableObject>();
			_networkObject = GetComponent<NetworkObject>();
			_originalParent = transform.parent;
			_destructibleObject = GetComponent<DestructibleObject>();
			if(_destructibleObject) _destructibleObject.OnDestruction += OnGrippedItemDestroyed;
		}

		public bool TryAttach(GripMagnetPoint magnet)
		{
			var handler = magnet.RagdollHandler;
			if (handler == null)
			{
				Debug.LogWarning(
					$"[AttachableObjectGrippable] Magnet has no assigned RagdollHandler: {magnet.name}", this);
				return false;
			}

			// Handle network ownership if this is a NetworkObject
			if (_networkObject != null)
			{
				var koboldNetwork = magnet.GetComponentInParent<KoboldNetworkController>();
				if (koboldNetwork != null && koboldNetwork.IsOwner)
				{
					// Request ownership if we don't have it
					if (!_networkObject.HasAuthority)
					{
						if (_networkObject.IsOwnershipTransferable)
							_networkObject.ChangeOwnership(koboldNetwork.OwnerClientId);
						else if (_networkObject.IsOwnershipRequestRequired) _networkObject.RequestOwnership();
					}

					// Set ownership status to prevent others from taking it
					_networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, true);
				}
			}

			var animatorBone = magnet.transform;
			if (animatorBone == null)
			{
				Debug.LogError(
					$"[AttachableObjectGrippable] Failed to map dummy bone '{magnet.transform.name}' to animator bone!",
					this);
				return false;
			}
			// For networked objects, we need to handle attachment differently
			// Store attachment info but don't actually parent
			_lastHandlerAttachedTo = handler;
			
			ApplyMassToRagdollBone(magnet, true);

			// IMPORTANT: Don't use WearAttachable for NetworkObjects!
			// It will try to parent the object which causes the error
			if (_networkObject != null)
			{
				// Disable physics while attached
				var rb = GetComponent<Rigidbody>();
				if (rb != null)
				{
					rb.isKinematic = true;
					rb.linearVelocity = Vector3.zero;
					rb.angularVelocity = Vector3.zero;
					rb.excludeLayers = _excludeWhenAttached;
				}

				// The KoboldGrabber will fire the event which triggers the network sync
				// KoboldNetworkEventListener handles the position updates in LateUpdate
			}
			else
			{
				// Non-networked objects can use the normal attachment system
				handler.WearAttachable(_attachableObject, animatorBone);
			}

			return true;
		}

		public void Detach(GripMagnetPoint magnet)
		{
			ApplyMassToRagdollBone(magnet, false);
			
			if (_networkObject != null)
			{
				// Make it transferable again
				_networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, true);
				_networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);

				// Re-enable physics
				var rb = GetComponent<Rigidbody>();
				if (rb != null)
				{
					rb.isKinematic = false;
					// Apply small impulse
					rb.linearVelocity = magnet.transform.forward * 1f;
					rb.excludeLayers = default;
				}
			}
			else if (_lastHandlerAttachedTo != null)
			{
				// Non-networked objects use normal detach
				_lastHandlerAttachedTo.UnwearAttachable(_attachableObject);
			}

			_lastHandlerAttachedTo = null;
		}

		public string GetInteractionPrompt()
		{
			return Prompt;
		}

		public GameObject GetObject()
		{
			return gameObject;
		}

		/// <summary>
		///     Gets the attachment position offset configured for this grippable object.
		/// </summary>
		public Vector3 GetAttachmentPositionOffset()
		{
			return _attachmentPositionOffset;
		}

		/// <summary>
		///     Gets the attachment rotation offset configured for this grippable object.
		/// </summary>
		public Quaternion GetAttachmentRotationOffset()
		{
			return Quaternion.Euler(_attachmentRotationOffset);
		}

		/// <summary>
		///     Gets whether this object should snap to the grip point or maintain relative offset.
		/// </summary>
		public bool ShouldSnapToGripPoint()
		{
			return _snapToPoint;
		}
		
		/// <summary>
		/// Applies or removes the object's mass to/from the ragdoll hand bone.
		/// </summary>
		private void ApplyMassToRagdollBone(GripMagnetPoint magnet, bool apply)
		{
			// Get our rigidbody
			var objectRb = GetComponent<Rigidbody>();
			if (objectRb == null) return;

			// Find the ragdoll bone's rigidbody
			var handler = magnet.RagdollHandler;
			if (handler == null) return;

			// Get the dummy bone parent rigidbody
			// magnet points are just floating in space so it looks weird if we use them, need the parent
			var dummyBoneRb = FindParentBoneForDummy(_lastHandlerAttachedTo, magnet.transform); // magnet.transform.parent.GetComponent<Rigidbody>();
			if (dummyBoneRb == null)
			{
				Debug.LogWarning($"[AttachableObjectGrippable] No Rigidbody found on grip point: {magnet.name}");
				return;
			}

			if (apply)
			{
				// Store original mass and current magnet
				_originalHandMass = dummyBoneRb.mass;
				_currentMagnet = magnet;
				
				// Add the object's mass to the hand
				float massToAdd = objectRb.mass * _massTransferMultiplier;
				dummyBoneRb.mass = _originalHandMass + massToAdd;
				
				Debug.Log($"[AttachableObjectGrippable] Applied {massToAdd}kg to {magnet.name}. New mass: {dummyBoneRb.mass}kg");
			}
			else
			{
				// Restore original mass
				dummyBoneRb.mass = _originalHandMass;
				_currentMagnet = null;
				
				Debug.Log($"[AttachableObjectGrippable] Restored {magnet.name} mass to: {_originalHandMass}kg");
			}
		}

		public static Transform FindAnimatorBoneForDummy(RagdollHandler handler, Transform dummy)
		{
			foreach (var chain in handler.Chains)
			{
				foreach (var bone in chain.BoneSetups)
					if (bone.PhysicalDummyBone == dummy)
						return bone.SourceBone;
			}

			Debug.LogWarning($"[Grip] Could not find animator bone for dummy transform: {dummy.name}");
			return null;
		}
		
		private static Rigidbody FindParentBoneForDummy(RagdollHandler handler, Transform magnet)
		{
			foreach (var chain in handler.Chains)
			{
				foreach (var bone in chain.BoneSetups)
					if (bone.SourceBone == magnet)
						return bone.ParentBone.GameRigidbody;
			}

			Debug.LogWarning($"[Grip] Could not find animator bone for dummy transform: {magnet.name}");
			return null;
		}

		private void OnGrippedItemDestroyed()
		{
			if (_currentMagnet)
			{
				_currentMagnet.ReleaseGrip();
			}
		}
	}
}
