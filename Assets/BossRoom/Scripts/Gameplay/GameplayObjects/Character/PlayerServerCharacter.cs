using System;
using System.Collections.Generic;
using Unity.BossRoom.ConnectionManagement;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Attached to the player-characters' prefab, this maintains a list of active ServerCharacter objects for players.
    /// </summary>
    /// <remarks>
    /// This is an optimization. In server code you can already get a list of players' ServerCharacters by
    /// iterating over the active connections and calling GetComponent() on their PlayerObject. But we need
    /// to iterate over all players quite often -- the monsters' IdleAIState does so in every Update() --
    /// and all those GetComponent() calls add up! So this optimization lets us iterate without calling
    /// GetComponent(). This will be refactored with a ScriptableObject-based approach on player collection.
    /// </remarks>
    [RequireComponent(typeof(ServerCharacter))]
    public class PlayerServerCharacter : NetworkBehaviour
    {
        static List<ServerCharacter> _sActivePlayers = new List<ServerCharacter>();

        [SerializeField]
        ServerCharacter m_CachedServerCharacter;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _sActivePlayers.Add(m_CachedServerCharacter);
            }
            else
            {
                enabled = false;
            }

        }

        void OnDisable()
        {
            _sActivePlayers.Remove(m_CachedServerCharacter);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                var movementTransform = m_CachedServerCharacter.Movement.transform;
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    var playerData = sessionPlayerData.Value;
                    playerData.PlayerPosition = movementTransform.position;
                    playerData.PlayerRotation = movementTransform.rotation;
                    playerData.CurrentHitPoints = m_CachedServerCharacter.HitPoints;
                    playerData.HasCharacterSpawned = true;
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                }
            }
        }

        /// <summary>
        /// Returns a list of all active players' ServerCharacters. Treat the list as read-only!
        /// The list will be empty on the client.
        /// </summary>
        public static List<ServerCharacter> GetPlayerServerCharacters()
        {
            return _sActivePlayers;
        }

        /// <summary>
        /// Returns the ServerCharacter owned by a specific client. Always returns null on the client.
        /// </summary>
        /// <param name="ownerClientId"></param>
        /// <returns>The ServerCharacter owned by the client, or null if no ServerCharacter is found</returns>
        public static ServerCharacter GetPlayerServerCharacter(ulong ownerClientId)
        {
            foreach (var playerServerCharacter in _sActivePlayers)
            {
                if (playerServerCharacter.OwnerClientId == ownerClientId)
                {
                    return playerServerCharacter;
                }
            }
            return null;
        }
    }
}
