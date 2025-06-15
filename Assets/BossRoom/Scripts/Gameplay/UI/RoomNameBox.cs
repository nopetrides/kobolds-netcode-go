using System;
using TMPro;
using Unity.BossRoom.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class RoomNameBox : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_RoomNameText;
        [SerializeField]
        Button m_CopyToClipboardButton;

        LocalLobby _mLocalLobby;
        string _mLobbyCode;

        [Inject]
        private void InjectDependencies(LocalLobby localLobby)
        {
            _mLocalLobby = localLobby;
            _mLocalLobby.Changed += UpdateUI;
        }

        void Awake()
        {
            UpdateUI(_mLocalLobby);
        }

        private void OnDestroy()
        {
            _mLocalLobby.Changed -= UpdateUI;
        }

        private void UpdateUI(LocalLobby localLobby)
        {
            if (!string.IsNullOrEmpty(localLobby.LobbyCode))
            {
                _mLobbyCode = localLobby.LobbyCode;
                m_RoomNameText.text = $"Lobby Code: {_mLobbyCode}";
                gameObject.SetActive(true);
                m_CopyToClipboardButton.gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void CopyToClipboard()
        {
            GUIUtility.systemCopyBuffer = _mLobbyCode;
        }
    }
}
