using System;
using System.Collections;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Server specialization of Character Select game state.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkCharSelection))]
    public class ServerCharSelectState : GameStateBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        public override GameState ActiveState => GameState.CharSelect;
        public NetworkCharSelection NetworkCharSelection { get; private set; }

        Coroutine _mWaitToEndLobbyCoroutine;

        [Inject]
        ConnectionManager _mConnectionManager;

        protected override void Awake()
        {
            base.Awake();
            NetworkCharSelection = GetComponent<NetworkCharSelection>();

            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_NetcodeHooks)
            {
                m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                m_NetcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        void OnClientChangedSeat(ulong clientId, int newSeatIdx, bool lockedIn)
        {
            int idx = FindLobbyPlayerIdx(clientId);
            if (idx == -1)
            {
                throw new Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and cannot change seats! Shouldn't be here!");
            }

            if (NetworkCharSelection.IsLobbyClosed.Value)
            {
                // The user tried to change their class after everything was locked in... too late! Discard this choice
                return;
            }

            if (newSeatIdx == -1)
            {
                // we can't lock in with no seat
                lockedIn = false;
            }
            else
            {
                // see if someone has already locked-in that seat! If so, too late... discard this choice
                foreach (NetworkCharSelection.LobbyPlayerState playerInfo in NetworkCharSelection.LobbyPlayers)
                {
                    if (playerInfo.ClientId != clientId && playerInfo.SeatIdx == newSeatIdx && playerInfo.SeatState == NetworkCharSelection.SeatState.LockedIn)
                    {
                        // somebody already locked this choice in. Stop!
                        // Instead of granting lock request, change this player to Inactive state.
                        NetworkCharSelection.LobbyPlayers[idx] = new NetworkCharSelection.LobbyPlayerState(clientId,
                            NetworkCharSelection.LobbyPlayers[idx].PlayerName,
                            NetworkCharSelection.LobbyPlayers[idx].PlayerNumber,
                            NetworkCharSelection.SeatState.Inactive);

                        // then early out
                        return;
                    }
                }
            }

            NetworkCharSelection.LobbyPlayers[idx] = new NetworkCharSelection.LobbyPlayerState(clientId,
                NetworkCharSelection.LobbyPlayers[idx].PlayerName,
                NetworkCharSelection.LobbyPlayers[idx].PlayerNumber,
                lockedIn ? NetworkCharSelection.SeatState.LockedIn : NetworkCharSelection.SeatState.Active,
                newSeatIdx,
                Time.time);

            if (lockedIn)
            {
                // to help the clients visually keep track of who's in what seat, we'll "kick out" any other players
                // who were also in that seat. (Those players didn't click "Ready!" fast enough, somebody else took their seat!)
                for (int i = 0; i < NetworkCharSelection.LobbyPlayers.Count; ++i)
                {
                    if (NetworkCharSelection.LobbyPlayers[i].SeatIdx == newSeatIdx && i != idx)
                    {
                        // change this player to Inactive state.
                        NetworkCharSelection.LobbyPlayers[i] = new NetworkCharSelection.LobbyPlayerState(
                            NetworkCharSelection.LobbyPlayers[i].ClientId,
                            NetworkCharSelection.LobbyPlayers[i].PlayerName,
                            NetworkCharSelection.LobbyPlayers[i].PlayerNumber,
                            NetworkCharSelection.SeatState.Inactive);
                    }
                }
            }

            CloseLobbyIfReady();
        }

        /// <summary>
        /// Returns the index of a client in the master LobbyPlayer list, or -1 if not found
        /// </summary>
        int FindLobbyPlayerIdx(ulong clientId)
        {
            for (int i = 0; i < NetworkCharSelection.LobbyPlayers.Count; ++i)
            {
                if (NetworkCharSelection.LobbyPlayers[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Looks through all our connections and sees if everyone has locked in their choice;
        /// if so, we lock in the whole lobby, save state, and begin the transition to gameplay
        /// </summary>
        void CloseLobbyIfReady()
        {
            foreach (NetworkCharSelection.LobbyPlayerState playerInfo in NetworkCharSelection.LobbyPlayers)
            {
                if (playerInfo.SeatState != NetworkCharSelection.SeatState.LockedIn)
                    return; // nope, at least one player isn't locked in yet!
            }

            // everybody's ready at the same time! Lock it down!
            NetworkCharSelection.IsLobbyClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveLobbyResults();

            // Delay a few seconds to give the UI time to react, then switch scenes
            _mWaitToEndLobbyCoroutine = StartCoroutine(WaitToEndLobby());
        }

        /// <summary>
        /// Cancels the process of closing the lobby, so that if a new player joins, they are able to chose a character.
        /// </summary>
        void CancelCloseLobby()
        {
            if (_mWaitToEndLobbyCoroutine != null)
            {
                StopCoroutine(_mWaitToEndLobbyCoroutine);
            }
            NetworkCharSelection.IsLobbyClosed.Value = false;
        }

        void SaveLobbyResults()
        {
            foreach (NetworkCharSelection.LobbyPlayerState playerInfo in NetworkCharSelection.LobbyPlayers)
            {
                var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerInfo.ClientId);

                if (playerNetworkObject && playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
                {
                    // pass avatar GUID to PersistentPlayer
                    // it'd be great to simplify this with something like a NetworkScriptableObjects :(
                    persistentPlayer.NetworkAvatarGuidState.AvatarGuid.Value =
                        NetworkCharSelection.AvatarConfiguration[playerInfo.SeatIdx].Guid.ToNetworkGuid();
                }
            }
        }

        IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            SceneLoaderWrapper.Instance.LoadScene("BossRoom", useNetworkSceneManager: true);
        }

        public void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            if (NetworkCharSelection)
            {
                NetworkCharSelection.OnClientChangedSeat -= OnClientChangedSeat;
            }
        }

        public void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                NetworkCharSelection.OnClientChangedSeat += OnClientChangedSeat;

                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            // We need to filter out the event that are not a client has finished loading the scene
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;
            // When the client finishes loading the Lobby Map, we will need to Seat it
            SeatNewPlayer(sceneEvent.ClientId);
        }

        int GetAvailablePlayerNumber()
        {
            for (int possiblePlayerNumber = 0; possiblePlayerNumber < _mConnectionManager.MaxConnectedPlayers; ++possiblePlayerNumber)
            {
                if (IsPlayerNumberAvailable(possiblePlayerNumber))
                {
                    return possiblePlayerNumber;
                }
            }
            // we couldn't get a Player# for this person... which means the lobby is full!
            return -1;
        }

        bool IsPlayerNumberAvailable(int playerNumber)
        {
            bool found = false;
            foreach (NetworkCharSelection.LobbyPlayerState playerState in NetworkCharSelection.LobbyPlayers)
            {
                if (playerState.PlayerNumber == playerNumber)
                {
                    found = true;
                    break;
                }
            }

            return !found;
        }

        void SeatNewPlayer(ulong clientId)
        {
            // If lobby is closing and waiting to start the game, cancel to allow that new player to select a character
            if (NetworkCharSelection.IsLobbyClosed.Value)
            {
                CancelCloseLobby();
            }

            SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            if (sessionPlayerData.HasValue)
            {
                var playerData = sessionPlayerData.Value;
                if (playerData.PlayerNumber == -1 || !IsPlayerNumberAvailable(playerData.PlayerNumber))
                {
                    // If no player num already assigned or if player num is no longer available, get an available one.
                    playerData.PlayerNumber = GetAvailablePlayerNumber();
                }
                if (playerData.PlayerNumber == -1)
                {
                    // Sanity check. We ran out of seats... there was no room!
                    throw new Exception($"we shouldn't be here, connection approval should have refused this connection already for client ID {clientId} and player num {playerData.PlayerNumber}");
                }

                NetworkCharSelection.LobbyPlayers.Add(new NetworkCharSelection.LobbyPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, NetworkCharSelection.SeatState.Inactive));
                SessionManager<SessionPlayerData>.Instance.SetPlayerData(clientId, playerData);
            }
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            // clear this client's PlayerNumber and any associated visuals (so other players know they're gone).
            for (int i = 0; i < NetworkCharSelection.LobbyPlayers.Count; ++i)
            {
                if (NetworkCharSelection.LobbyPlayers[i].ClientId == clientId)
                {
                    NetworkCharSelection.LobbyPlayers.RemoveAt(i);
                    break;
                }
            }

            if (!NetworkCharSelection.IsLobbyClosed.Value)
            {
                // If the lobby is not already closing, close if the remaining players are all ready
                CloseLobbyIfReady();
            }
        }
    }
}
