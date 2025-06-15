using System;
using System.Collections;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a client attempting to reconnect to a server. It will try to reconnect a
    /// number of times defined by the ConnectionManager's NbReconnectAttempts property. If it succeeds, it will
    /// transition to the ClientConnected state. If not, it will transition to the Offline state. If given a disconnect
    /// reason first, depending on the reason given, may not try to reconnect again and transition directly to the
    /// Offline state.
    /// </summary>
    class ClientReconnectingState : ClientConnectingState
    {
        [Inject]
        IPublisher<ReconnectMessage> _mReconnectMessagePublisher;

        Coroutine _mReconnectCoroutine;
        int _mNbAttempts;

        const float KTimeBeforeFirstAttempt = 1;
        const float KTimeBetweenAttempts = 5;

        public override void Enter()
        {
            _mNbAttempts = 0;
            _mReconnectCoroutine = MConnectionManager.StartCoroutine(ReconnectCoroutine());
        }

        public override void Exit()
        {
            if (_mReconnectCoroutine != null)
            {
                MConnectionManager.StopCoroutine(_mReconnectCoroutine);
                _mReconnectCoroutine = null;
            }
            _mReconnectMessagePublisher.Publish(new ReconnectMessage(MConnectionManager.NbReconnectAttempts, MConnectionManager.NbReconnectAttempts));
        }

        public override void OnClientConnected(ulong _)
        {
            MConnectionManager.ChangeState(MConnectionManager.MClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            var disconnectReason = MConnectionManager.NetworkManager.DisconnectReason;
            if (_mNbAttempts < MConnectionManager.NbReconnectAttempts)
            {
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    _mReconnectCoroutine = MConnectionManager.StartCoroutine(ReconnectCoroutine());
                }
                else
                {
                    var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    MConnectStatusPublisher.Publish(connectStatus);
                    switch (connectStatus)
                    {
                        case ConnectStatus.UserRequestedDisconnect:
                        case ConnectStatus.HostEndedSession:
                        case ConnectStatus.ServerFull:
                        case ConnectStatus.IncompatibleBuildType:
                            MConnectionManager.ChangeState(MConnectionManager.MOffline);
                            break;
                        default:
                            _mReconnectCoroutine = MConnectionManager.StartCoroutine(ReconnectCoroutine());
                            break;
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    MConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
                }
                else
                {
                    var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    MConnectStatusPublisher.Publish(connectStatus);
                }

                MConnectionManager.ChangeState(MConnectionManager.MOffline);
            }
        }

        IEnumerator ReconnectCoroutine()
        {
            // If not on first attempt, wait some time before trying again, so that if the issue causing the disconnect
            // is temporary, it has time to fix itself before we try again. Here we are using a simple fixed cooldown
            // but we could want to use exponential backoff instead, to wait a longer time between each failed attempt.
            // See https://en.wikipedia.org/wiki/Exponential_backoff
            if (_mNbAttempts > 0)
            {
                yield return new WaitForSeconds(KTimeBetweenAttempts);
            }

            Debug.Log("Lost connection to host, trying to reconnect...");

            MConnectionManager.NetworkManager.Shutdown();

            yield return new WaitWhile(() => MConnectionManager.NetworkManager.ShutdownInProgress); // wait until NetworkManager completes shutting down
            Debug.Log($"Reconnecting attempt {_mNbAttempts + 1}/{MConnectionManager.NbReconnectAttempts}...");
            _mReconnectMessagePublisher.Publish(new ReconnectMessage(_mNbAttempts, MConnectionManager.NbReconnectAttempts));

            // If first attempt, wait some time before attempting to reconnect to give time to services to update
            // (i.e. if in a Lobby and the host shuts down unexpectedly, this will give enough time for the lobby to be
            // properly deleted so that we don't reconnect to an empty lobby
            if (_mNbAttempts == 0)
            {
                yield return new WaitForSeconds(KTimeBeforeFirstAttempt);
            }

            _mNbAttempts++;
            var reconnectingSetupTask = MConnectionMethod.SetupClientReconnectionAsync();
            yield return new WaitUntil(() => reconnectingSetupTask.IsCompleted);

            if (!reconnectingSetupTask.IsFaulted && reconnectingSetupTask.Result.success)
            {
                // If this fails, the OnClientDisconnect callback will be invoked by Netcode
                var connectingTask = ConnectClientAsync();
                yield return new WaitUntil(() => connectingTask.IsCompleted);
            }
            else
            {
                if (!reconnectingSetupTask.Result.shouldTryAgain)
                {
                    // setting number of attempts to max so no new attempts are made
                    _mNbAttempts = MConnectionManager.NbReconnectAttempts;
                }
                // Calling OnClientDisconnect to mark this attempt as failed and either start a new one or give up
                // and return to the Offline state
                OnClientDisconnect(0);
            }
        }
    }
}
