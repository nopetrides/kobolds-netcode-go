using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

namespace Unity.BossRoom.Gameplay.UI
{
    public class IPHostingUI : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_IPInputField;
        [SerializeField] TMP_InputField m_PortInputField;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        Button m_HostButton;

        [Inject] IpuiMediator _mIpuiMediator;

        void Awake()
        {
            m_IPInputField.text = IpuiMediator.KDefaultIP;
            m_PortInputField.text = IpuiMediator.KDefaultPort.ToString();
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

        public void OnCreateClick()
        {
            _mIpuiMediator.HostIPRequest(m_IPInputField.text, m_PortInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Room/IP UI text.
        /// </summary>
        public void SanitizeIPInputText()
        {
            m_IPInputField.text = IpuiMediator.SanitizeIP(m_IPInputField.text);
            m_HostButton.interactable = IpuiMediator.AreIpAddressAndPortValid(m_IPInputField.text, m_PortInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            m_PortInputField.text = IpuiMediator.SanitizePort(m_PortInputField.text);
            m_HostButton.interactable = IpuiMediator.AreIpAddressAndPortValid(m_IPInputField.text, m_PortInputField.text);
        }
    }
}
