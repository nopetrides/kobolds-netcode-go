using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class SessionOwnerNetworkObjectSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject m_NetworkObjectToSpawn;

        NetworkVariable<bool> _mIsRespawning = new NetworkVariable<bool>();

        NetworkVariable<int> _mTickToRespawn = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            if (IsSessionOwner)
            {
                Spawn();
            }
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
        }

        void Spawn()
        {
            var spawnedNetworkObject = m_NetworkObjectToSpawn.InstantiateAndSpawn(NetworkManager, position: transform.position, rotation: transform.rotation);
            var spawnable = spawnedNetworkObject.GetComponent<ISpawnable>();
            spawnable.Init(this);
            _mIsRespawning.Value = false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="respawnTime"> Network tick at which to respawn this NetworkObject prefab </param>
        [Rpc(SendTo.Authority)]
        public void RespawnRpc(int respawnTime)
        {
            _mTickToRespawn.Value = respawnTime;
            _mIsRespawning.Value = true;
            StartCoroutine(WaitToRespawn());
        }

        IEnumerator WaitToRespawn()
        {
            yield return new WaitUntil(() => NetworkManager.NetworkTickSystem.ServerTime.Tick > _mTickToRespawn.Value);
            Spawn();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (HasAuthority && _mIsRespawning.Value)
            {
                StartCoroutine(WaitToRespawn());
            }
            else
            {
                StopAllCoroutines();
            }
        }

        // Add gizmo to show the spawn position of the network object
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.848f, 0.501f, 0.694f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
