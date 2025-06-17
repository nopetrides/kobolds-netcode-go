using System;
using System.Collections;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class IPConnectionWindow : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TextMeshProUGUI m_TitleText;

        [Inject] IpuiMediator _mIpuiMediator;

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

        void OnDestroy()
        {
            if (_mConnectStatusSubscriber != null)
            {
                _mConnectStatusSubscriber.Unsubscribe(OnConnectStatusMessage);
            }
        }

        void OnConnectStatusMessage(ConnectStatus connectStatus)
        {
            CancelConnectionWindow();
            _mIpuiMediator.DisableSignInSpinner();
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void ShowConnectingWindow()
        {
            void OnTimeElapsed()
            {
                Hide();
                _mIpuiMediator.DisableSignInSpinner();
            }

            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            var maxConnectAttempts = utp.MaxConnectAttempts;
            var connectTimeoutMS = utp.ConnectTimeoutMS;
            StartCoroutine(DisplayUtpConnectionDuration(maxConnectAttempts, connectTimeoutMS, OnTimeElapsed));

            Show();
        }

        public void CancelConnectionWindow()
        {
            Hide();
            StopAllCoroutines();
        }

        IEnumerator DisplayUtpConnectionDuration(int maxReconnectAttempts, int connectTimeoutMS, Action endAction)
        {
            var connectionDuration = maxReconnectAttempts * connectTimeoutMS / 1000f;

            var seconds = Mathf.CeilToInt(connectionDuration);

            while (seconds > 0)
            {
                m_TitleText.text = $"Connecting...\n{seconds}";
                yield return new WaitForSeconds(1f);
                seconds--;
            }
            m_TitleText.text = "Connecting...";

            endAction();
        }

        // invoked by UI cancel button
        public void OnCancelJoinButtonPressed()
        {
            CancelConnectionWindow();
            _mIpuiMediator.JoiningWindowCancelled();
        }
    }
}
