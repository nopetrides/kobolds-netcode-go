using System;
using System.Text.RegularExpressions;
using Unity.BossRoom.Gameplay.Configuration;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.Networking.Transport;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class IpuiMediator : MonoBehaviour
    {
        public const string KDefaultIP = "127.0.0.1";
        public const int KDefaultPort = 9998;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;

        [SerializeField] IPJoiningUI m_IPJoiningUI;

        [SerializeField] IPHostingUI m_IPHostingUI;

        [SerializeField] UITinter m_JoinTabButtonHighlightTinter;

        [SerializeField] UITinter m_JoinTabButtonTabBlockerTinter;

        [SerializeField] UITinter m_HostTabButtonHighlightTinter;

        [SerializeField] UITinter m_HostTabButtonTabBlockerTinter;

        [SerializeField] GameObject m_SignInSpinner;

        [SerializeField]
        IPConnectionWindow m_IPConnectionWindow;

        [Inject] NameGenerationData _mNameGenerationData;
        [Inject] ConnectionManager _mConnectionManager;

        public IPHostingUI IPHostingUI => m_IPHostingUI;

        ISubscriber<ConnectStatus> _mConnectStatusSubscriber;

        [Inject]
        void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSubscriber)
        {
            _mConnectStatusSubscriber = connectStatusSubscriber;
            _mConnectStatusSubscriber.Subscribe(OnConnectStatusMessage);
        }

        void Awake()
        {
            Hide();
        }

        void Start()
        {
            // show create IP as default
            ToggleCreateIpui();
            RegenerateName();
        }

        void OnDestroy()
        {
            if (_mConnectStatusSubscriber != null)
            {
                _mConnectStatusSubscriber.Unsubscribe(OnConnectStatusMessage);
            }
        }

        void OnConnectStatusMessage(ConnectStatus connectStatus)
        {
            DisableSignInSpinner();
        }

        public void HostIPRequest(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = KDefaultPort;
            }

            ip = string.IsNullOrEmpty(ip) ? KDefaultIP : ip;

            m_SignInSpinner.SetActive(true);
            _mConnectionManager.StartHostIp(m_PlayerNameLabel.text, ip, portNum);
        }

        public void JoinWithIP(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = KDefaultPort;
            }

            ip = string.IsNullOrEmpty(ip) ? KDefaultIP : ip;

            m_SignInSpinner.SetActive(true);

            _mConnectionManager.StartClientIp(m_PlayerNameLabel.text, ip, portNum);

            m_IPConnectionWindow.ShowConnectingWindow();
        }

        public void JoiningWindowCancelled()
        {
            DisableSignInSpinner();
            RequestShutdown();
        }

        public void DisableSignInSpinner()
        {
            m_SignInSpinner.SetActive(false);
        }

        void RequestShutdown()
        {
            if (_mConnectionManager && _mConnectionManager.NetworkManager)
            {
                _mConnectionManager.RequestShutdown();
            }
        }

        public void RegenerateName()
        {
            m_PlayerNameLabel.text = _mNameGenerationData.GenerateName();
        }

        public void ToggleJoinIpui()
        {
            m_IPJoiningUI.Show();
            m_IPHostingUI.Hide();
            m_JoinTabButtonHighlightTinter.SetToColor(1);
            m_JoinTabButtonTabBlockerTinter.SetToColor(1);
            m_HostTabButtonHighlightTinter.SetToColor(0);
            m_HostTabButtonTabBlockerTinter.SetToColor(0);
        }

        public void ToggleCreateIpui()
        {
            m_IPJoiningUI.Hide();
            m_IPHostingUI.Show();
            m_JoinTabButtonHighlightTinter.SetToColor(0);
            m_JoinTabButtonTabBlockerTinter.SetToColor(0);
            m_HostTabButtonHighlightTinter.SetToColor(1);
            m_HostTabButtonTabBlockerTinter.SetToColor(1);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;

            DisableSignInSpinner();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }

        // To be called from the Cancel (X) UI button
        public void CancelConnectingWindow()
        {
            RequestShutdown();
            m_IPConnectionWindow.CancelConnectionWindow();
        }

        /// <summary>
        /// Sanitize user IP address InputField box allowing only numbers and '.'. This also prevents undesirable
        /// invisible characters from being copy-pasted accidentally.
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        public static string SanitizeIP(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^0-9.]", "");
        }

        /// <summary>
        /// Sanitize user port InputField box allowing only numbers. This also prevents undesirable invisible characters
        /// from being copy-pasted accidentally.
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        public static string SanitizePort(string dirtyString)
        {

            return Regex.Replace(dirtyString, "[^0-9]", "");
        }

        public static bool AreIpAddressAndPortValid(string ipAddress, string port)
        {
            var portValid = ushort.TryParse(port, out var portNum);
            return portValid && NetworkEndpoint.TryParse(ipAddress, portNum, out var networkEndPoint);
        }
    }
}
