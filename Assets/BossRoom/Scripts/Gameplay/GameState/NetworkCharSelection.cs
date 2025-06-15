using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Utils;
using Unity.Netcode;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Common data and RPCs for the CharSelect stage.
    /// </summary>
    public class NetworkCharSelection : NetworkBehaviour
    {
        public enum SeatState : byte
        {
            Inactive,
            Active,
            LockedIn,
        }

        /// <summary>
        /// Describes one of the players in the lobby, and their current character-select status.
        /// </summary>
        /// <remarks>
        /// Putting FixedString inside an INetworkSerializeByMemcpy struct is not recommended because it will lose the
        /// bandwidth optimization provided by INetworkSerializable -- an empty FixedString128Bytes serialized normally
        /// or through INetworkSerializable will use 4 bytes of bandwidth, but inside an INetworkSerializeByMemcpy, that
        /// same empty value would consume 132 bytes of bandwidth. 
        /// </remarks>
        public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
        {
            public ulong ClientId;

            private FixedPlayerName _mPlayerName; // I'm sad there's no 256Bytes fixed list :(

            public int PlayerNumber; // this player's assigned "P#". (0=P1, 1=P2, etc.)
            public int SeatIdx; // the latest seat they were in. -1 means none
            public float LastChangeTime;

            public SeatState SeatState;


            public LobbyPlayerState(ulong clientId, string name, int playerNumber, SeatState state, int seatIdx = -1, float lastChangeTime = 0)
            {
                ClientId = clientId;
                PlayerNumber = playerNumber;
                SeatState = state;
                SeatIdx = seatIdx;
                LastChangeTime = lastChangeTime;
                _mPlayerName = new FixedPlayerName();

                PlayerName = name;
            }

            public string PlayerName
            {
                get => _mPlayerName;
                private set => _mPlayerName = value;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref _mPlayerName);
                serializer.SerializeValue(ref PlayerNumber);
                serializer.SerializeValue(ref SeatState);
                serializer.SerializeValue(ref SeatIdx);
                serializer.SerializeValue(ref LastChangeTime);
            }

            public bool Equals(LobbyPlayerState other)
            {
                return ClientId == other.ClientId &&
                       _mPlayerName.Equals(other._mPlayerName) &&
                       PlayerNumber == other.PlayerNumber &&
                       SeatIdx == other.SeatIdx &&
                       LastChangeTime.Equals(other.LastChangeTime) &&
                       SeatState == other.SeatState;
            }
        }

        private NetworkList<LobbyPlayerState> _mLobbyPlayers;

        public Avatar[] AvatarConfiguration;

        private void Awake()
        {
            _mLobbyPlayers = new NetworkList<LobbyPlayerState>();
        }

        /// <summary>
        /// Current state of all players in the lobby.
        /// </summary>
        public NetworkList<LobbyPlayerState> LobbyPlayers => _mLobbyPlayers;

        /// <summary>
        /// When this becomes true, the lobby is closed and in process of terminating (switching to gameplay).
        /// </summary>
        public NetworkVariable<bool> IsLobbyClosed { get; } = new NetworkVariable<bool>(false);

        /// <summary>
        /// Server notification when a client requests a different lobby-seat, or locks in their seat choice
        /// </summary>
        public event Action<ulong, int, bool> OnClientChangedSeat;

        /// <summary>
        /// RPC to notify the server that a client has chosen a seat.
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void ServerChangeSeatRpc(ulong clientId, int seatIdx, bool lockedIn)
        {
            OnClientChangedSeat?.Invoke(clientId, seatIdx, lockedIn);
        }
    }
}
