using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class NetworkObjectDespawner : NetworkBehaviour
    {
        [SerializeField]
        float m_SecondsUntilDespawn;

        NetworkVariable<int> _mDespawnTick = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                _mDespawnTick.Value = NetworkManager.ServerTime.Tick + Mathf.RoundToInt(NetworkManager.ServerTime.TickRate * m_SecondsUntilDespawn);
            }
            OnOwnershipChanged(0L, 0L);
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (HasAuthority)
            {
                StartCoroutine(DespawnCoroutine());
            }
            else
            {
                StopAllCoroutines();
            }
        }

        IEnumerator DespawnCoroutine()
        {
            yield return new WaitUntil(() => NetworkManager.NetworkTickSystem.ServerTime.Tick > _mDespawnTick.Value);
            // TODO: add hook to this NetworkObject's pool system
            NetworkObject.Despawn();
        }
    }
}
