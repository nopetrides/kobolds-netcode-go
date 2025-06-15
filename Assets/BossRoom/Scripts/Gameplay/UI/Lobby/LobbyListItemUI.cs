using System;
using TMPro;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    // Note: MultiplayerSDK refactoring
    /// <summary>
    /// An individual Lobby UI in the list of available lobbies
    /// </summary>
    public class LobbyListItemUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_lobbyNameText;
        [SerializeField] TextMeshProUGUI m_lobbyCountText;

        [Inject] LobbyUIMediator _mLobbyUIMediator;

        ISessionInfo _mData;


        public void SetData(ISessionInfo data)
        {
            _mData = data;
            m_lobbyNameText.SetText(data.Name);
            m_lobbyCountText.SetText($"{data.MaxPlayers - data.AvailableSlots}/{data.MaxPlayers}");
        }

        public void OnClick()
        {
            _mLobbyUIMediator.JoinLobbyRequest(_mData);
        }
    }
}
