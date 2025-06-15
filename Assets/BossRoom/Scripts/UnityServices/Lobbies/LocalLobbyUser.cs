using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Unity.BossRoom.UnityServices.Lobbies
{
    /// <summary>
    /// Data for a local lobby user instance. This will update data and is observed to know when to push local user changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LocalLobbyUser
    {
        public event Action<LocalLobbyUser> Changed;

        public LocalLobbyUser()
        {
            _mUserData = new UserData(isHost: false, displayName: null, id: null);
        }

        public struct UserData
        {
            public bool IsHost { get; set; }
            public string DisplayName { get; set; }
            public string ID { get; set; }

            public UserData(bool isHost, string displayName, string id)
            {
                IsHost = isHost;
                DisplayName = displayName;
                ID = id;
            }
        }

        UserData _mUserData;

        public void ResetState()
        {
            _mUserData = new UserData(false, _mUserData.DisplayName, _mUserData.ID);
        }

        /// <summary>
        /// Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [Flags]
        public enum UserMembers
        {
            IsHost = 1,
            DisplayName = 2,
            ID = 4,
        }

        UserMembers _mLastChanged;

        public bool IsHost
        {
            get { return _mUserData.IsHost; }
            set
            {
                if (_mUserData.IsHost != value)
                {
                    _mUserData.IsHost = value;
                    _mLastChanged = UserMembers.IsHost;
                    OnChanged();
                }
            }
        }

        public string DisplayName
        {
            get => _mUserData.DisplayName;
            set
            {
                if (_mUserData.DisplayName != value)
                {
                    _mUserData.DisplayName = value;
                    _mLastChanged = UserMembers.DisplayName;
                    OnChanged();
                }
            }
        }

        public string ID
        {
            get => _mUserData.ID;
            set
            {
                if (_mUserData.ID != value)
                {
                    _mUserData.ID = value;
                    _mLastChanged = UserMembers.ID;
                    OnChanged();
                }
            }
        }


        public void CopyDataFrom(LocalLobbyUser lobby)
        {
            var data = lobby._mUserData;
            int lastChanged = // Set flags just for the members that will be changed.
                (_mUserData.IsHost == data.IsHost ? 0 : (int)UserMembers.IsHost) |
                (_mUserData.DisplayName == data.DisplayName ? 0 : (int)UserMembers.DisplayName) |
                (_mUserData.ID == data.ID ? 0 : (int)UserMembers.ID);

            if (lastChanged == 0) // Ensure something actually changed.
            {
                return;
            }

            _mUserData = data;
            _mLastChanged = (UserMembers)lastChanged;

            OnChanged();
        }

        void OnChanged()
        {
            Changed?.Invoke(this);
        }

        public Dictionary<string, PlayerDataObject> GetDataForUnityServices() =>
            new Dictionary<string, PlayerDataObject>()
            {
                {"DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, DisplayName)},
            };
    }
}
