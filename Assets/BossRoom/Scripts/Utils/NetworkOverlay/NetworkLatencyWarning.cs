using System;
using TMPro;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Assertions;


namespace Unity.BossRoom.Utils.Editor
{
    public class NetworkLatencyWarning : MonoBehaviour
    {
        [SerializeField]
        NetworkSimulator m_NetworkSimulator;

        TextMeshProUGUI _mLatencyText;
        bool _mLatencyTextCreated;

        Color _mTextColor = Color.red;

        bool _mArtificialLatencyEnabled;

        void Update()
        {
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
            {
                var unityTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

                // adding this preprocessor directive check since UnityTransport's simulator tools only inject latency in #UNITY_EDITOR or in #DEVELOPMENT_BUILD
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var currentSimulationPreset = m_NetworkSimulator.CurrentPreset;
                _mArtificialLatencyEnabled = currentSimulationPreset.PacketDelayMs > 0 ||
                    currentSimulationPreset.PacketJitterMs > 0 ||
                    currentSimulationPreset.PacketLossInterval > 0 ||
                    currentSimulationPreset.PacketLossPercent > 0;
#else
                _mArtificialLatencyEnabled = false;
#endif

                if (_mArtificialLatencyEnabled)
                {
                    if (!_mLatencyTextCreated)
                    {
                        _mLatencyTextCreated = true;
                        CreateLatencyText();
                    }

                    _mTextColor.a = Mathf.PingPong(Time.time, 1f);
                    _mLatencyText.color = _mTextColor;
                }
            }
            else
            {
                _mArtificialLatencyEnabled = false;
            }

            if (!_mArtificialLatencyEnabled)
            {
                if (_mLatencyTextCreated)
                {
                    _mLatencyTextCreated = false;
                    Destroy(_mLatencyText);
                }
            }
        }

        // Creating a UI text object and add it to NetworkOverlay canvas
        void CreateLatencyText()
        {
            Assert.IsNotNull(NetworkOverlay.Instance,
                "No NetworkOverlay object part of scene. Add NetworkOverlay prefab to bootstrap scene!");

            NetworkOverlay.Instance.AddTextToUI("UI Latency Warning Text", "Network Latency Enabled", out _mLatencyText);
        }
    }
}
