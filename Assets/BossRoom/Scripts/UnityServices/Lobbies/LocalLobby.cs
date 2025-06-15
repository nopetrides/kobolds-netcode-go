using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.BossRoom.UnityServices.Lobbies
{
    /// <summary>
    /// A local wrapper around a lobby's remote data, with additional functionality for providing that data to UI elements and tracking local player objects.
    /// </summary>
    [Serializable]
    public sealed class LocalLobby
    {
        public event Action<LocalLobby> Changed;

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public static List<LocalLobby> CreateLocalLobbies(QueryResponse response)
        {
            var retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
            {
                retLst.Add(Create(lobby));
            }
            return retLst;
        }

        public static LocalLobby Create(Lobby lobby)
        {
            var data = new LocalLobby();
            data.ApplyRemoteData(lobby);
            return data;
        }

        Dictionary<string, LocalLobbyUser> _mLobbyUsers = new Dictionary<string, LocalLobbyUser>();
        public Dictionary<string, LocalLobbyUser> LobbyUsers => _mLobbyUsers;

        public struct LobbyData
        {
            public string LobbyID { get; set; }
            public string LobbyCode { get; set; }
            public string RelayJoinCode { get; set; }
            public string LobbyName { get; set; }
            public bool Private { get; set; }
            public int MaxPlayerCount { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayJoinCode = existing.RelayJoinCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
            }

            public LobbyData(string lobbyCode)
            {
                LobbyID = null;
                LobbyCode = lobbyCode;
                RelayJoinCode = null;
                LobbyName = null;
                Private = false;
                MaxPlayerCount = -1;
            }
        }

        LobbyData _mData;
        public LobbyData Data => new LobbyData(_mData);

        public void AddUser(LocalLobbyUser user)
        {
            if (!_mLobbyUsers.ContainsKey(user.ID))
            {
                DoAddUser(user);
                OnChanged();
            }
        }

        void DoAddUser(LocalLobbyUser user)
        {
            _mLobbyUsers.Add(user.ID, user);
            user.Changed += OnChangedUser;
        }

        public void RemoveUser(LocalLobbyUser user)
        {
            DoRemoveUser(user);
            OnChanged();
        }

        void DoRemoveUser(LocalLobbyUser user)
        {
            if (!_mLobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            _mLobbyUsers.Remove(user.ID);
            user.Changed -= OnChangedUser;
        }

        void OnChangedUser(LocalLobbyUser user)
        {
            OnChanged();
        }

        void OnChanged()
        {
            Changed?.Invoke(this);
        }

        public string LobbyID
        {
            get => _mData.LobbyID;
            set
            {
                _mData.LobbyID = value;
                OnChanged();
            }
        }

        public string LobbyCode
        {
            get => _mData.LobbyCode;
            set
            {
                _mData.LobbyCode = value;
                OnChanged();
            }
        }

        public string RelayJoinCode
        {
            get => _mData.RelayJoinCode;
            set
            {
                _mData.RelayJoinCode = value;
                OnChanged();
            }
        }

        public string LobbyName
        {
            get => _mData.LobbyName;
            set
            {
                _mData.LobbyName = value;
                OnChanged();
            }
        }

        public bool Private
        {
            get => _mData.Private;
            set
            {
                _mData.Private = value;
                OnChanged();
            }
        }

        public int PlayerCount => _mLobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => _mData.MaxPlayerCount;
            set
            {
                _mData.MaxPlayerCount = value;
                OnChanged();
            }
        }

        public void CopyDataFrom(LobbyData data, Dictionary<string, LocalLobbyUser> currUsers)
        {
            _mData = data;

            if (currUsers == null)
            {
                _mLobbyUsers = new Dictionary<string, LocalLobbyUser>();
            }
            else
            {
                List<LocalLobbyUser> toRemove = new List<LocalLobbyUser>();
                foreach (var oldUser in _mLobbyUsers)
                {
                    if (currUsers.ContainsKey(oldUser.Key))
                    {
                        oldUser.Value.CopyDataFrom(currUsers[oldUser.Key]);
                    }
                    else
                    {
                        toRemove.Add(oldUser.Value);
                    }
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var currUser in currUsers)
                {
                    if (!_mLobbyUsers.ContainsKey(currUser.Key))
                    {
                        DoAddUser(currUser.Value);
                    }
                }
            }

            OnChanged();
        }

        public Dictionary<string, DataObject> GetDataForUnityServices() =>
            new Dictionary<string, DataObject>()
            {
                {"RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public,  RelayJoinCode)}
            };

        public void ApplyRemoteData(Lobby lobby)
        {
            var info = new LobbyData(); // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.
            info.LobbyID = lobby.Id;
            info.LobbyCode = lobby.LobbyCode;
            info.Private = lobby.IsPrivate;
            info.LobbyName = lobby.Name;
            info.MaxPlayerCount = lobby.MaxPlayers;

            if (lobby.Data != null)
            {
                info.RelayJoinCode = lobby.Data.ContainsKey("RelayJoinCode") ? lobby.Data["RelayJoinCode"].Value : null; // By providing RelayCode through the lobby data with Member visibility, we ensure a client is connected to the lobby before they could attempt a relay connection, preventing timing issues between them.
            }
            else
            {
                info.RelayJoinCode = null;
            }

            var lobbyUsers = new Dictionary<string, LocalLobbyUser>();
            foreach (var player in lobby.Players)
            {
                if (player.Data != null)
                {
                    if (LobbyUsers.ContainsKey(player.Id))
                    {
                        lobbyUsers.Add(player.Id, LobbyUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the lobby knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                var incomingData = new LocalLobbyUser
                {
                    IsHost = lobby.HostId.Equals(player.Id),
                    DisplayName = player.Data != null && player.Data.ContainsKey("DisplayName") ? player.Data["DisplayName"].Value : default,
                    ID = player.Id
                };

                lobbyUsers.Add(incomingData.ID, incomingData);
            }

            CopyDataFrom(info, lobbyUsers);
        }

        public void Reset(LocalLobbyUser localUser)
        {
            CopyDataFrom(new LobbyData(), new Dictionary<string, LocalLobbyUser>());
            AddUser(localUser);
        }
    }
}
