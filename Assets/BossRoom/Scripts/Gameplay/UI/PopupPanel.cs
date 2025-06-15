using System;
using UnityEngine;
using TMPro;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Simple popup panel to display information to players.
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_TitleText;
        [SerializeField]
        TextMeshProUGUI m_MainText;
        [SerializeField]
        GameObject m_ConfirmButton;
        [SerializeField]
        GameObject m_LoadingSpinner;
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        public bool IsDisplaying => _mIsDisplaying;

        bool _mIsDisplaying;

        bool _mClosableByUser;

        void Awake()
        {
            Hide();
        }

        public void OnConfirmClick()
        {
            if (_mClosableByUser)
            {
                Hide();
            }
        }

        public void SetupPopupPanel(string titleText, string mainText, bool closeableByUser = true)
        {
            m_TitleText.text = titleText;
            m_MainText.text = mainText;
            _mClosableByUser = closeableByUser;
            m_ConfirmButton.SetActive(_mClosableByUser);
            m_LoadingSpinner.SetActive(!_mClosableByUser);
            Show();
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            _mIsDisplaying = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
            _mIsDisplaying = false;
        }
    }
}
