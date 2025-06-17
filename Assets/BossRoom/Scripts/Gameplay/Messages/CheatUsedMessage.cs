using System;
using Unity.BossRoom.Utils;
using Unity.Collections;
using Unity.Netcode;

namespace Unity.BossRoom.Gameplay.Messages
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    public struct CheatUsedMessage : INetworkSerializeByMemcpy
    {
        FixedString32Bytes _mCheatUsed;
        FixedPlayerName _mCheaterName;

        public string CheatUsed => _mCheatUsed.ToString();
        public string CheaterName => _mCheaterName.ToString();

        public CheatUsedMessage(string cheatUsed, string cheaterName)
        {
            _mCheatUsed = cheatUsed;
            _mCheaterName = cheaterName;
        }
    }

#endif
}
