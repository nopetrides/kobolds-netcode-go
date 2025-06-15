using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.GameState;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer;


namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Server-only component which publishes a message once the LifeState changes.
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState), typeof(ServerCharacter))]
    public class PublishMessageOnLifeChange : NetworkBehaviour
    {
        NetworkLifeState _mNetworkLifeState;
        ServerCharacter _mServerCharacter;

        [SerializeField]
        string m_CharacterName;

        NetworkNameState _mNameState;

        [Inject]
        IPublisher<LifeStateChangedEventMessage> _mPublisher;

        void Awake()
        {
            _mNetworkLifeState = GetComponent<NetworkLifeState>();
            _mServerCharacter = GetComponent<ServerCharacter>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _mNameState = GetComponent<NetworkNameState>();
                _mNetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;

                var gameState = FindFirstObjectByType<ServerBossRoomState>();
                if (gameState != null)
                {
                    gameState.Container.Inject(this);
                }
            }
        }

        void OnLifeStateChanged(LifeState previousState, LifeState newState)
        {
            _mPublisher.Publish(new LifeStateChangedEventMessage()
            {
                CharacterName = _mNameState != null ? _mNameState.Name.Value : (FixedPlayerName)m_CharacterName,
                CharacterType = _mServerCharacter.CharacterClass.CharacterType,
                NewLifeState = newState
            });
        }
    }
}
