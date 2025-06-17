using System;
using Kobold.GameManagement;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Net
{
	internal class KoboldTransferableObject : NetworkBehaviour, IKoboldOwnershipRequestable
	{
		public enum ObjectState
		{
			AtRest,
			PickedUp,
			Thrown
		} 
		internal ObjectState CurrentObjectState { get; private set; }

		public event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus>
			OnNetworkObjectOwnershipRequestResponse;

		public override void OnNetworkSpawn()
		{
			if (HasAuthority)
			{
				NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, true);
				NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
			}

			base.OnNetworkSpawn();

			NetworkObject.OnOwnershipRequested += OnOwnershipRequested;
			NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponse;
		}

		public override void OnNetworkDespawn()
		{
			base.OnNetworkDespawn();
			if (NetworkObject)
			{
				NetworkObject.OnOwnershipRequested -= OnOwnershipRequested;
				NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
			}

			KoboldEventHandler.NetworkObjectDespawned(NetworkObject);
			OnNetworkObjectOwnershipRequestResponse = null;
		}

		protected override void OnOwnershipChanged(ulong previous, ulong current)
		{
			base.OnOwnershipChanged(previous, current);

			KoboldEventHandler.NetworkObjectOwnershipChanged(NetworkObject, previous, current);
		}

		// note: invoked on owning client
		private bool OnOwnershipRequested(ulong clientRequesting)
		{
			// defaulting all ownership requests to true, as is the default for all ownership requests
			// here, you'd introduce game-based logic to deny/approve requests
			return true;
		}

		// note: invoked on requesting client
		private void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus ownershipRequestResponse)
		{
			OnNetworkObjectOwnershipRequestResponse?.Invoke(this, ownershipRequestResponse);
		}

		internal void SetObjectState(ObjectState state)
		{
			CurrentObjectState = state;
		}
	}
}
