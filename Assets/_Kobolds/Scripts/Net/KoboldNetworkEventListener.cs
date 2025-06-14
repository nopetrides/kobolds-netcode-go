using Unity.Netcode;
using UnityEngine;

namespace Kobolds.Net
{
	/// <summary>
	///     Listens to gameplay events and translates them to network RPCs.
	///     Maintains separation between gameplay logic and networking.
	/// </summary>
	[RequireComponent(typeof(KoboldNetworkController))]
	public class KoboldNetworkEventListener : NetworkBehaviour
	{
		[SerializeField] private KoboldNetworkController _networkController;
		[SerializeField] private KoboldGameplayEvents _gameplayEvents;
		
		// Track grabbed objects for position updates
		private NetworkObject _currentLeftHandObject;
		private NetworkObject _currentRightHandObject;
		private NetworkObject _currentJawObject;
		
		// Local offsets for position sync
		private Vector3 _leftHandOffset;
		private Quaternion _leftHandRotOffset;
		private Vector3 _rightHandOffset;
		private Quaternion _rightHandRotOffset;
		private Vector3 _jawOffset;
		private Quaternion _jawRotOffset;

		private void Awake()
		{
			if (_gameplayEvents == null) Debug.LogError($"[{name}] KoboldGameplayEvents component not found!");
			if (_networkController == null) Debug.LogError($"[{name}] KoboldNetworkController component not found!");
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// Only subscribe if we're the owner
			if (!IsOwner) return;

			SubscribeToEvents();
		}

		public override void OnNetworkDespawn()
		{
			if (IsOwner) UnsubscribeFromEvents();

			base.OnNetworkDespawn();
		}
		
		private void LateUpdate()
		{
			if (!IsOwner) return;
			
			// Update positions of grabbed NetworkObjects
			UpdateGrabbedObjectPosition(_currentLeftHandObject, _networkController.GetHandBone(true), _leftHandOffset, _leftHandRotOffset);
			UpdateGrabbedObjectPosition(_currentRightHandObject, _networkController.GetHandBone(false), _rightHandOffset, _rightHandRotOffset);
			UpdateGrabbedObjectPosition(_currentJawObject, _networkController.GetMouthBone(), _jawOffset, _jawRotOffset);
		}
		
		private void UpdateGrabbedObjectPosition(NetworkObject grabbedObject, Transform gripPoint, Vector3 localOffset, Quaternion localRotOffset)
		{
			if (grabbedObject == null || gripPoint == null) return;
			
			// Update position without parenting
			grabbedObject.transform.position = gripPoint.TransformPoint(localOffset);
			grabbedObject.transform.rotation = gripPoint.rotation * localRotOffset;
		}

		private void SubscribeToEvents()
		{
			if (_gameplayEvents == null) return;

			_gameplayEvents.OnObjectGrabbed += HandleObjectGrabbed;
			_gameplayEvents.OnObjectReleased += HandleObjectReleased;
			_gameplayEvents.OnLatched += HandleLatched;
			_gameplayEvents.OnDetached += HandleDetached;
			_gameplayEvents.OnUnburyComplete += HandleUnburyComplete;
		}

		private void UnsubscribeFromEvents()
		{
			if (_gameplayEvents == null) return;

			_gameplayEvents.OnObjectGrabbed -= HandleObjectGrabbed;
			_gameplayEvents.OnObjectReleased -= HandleObjectReleased;
			_gameplayEvents.OnLatched -= HandleLatched;
			_gameplayEvents.OnDetached -= HandleDetached;
			_gameplayEvents.OnUnburyComplete -= HandleUnburyComplete;
		}

		private void HandleObjectGrabbed(GameObject grabbedObject, GripType gripType)
		{
			Debug.Log($"<color=yellow>[HandleObjectGrabbed] {grabbedObject.name}</color>");
			// Convert GameObject to NetworkObject reference
			var networkObject = grabbedObject.GetComponent<NetworkObject>();
			if (networkObject != null)
			{
				// Store reference and calculate offset
				Transform gripPoint = null;
				switch (gripType)
				{
					case GripType.LeftHand:
						_currentLeftHandObject = networkObject;
						gripPoint = _networkController.GetHandBone(true);
						if (gripPoint != null)
						{
							_leftHandOffset = gripPoint.InverseTransformPoint(grabbedObject.transform.position);
							_leftHandRotOffset = Quaternion.Inverse(gripPoint.rotation) * grabbedObject.transform.rotation;
						}
						break;
					case GripType.RightHand:
						_currentRightHandObject = networkObject;
						gripPoint = _networkController.GetHandBone(false);
						if (gripPoint != null)
						{
							_rightHandOffset = gripPoint.InverseTransformPoint(grabbedObject.transform.position);
							_rightHandRotOffset = Quaternion.Inverse(gripPoint.rotation) * grabbedObject.transform.rotation;
						}
						break;
					case GripType.Jaw:
						_currentJawObject = networkObject;
						gripPoint = _networkController.GetMouthBone();
						if (gripPoint != null)
						{
							_jawOffset = gripPoint.InverseTransformPoint(grabbedObject.transform.position);
							_jawRotOffset = Quaternion.Inverse(gripPoint.rotation) * grabbedObject.transform.rotation;
						}
						break;
				}
				
				_networkController.OnGrabObjectRpc(networkObject, gripType);
				_networkController.SetGrabbedObject(networkObject);
			}
			else
			{
				Debug.LogWarning($"[{name}] Grabbed object {grabbedObject.name} has no NetworkObject component!");
			}
		}

		private void HandleObjectReleased(GripType gripType)
		{
			// Clear reference
			switch (gripType)
			{
				case GripType.LeftHand:
					_currentLeftHandObject = null;
					break;
				case GripType.RightHand:
					_currentRightHandObject = null;
					break;
				case GripType.Jaw:
					_currentJawObject = null;
					break;
			}
			
			_networkController.OnReleaseObjectRpc(gripType);
			_networkController.SetGrabbedObject(null);
		}

		private void HandleLatched(Collider target, Vector3 localPos, Quaternion localRot)
		{
			// Convert Collider to NetworkObject reference
			var networkObject = target.GetComponent<NetworkObject>();
			if (networkObject != null)
			{
				// Convert local position to world position for the RPC
				var worldPos = target.transform.TransformPoint(localPos);

				_networkController.OnLatchRpc(networkObject, worldPos);
				_networkController.SetLatchTarget(networkObject, localPos, localRot);
			}
			else
			{
				Debug.LogWarning($"[{name}] Latch target {target.name} has no NetworkObject component!");
			}
		}

		private void HandleDetached()
		{
			_networkController.OnDetachRpc();
			_networkController.SetLatchTarget(null, Vector3.zero, Quaternion.identity);
		}

		private void HandleUnburyComplete()
		{
			// The state change to Active is already handled by KoboldStateManager
			// But we could add specific unbury completion effects here if needed
			Debug.Log($"[{name}] Unbury complete - state should transition to Active");
		}
	}
}