using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



namespace Unity.Multiplayer.Samples.Utilities
{
	/// <summary>
	///     Contains data on scene loading progress for the local instance and remote instances.
	/// </summary>
	public class LoadingProgressManager : NetworkBehaviour
	{
		[SerializeField] private GameObject m_ProgressTrackerPrefab;

		private bool _mIsLoading;

		private AsyncOperation _mLocalLoadOperation;

		private float _mLocalProgress;

		/// <summary>
		///     Dictionary containing references to the NetworkedLoadingProgessTrackers that contain the loading progress of
		///     each client. Keys are ClientIds.
		/// </summary>
		public Dictionary<ulong, NetworkedLoadingProgressTracker> ProgressTrackers { get; } = new();

		/// <summary>
		///     This is the AsyncOperation of the current load operation. This property should be set each time a new
		///     loading operation begins.
		/// </summary>
		public AsyncOperation LocalLoadOperation
		{
			set
			{
				_mIsLoading = true;
				LocalProgress = 0;
				_mLocalLoadOperation = value;
			}
		}

		/// <summary>
		///     The current loading progress for the local client. Handled by a local field if not in a networked session,
		///     or by a progress tracker from the dictionary.
		/// </summary>
		public float LocalProgress
		{
			get =>
				IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) ?
					ProgressTrackers[NetworkManager.LocalClientId].Progress.Value :
					_mLocalProgress;
			private set
			{
				if (IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) &&
					ProgressTrackers[NetworkManager.LocalClientId].IsSpawned)
					ProgressTrackers[NetworkManager.LocalClientId].Progress.Value = value;
				else
					_mLocalProgress = value;
			}
		}

		private void Update()
		{
			if (_mLocalLoadOperation != null && _mIsLoading)
			{
				if (_mLocalLoadOperation.isDone)
				{
					_mIsLoading = false;
					LocalProgress = 1;
				}
				else
				{
					LocalProgress = _mLocalLoadOperation.progress;
				}
			}
		}

		/// <summary>
		///     This event is invoked each time the dictionary of progress trackers is updated (if one is removed or added, for
		///     example.)
		/// </summary>
		public event Action OnTrackersUpdated;

		public override void OnNetworkSpawn()
		{
			if (IsServer)
			{
				NetworkManager.OnClientConnectedCallback += AddTracker;
				NetworkManager.OnClientDisconnectCallback += RemoveTracker;
				AddTracker(NetworkManager.LocalClientId);
			}
		}

		public override void OnNetworkDespawn()
		{
			if (IsServer)
			{
				NetworkManager.OnClientConnectedCallback -= AddTracker;
				NetworkManager.OnClientDisconnectCallback -= RemoveTracker;
			}

			ProgressTrackers.Clear();
			OnTrackersUpdated?.Invoke();
		}

		[Rpc(SendTo.ClientsAndHost)]
		private void ClientUpdateTrackersRpc()
		{
			if (!IsHost)
			{
				ProgressTrackers.Clear();
				foreach (var tracker in FindObjectsOfType<NetworkedLoadingProgressTracker>())
					// If a tracker is despawned but not destroyed yet, don't add it
					if (tracker.IsSpawned)
					{
						ProgressTrackers[tracker.OwnerClientId] = tracker;
						if (tracker.OwnerClientId == NetworkManager.LocalClientId)
							LocalProgress = Mathf.Max(_mLocalProgress, LocalProgress);
					}
			}

			OnTrackersUpdated?.Invoke();
		}

		private void AddTracker(ulong clientId)
		{
			if (IsServer)
			{
				var tracker = Instantiate(m_ProgressTrackerPrefab);
				var networkObject = tracker.GetComponent<NetworkObject>();
				networkObject.SpawnWithOwnership(clientId);
				ProgressTrackers[clientId] = tracker.GetComponent<NetworkedLoadingProgressTracker>();
				ClientUpdateTrackersRpc();
			}
		}

		private void RemoveTracker(ulong clientId)
		{
			if (IsServer)
				if (ProgressTrackers.ContainsKey(clientId))
				{
					var tracker = ProgressTrackers[clientId];
					ProgressTrackers.Remove(clientId);
					tracker.NetworkObject.Despawn();
					ClientUpdateTrackersRpc();
				}
		}
	}
}
