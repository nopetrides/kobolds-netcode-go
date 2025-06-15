using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Utils
{
    /// This utility help showing Network statistics at runtime.
    ///
    /// This component attaches to any networked object.
    /// It'll spawn all the needed text and canvas.
    ///
    /// NOTE: This class will be removed once Unity provides support for this.
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkStats : NetworkBehaviour
    {
        // For a value like RTT an exponential moving average is a better indication of the current rtt and fluctuates less.
        struct ExponentialMovingAverageCalculator
        {
            readonly float _mAlpha;
            float _mAverage;

            public float Average => _mAverage;

            public ExponentialMovingAverageCalculator(float average)
            {
                _mAlpha = 2f / (KMaxWindowSize + 1);
                _mAverage = average;
            }

            public float NextValue(float value) => _mAverage = (value - _mAverage) * _mAlpha + _mAverage;
        }

        // RTT
        // Client sends a ping RPC to the server and starts it's timer.
        // The server receives the ping and sends a pong response to the client.
        // The client receives that pong response and stops its time.
        // The RPC value is using a moving average, so we don't have a value that moves too much, but is still reactive to RTT changes.

        const int KMaxWindowSizeSeconds = 3; // it should take x seconds for the value to react to change
        const float KPingIntervalSeconds = 0.1f;
        const float KMaxWindowSize = KMaxWindowSizeSeconds / KPingIntervalSeconds;

        // Some games are less sensitive to latency than others. For fast-paced games, latency above 100ms becomes a challenge for players while for others 500ms is fine. It's up to you to establish those thresholds.
        const float KStrugglingNetworkConditionsRTTThreshold = 130;
        const float KBadNetworkConditionsRTTThreshold = 200;

        ExponentialMovingAverageCalculator _mBossRoomRTT = new ExponentialMovingAverageCalculator(0);
        ExponentialMovingAverageCalculator _mUtpRTT = new ExponentialMovingAverageCalculator(0);

        float _mLastPingTime;
        TextMeshProUGUI _mTextStat;
        TextMeshProUGUI _mTextHostType;
        TextMeshProUGUI _mTextBadNetworkConditions;

        // When receiving pong client RPCs, we need to know when the initiating ping sent it so we can calculate its individual RTT
        int _mCurrentRTTPingId;

        Dictionary<int, float> _mPingHistoryStartTimes = new Dictionary<int, float>();

        RpcParams _mPongClientParams;

        string _mTextToDisplay;

        public override void OnNetworkSpawn()
        {
            bool isClientOnly = IsClient && !IsServer;
            if (!IsOwner && isClientOnly) // we don't want to track player ghost stats, only our own
            {
                enabled = false;
                return;
            }

            if (IsOwner)
            {
                CreateNetworkStatsText();
            }

            _mPongClientParams = RpcTarget.Group(new[] { OwnerClientId }, RpcTargetUse.Persistent);
        }

        // Creating a UI text object and add it to NetworkOverlay canvas
        void CreateNetworkStatsText()
        {
            Assert.IsNotNull(Editor.NetworkOverlay.Instance,
                "No NetworkOverlay object part of scene. Add NetworkOverlay prefab to bootstrap scene!");

            string hostType = IsHost ? "Host" : IsClient ? "Client" : "Unknown";
            Editor.NetworkOverlay.Instance.AddTextToUI("UI Host Type Text", $"Type: {hostType}", out _mTextHostType);
            Editor.NetworkOverlay.Instance.AddTextToUI("UI Stat Text", "No Stat", out _mTextStat);
            Editor.NetworkOverlay.Instance.AddTextToUI("UI Bad Conditions Text", "", out _mTextBadNetworkConditions);
        }

        void FixedUpdate()
        {
            if (!IsServer)
            {
                if (Time.realtimeSinceStartup - _mLastPingTime > KPingIntervalSeconds)
                {
                    // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                    // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                    ServerPingRpc(_mCurrentRTTPingId);
                    _mPingHistoryStartTimes[_mCurrentRTTPingId] = Time.realtimeSinceStartup;
                    _mCurrentRTTPingId++;
                    _mLastPingTime = Time.realtimeSinceStartup;

                    _mUtpRTT.NextValue(NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId));
                }

                if (_mTextStat != null)
                {
                    _mTextToDisplay = $"RTT: {(_mBossRoomRTT.Average * 1000).ToString("0")} ms;\nUTP RTT {_mUtpRTT.Average.ToString("0")} ms";
                    if (_mUtpRTT.Average > KBadNetworkConditionsRTTThreshold)
                    {
                        _mTextStat.color = Color.red;
                    }
                    else if (_mUtpRTT.Average > KStrugglingNetworkConditionsRTTThreshold)
                    {
                        _mTextStat.color = Color.yellow;
                    }
                    else
                    {
                        _mTextStat.color = Color.white;
                    }
                }

                if (_mTextBadNetworkConditions != null)
                {
                    // Right now, we only base this warning on UTP's RTT metric, but in the future we could watch for packet loss as well, or other metrics.
                    // This could be a simple icon instead of doing heavy string manipulations.
                    _mTextBadNetworkConditions.text = _mUtpRTT.Average > KBadNetworkConditionsRTTThreshold ? "Bad Network Conditions Detected!" : "";
                    var color = Color.red;
                    color.a = Mathf.PingPong(Time.time, 1f);
                    _mTextBadNetworkConditions.color = color;
                }
            }
            else
            {
                _mTextToDisplay = $"Connected players: {NetworkManager.Singleton.ConnectedClients.Count.ToString()}";
            }

            if (_mTextStat)
            {
                _mTextStat.text = _mTextToDisplay;
            }
        }

        [Rpc(SendTo.Server)]
        void ServerPingRpc(int pingId, RpcParams serverParams = default)
        {
            ClientPongRpc(pingId, _mPongClientParams);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void ClientPongRpc(int pingId, RpcParams clientParams = default)
        {
            var startTime = _mPingHistoryStartTimes[pingId];
            _mPingHistoryStartTimes.Remove(pingId);
            _mBossRoomRTT.NextValue(Time.realtimeSinceStartup - startTime);
        }

        public override void OnNetworkDespawn()
        {
            if (_mTextStat != null)
            {
                Destroy(_mTextStat.gameObject);
            }

            if (_mTextHostType != null)
            {
                Destroy(_mTextHostType.gameObject);
            }

            if (_mTextBadNetworkConditions != null)
            {
                Destroy(_mTextBadNetworkConditions.gameObject);
            }
        }
    }
}
