using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class LobbyCreationUI : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_LobbyNameInputField;
        [SerializeField] GameObject m_LoadingIndicatorObject;
        [SerializeField] Toggle m_IsPrivate;
        [SerializeField] CanvasGroup m_CanvasGroup;
        [Inject] LobbyUIMediator _mLobbyUIMediator;

        void Awake()
        {
            EnableUnityRelayUI();
        }

        void EnableUnityRelayUI()
        {
            m_LoadingIndicatorObject.SetActive(false);
        }

        public void OnCreateClick()
        {
            _mLobbyUIMediator.CreateLobbyRequest(m_LobbyNameInputField.text, m_IsPrivate.isOn);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}
