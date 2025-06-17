using System;
using Unity.Netcode;

namespace Kobold.Net
{
	internal interface IKoboldOwnershipRequestable
	{
		event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus>
			OnNetworkObjectOwnershipRequestResponse;
	}
}
