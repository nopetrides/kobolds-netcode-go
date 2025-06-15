using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Utils
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariableString which represents this object's name.
    /// </summary>
    public class NetworkNameState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<FixedPlayerName> Name = new NetworkVariable<FixedPlayerName>();
    }

    /// <summary>
    /// Wrapping FixedString so that if we want to change player name max size in the future, we only do it once here
    /// </summary>
    public struct FixedPlayerName : INetworkSerializable
    {
        FixedString32Bytes _mName;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _mName);
        }

        public override string ToString()
        {
            return _mName.Value.ToString();
        }

        public static implicit operator string(FixedPlayerName s) => s.ToString();
        public static implicit operator FixedPlayerName(string s) => new FixedPlayerName() { _mName = new FixedString32Bytes(s) };
    }
}
