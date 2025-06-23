using Unity.Netcode;
using UnityEngine;

namespace Kobold.Net
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
		private NetworkObject _currentJawObject;

		// Track grabbed objects for position updates
		private NetworkObject _currentLeftHandObject;
		private NetworkObject _currentRightHandObject;
		private Vector3 _jawOffset;
		private Quaternion _jawRotOffset;

		// Local offsets for position sync
		private Vector3 _leftHandOffset;
		private Quaternion _leftHandRotOffset;
		private Vector3 _rightHandOffset;
		private Quaternion _rightHandRotOffset;

		private void Awake()
		{
			if (_gameplayEvents == null) Debug.LogError($"[{name}] KoboldGameplayEvents component not found!");
			if (_networkController == null) Debug.LogError($"[{name}] KoboldNetworkController component not found!");
		}

		private void LateUpdate()
		{
			if (!IsOwner) return;

			// Update positions of grabbed NetworkObjects
			UpdateGrabbedObjectPosition(
				_currentLeftHandObject, _networkController.GetHandBone(true), _leftHandOffset, _leftHandRotOffset);
			UpdateGrabbedObjectPosition(
				_currentRightHandObject, _networkController.GetHandBone(false), _rightHandOffset, _rightHandRotOffset);
			UpdateGrabbedObjectPosition(
				_currentJawObject, _networkController.GetMouthBone(), _jawOffset, _jawRotOffset);
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

		private void UpdateGrabbedObjectPosition(
			NetworkObject grabbedObject, Transform gripPoint, Vector3 localOffset, Quaternion localRotOffset)
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
			_gameplayEvents.OnLatchStateChanged += HandleLatchStateChanged;
			_gameplayEvents.OnLatchStateTransitioned += HandleLatchStateTransitioned;
			_gameplayEvents.OnUnburyComplete += HandleUnburyComplete;
			_gameplayEvents.OnFlop += HandleFlop;
			_gameplayEvents.OnGetUp += HandleGetUp;
		}

		private void UnsubscribeFromEvents()
		{
			if (_gameplayEvents == null) return;

			_gameplayEvents.OnObjectGrabbed -= HandleObjectGrabbed;
			_gameplayEvents.OnObjectReleased -= HandleObjectReleased;
			_gameplayEvents.OnLatched -= HandleLatched;
			_gameplayEvents.OnDetached -= HandleDetached;
			_gameplayEvents.OnLatchStateChanged -= HandleLatchStateChanged;
			_gameplayEvents.OnLatchStateTransitioned -= HandleLatchStateTransitioned;
			_gameplayEvents.OnUnburyComplete -= HandleUnburyComplete;
			_gameplayEvents.OnFlop -= HandleFlop;
			_gameplayEvents.OnGetUp -= HandleGetUp;
		}

		private void HandleObjectGrabbed(GameObject grabbedObject, GripType gripType)
		{
			Debug.Log($"<color=yellow>[HandleObjectGrabbed] {grabbedObject.name}</color>");
			// Convert GameObject to NetworkObject reference
			var networkObject = grabbedObject.GetComponent<NetworkObject>();
			if (networkObject != null)
			{
				// Get the grippable component for offset settings
				var grippable = grabbedObject.GetComponent<AttachableObjectGrippable>();

				// Store reference and calculate offset
				Transform gripPoint = null;
				switch (gripType)
				{
					case GripType.LeftHand:
						_currentLeftHandObject = networkObject;
						gripPoint = _networkController.GetHandBone(true);
						if (gripPoint != null)
						{
							if (grippable != null && grippable.ShouldSnapToGripPoint())
							{
								// Use configured offsets
								_leftHandOffset = grippable.GetAttachmentPositionOffset();
								_leftHandRotOffset = grippable.GetAttachmentRotationOffset();
							}
							else
							{
								// Calculate dynamic offset based on current position
								_leftHandOffset = gripPoint.InverseTransformPoint(grabbedObject.transform.position);
								_leftHandRotOffset = Quaternion.Inverse(gripPoint.rotation) *
													grabbedObject.transform.rotation;
							}
						}

						break;
					case GripType.RightHand:
						_currentRightHandObject = networkObject;
						gripPoint = _networkController.GetHandBone(false);
						if (gripPoint != null)
						{
							if (grippable != null && grippable.ShouldSnapToGripPoint())
							{
								// Use configured offsets
								_rightHandOffset = grippable.GetAttachmentPositionOffset();
								_rightHandRotOffset = grippable.GetAttachmentRotationOffset();
							}
							else
							{
								// Calculate dynamic offset based on current position
								_rightHandOffset = gripPoint.InverseTransformPoint(grabbedObject.transform.position);
								_rightHandRotOffset = Quaternion.Inverse(gripPoint.rotation) *
													grabbedObject.transform.rotation;
							}
						}

						break;
					case GripType.Jaw:
						_currentJawObject = networkObject;
						gripPoint = _networkController.GetMouthBone();
						if (gripPoint != null)
						{
							if (grippable != null && grippable.ShouldSnapToGripPoint())
							{
								// Use configured offsets
								_jawOffset = grippable.GetAttachmentPositionOffset();
								_jawRotOffset = grippable.GetAttachmentRotationOffset();
							}
							else
							{
								// Calculate dynamic offset based on current position
								_jawOffset = gripPoint.InverseTransformPoint(grabbedObject.transform.position);
								_jawRotOffset = Quaternion.Inverse(gripPoint.rotation) *
												grabbedObject.transform.rotation;
							}
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
			var networkObject = target.GetComponentInParent<NetworkObject>();
			if (networkObject != null)
			{
				// Convert local position to world position for the RPC
				var worldPos = target.transform.TransformPoint(localPos);

				_networkController.OnLatchRpc(networkObject, worldPos);
				_networkController.SetLatchTarget(networkObject, localPos, localRot);
				_networkController.SetLatchState(LatchState.Gnawing);
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
			_networkController.SetLatchState(LatchState.None);
		}
		
		private void HandleLatchStateChanged(LatchState newState)
		{
			// Handle latch state changed event
			Debug.Log($"[{name}] Latch state changed to: {newState}");
		}

		private void HandleLatchStateTransitioned(LatchState oldState, LatchState newState)
		{
			// Handle latch state transitioned event
			Debug.Log($"[{name}] Latch state transitioned from: {oldState} to: {newState}");
		}

		private void HandleUnburyComplete()
		{
			// The state change to Active is already handled by KoboldStateManager
			// But we could add specific unbury completion effects here if needed
			Debug.Log($"[{name}] Unbury complete - state should transition to Active");
		}

		private void HandleFlop()
		{
			_networkController.OnFlopRpc();
		}
		
		private void HandleGetUp()
		{
			// The state change to Active is already handled by Flop
			// But we could add specific unbury completion effects here if needed
			_networkController.OnGetUpRpc();
		}
	}
}
