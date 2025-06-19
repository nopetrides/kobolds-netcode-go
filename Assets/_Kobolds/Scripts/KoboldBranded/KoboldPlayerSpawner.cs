using Kobold.Services;
using Kobolds.Bosses;
using Unity.Netcode;
using UnityEngine;

namespace Kobolds.Gameplay
{
	public class KoboldPlayerSpawner : NetworkBehaviour
	{
		[SerializeField]
		private NetworkObject _playerPrefab;

		private bool _bossesSpawned = false;

		protected override void OnNetworkSessionSynchronized()
		{
			Debug.Assert(_playerPrefab != null, $"Prefab reference '{nameof(_playerPrefab)}' is missing or not assigned.");

			if (_playerPrefab != null)
			{
				var spawnPoint = KoboldPlayerSpawnPoints.Instance.GetRandomSpawnPoint();
				_playerPrefab.InstantiateAndSpawn(
					networkManager: NetworkManager,
					ownerClientId: NetworkManager.LocalClientId,
					isPlayerObject: true,
					position: spawnPoint.position,
					rotation: spawnPoint.rotation
				);
			}

			// Only session owner spawns the boss, and only once
			if (!_bossesSpawned && IsSessionOwner)
			{
				_bossesSpawned = true;
				Debug.Log("[KoboldPlayerSpawner] Spawning bosses as session owner.");
				BossManager.Instance?.SpawnBossAtIndex(0); // Replace with loop if multiple
			}

			base.OnNetworkSessionSynchronized();
		}
	}
}
