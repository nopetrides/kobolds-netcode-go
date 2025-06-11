using UnityEngine;
using FIMSpace.FProceduralAnimation;

namespace Kobolds
{
	public enum GripType
	{
		LeftHand,
		RightHand,
		Jaw
	}

	public class GripMagnetPoint : MonoBehaviour
	{
		[Header("Grip Configuration")]
		[SerializeField] private RagdollAnimator2 Ragdoll;
		[SerializeField] private RA2MagnetPoint Magnet;

		[SerializeField] private GripType GripType;
		[SerializeField] private float GripRadius = 0.4f;
		[SerializeField] private LayerMask GrippableLayers;

		public GripType GripTypeValue => GripType;
		private IGrippable _currentTarget;
		private Collider[] _overlapBuffer = new Collider[6];

		public RA2MagnetPoint MagnetPoint => Magnet;
		public RagdollAnimator2 RagdollAnimator => Ragdoll;
		public RagdollHandler RagdollHandler => Ragdoll.Handler;
		public bool HasTargetAttached => _currentTarget != null;
		
		public bool TryAttachNearby()
		{
			int hits = Physics.OverlapSphereNonAlloc(transform.position, GripRadius, _overlapBuffer, GrippableLayers);
			for (int i = 0; i < hits; i++)
			{
				var grippable = _overlapBuffer[i].GetComponentInParent<IGrippable>();
				if (grippable != null && grippable != _currentTarget)
				{
					if (grippable.TryAttach(this))
					{
						_currentTarget = grippable;
						return true;
					}
				}
			}

			return false;
		}

		public void ReleaseGrip()
		{
			if (_currentTarget != null)
			{
				_currentTarget.Detach(this);
				_currentTarget = null;
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = GripType == GripType.Jaw ?
				Color.yellow :
				(GripType == GripType.LeftHand ? Color.blue : Color.red);
			Gizmos.DrawWireSphere(transform.position, GripRadius);
		}
	}
}