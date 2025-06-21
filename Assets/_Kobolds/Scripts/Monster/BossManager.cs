using System.Collections.Generic;
using Kobold.GameManagement;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
    public class BossManager : NetworkBehaviour
    {
        public static BossManager Instance { get; private set; }

        [SerializeField] private NetworkObject bossPrefab;
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
            if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
            {
                Debug.LogError($"Invalid spawn index {spawnIndex}");
                return;
            }

            Vector3 pos = spawnPoints[spawnIndex].position;
            Quaternion rot = spawnPoints[spawnIndex].rotation;
			
            var boss = bossPrefab.InstantiateAndSpawn(
				NetworkManager, destroyWithScene: true, position: pos, rotation: rot);

			var c = boss.GetComponent<MonsterBossController>();
			if (c)
				RegisterBoss(c);
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
            // Use HasAuthority instead of IsOwner for distributed authority
            bool hasAuthority = boss.HasAuthority;
            
            // Configure boss controller - always enabled, but logic controlled by authority
            boss.enabled = true;
            
            // Configure RPC handler - enabled on non-authority clients for visual effects
            var rpcHandler = boss.GetComponent<MonsterBossRPCHandler>();
            if (rpcHandler != null) 
            {
                rpcHandler.enabled = !hasAuthority;
            }
            
            // Configure boss mover - only authority controls movement
            var bossMover = boss.GetComponent<BossMover>();
            if (bossMover != null)
            {
                bossMover.enabled = hasAuthority;
            }
            
            Debug.Log($"[BossManager] Configured boss {boss.name} - Authority: {hasAuthority}");
        }
		
		
    }
}
