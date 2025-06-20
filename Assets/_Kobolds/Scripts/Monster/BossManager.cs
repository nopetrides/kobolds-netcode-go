using System.Collections.Generic;
using Kobold.GameManagement;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
    public class BossManager : NetworkBehaviour
    {
        public static BossManager Instance { get; private set; }

        [SerializeField] private MonsterBossController bossPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private readonly List<MonsterBossController> _activeBosses = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SpawnBossAtIndex(int spawnIndex)
        {
            if (!IsOwner) return;

            if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
            {
                Debug.LogError($"Invalid spawn index {spawnIndex}");
                return;
            }

            Vector3 pos = spawnPoints[spawnIndex].position;
            Quaternion rot = spawnPoints[spawnIndex].rotation;

            var boss = Instantiate(bossPrefab, pos, rot);
            boss.NetworkObject.Spawn();
			
			// Spawn all networked child objects - redundant?
			// foreach (var netObj in boss.GetComponentsInChildren<NetworkObject>(includeInactive: true))
			// {
			// 	if (!netObj.IsSpawned)
			// 	{
			// 		netObj.Spawn(true); // Use destroyWithScene: true for proper cleanup
			// 	}
			// }

            RegisterBoss(boss);
        }

		public void RegisterBoss(MonsterBossController boss)
		{
			if (!_activeBosses.Contains(boss))
				_activeBosses.Add(boss);
		}

		public void UnregisterBoss(MonsterBossController boss)
		{
			_activeBosses.Remove(boss);
			CheckBossVictoryCondition(); // 🆕
		}

		private void CheckBossVictoryCondition()
		{
			foreach (var boss in _activeBosses)
				if (boss.State != MonsterBossController.BossState.Dead)
					return;

			// All bosses are dead!
			KoboldEventHandler.AllBossesDefeated(); // 🆕
		}


        public IReadOnlyList<MonsterBossController> GetAllBosses() => _activeBosses.AsReadOnly();

        public void ConfigureAuthority(MonsterBossController boss)
        {
            bool isOwner = boss.IsOwner;
            boss.enabled = isOwner;

            var rpcHandler = boss.GetComponent<MonsterBossRPCHandler>();
            if (rpcHandler != null) rpcHandler.enabled = !isOwner;
        }
		
		
    }
}
