using UnityEngine;
using FIMSpace.FProceduralAnimation;

namespace Kobolds
{
	[RequireComponent(typeof(RA2AttachableObject))]
	public class AttachableObjectGrippable : MonoBehaviour, IGrippable
	{
		[SerializeField] private string Prompt = "Pick Up";

		private RA2AttachableObject _attachableObject;
		private RagdollHandler _lastHandlerAttachedTo;

		private void Awake()
		{
			_attachableObject = GetComponent<RA2AttachableObject>();
		}

		public bool TryAttach(GripMagnetPoint magnet)
		{
			var handler = magnet.RagdollHandler;
			if (handler == null)
			{
				Debug.LogWarning($"[AttachableObjectGrippable] Magnet has no assigned RagdollHandler: {magnet.name}", this);
				return false;
			}

			var animatorBone = magnet.transform;
			//var animatorBone = FindAnimatorBoneForDummy(handler, magnet.transform);
			if (animatorBone == null)
			{
				Debug.LogError($"[AttachableObjectGrippable] Failed to map dummy bone '{magnet.transform.name}' to animator bone!", this);
				return false;
			}

			handler.WearAttachable(_attachableObject, animatorBone);
			_lastHandlerAttachedTo = handler;
			return true;
		}

		public void Detach(GripMagnetPoint magnet)
		{
			if (_lastHandlerAttachedTo != null)
			{
				_lastHandlerAttachedTo.UnwearAttachable(_attachableObject);
				_lastHandlerAttachedTo = null;
			}

			// Optionally apply small physics impulse or reset transform
			var rb = _attachableObject.GetComponent<Rigidbody>();
			if (rb != null && rb.linearVelocity.magnitude < 0.1f)
			{
				rb.linearVelocity = magnet.transform.forward * 1f;
			}
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
