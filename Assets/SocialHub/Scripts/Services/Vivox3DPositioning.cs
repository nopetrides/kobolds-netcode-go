using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class Vivox3DPositioning : NetworkBehaviour
    {
        bool _mInitialized;
        float _mNextPosUpdate;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority)
            {
                enabled = false;
                return;
            }

            GameplayEventHandler.OnChatIsReady += OnChatIsReady;
            GameplayEventHandler.OnExitedSession += OnExitSession;
        }

        void OnChatIsReady(bool chatIsReady, string channelName)
        {
            _mInitialized = chatIsReady;
        }

        void OnExitSession()
        {
            _mInitialized = false;
        }

        void Update()
        {
            if (!_mInitialized)
            {
                return;
            }

            if (Time.time > _mNextPosUpdate)
            {
                VivoxManager.Instance.SetPlayer3DPosition(gameObject);
                _mNextPosUpdate = Time.time + 0.3f;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            GameplayEventHandler.OnChatIsReady -= OnChatIsReady;
            GameplayEventHandler.OnExitedSession -= OnExitSession;
        }
    }
}
