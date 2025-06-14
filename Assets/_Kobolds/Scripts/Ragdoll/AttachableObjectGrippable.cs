using UnityEngine;
using FIMSpace.FProceduralAnimation;
using Unity.Netcode;
using Kobolds.Net;

namespace Kobolds
{
	[RequireComponent(typeof(RA2AttachableObject))]
	public class AttachableObjectGrippable : MonoBehaviour, IGrippable
	{
		[SerializeField] private string Prompt = "Pick Up";

		private RA2AttachableObject _attachableObject;
		private RagdollHandler _lastHandlerAttachedTo;
		private NetworkObject _networkObject;
		private Transform _originalParent;

		private void Awake()
		{
			_attachableObject = GetComponent<RA2AttachableObject>();
			_networkObject = GetComponent<NetworkObject>();
			_originalParent = transform.parent;
		}

		public bool TryAttach(GripMagnetPoint magnet)
		{
			var handler = magnet.RagdollHandler;
			if (handler == null)
			{
				Debug.LogWarning($"[AttachableObjectGrippable] Magnet has no assigned RagdollHandler: {magnet.name}", this);
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
						{
							_networkObject.ChangeOwnership(koboldNetwork.OwnerClientId);
						}
						else if (_networkObject.IsOwnershipRequestRequired)
						{
							_networkObject.RequestOwnership();
						}
					}
					
					// Set ownership status to prevent others from taking it
					_networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, clearAndSet: true);
				}
			}

			var animatorBone = magnet.transform;
			if (animatorBone == null)
			{
				Debug.LogError($"[AttachableObjectGrippable] Failed to map dummy bone '{magnet.transform.name}' to animator bone!", this);
				return false;
			}

			// IMPORTANT: Don't use WearAttachable for NetworkObjects!
			// It will try to parent the object which causes the error
			if (_networkObject != null)
			{
				// For networked objects, we need to handle attachment differently
				// Store attachment info but don't actually parent
				_lastHandlerAttachedTo = handler;
				
				// Disable physics while attached
				var rb = GetComponent<Rigidbody>();
				if (rb != null)
				{
					rb.isKinematic = true;
					rb.linearVelocity = Vector3.zero;
					rb.angularVelocity = Vector3.zero;
				}
				
				// The KoboldGrabber will fire the event which triggers the network sync
				// KoboldNetworkEventListener handles the position updates in LateUpdate
			}
			else
			{
				// Non-networked objects can use the normal attachment system
				handler.WearAttachable(_attachableObject, animatorBone);
				_lastHandlerAttachedTo = handler;
			}
			
			return true;
		}

		public void Detach(GripMagnetPoint magnet)
		{
			if (_networkObject != null)
			{
				// Make it transferable again
				_networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
				_networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
				
				// Re-enable physics
				var rb = GetComponent<Rigidbody>();
				if (rb != null)
				{
					rb.isKinematic = false;
					// Apply small impulse
					rb.linearVelocity = magnet.transform.forward * 1f;
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

		public static Transform FindAnimatorBoneForDummy(RagdollHandler handler, Transform dummy)
		{
			foreach (var chain in handler.Chains)
			{
				foreach (var bone in chain.BoneSetups)
				{
					if (bone.PhysicalDummyBone == dummy)
						return bone.SourceBone;
				}
			}

			Debug.LogWarning($"[Grip] Could not find animator bone for dummy transform: {dummy.name}");
			return null;
		}
	}
}