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
			// Convert GameObject to NetworkObject reference
			var networkObject = grabbedObject.GetComponent<NetworkObject>();
			if (networkObject != null)
			{
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
