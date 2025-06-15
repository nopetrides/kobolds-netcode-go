using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.BossRoom.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using Unity.Services.Wire.Internal;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Unity.BossRoom.UnityServices.Lobbies
{
    // Note: MultiplayerSDK refactoring
    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want.
    /// </summary>
    public class LobbyServiceFacade : IDisposable, IStartable
    {
        [Inject] LifetimeScope _mParentScope;
        [Inject] UpdateRunner _mUpdateRunner;
        [Inject] LocalLobby _mLocalLobby;
        [Inject] LocalLobbyUser _mLocalUser;
        [Inject] IPublisher<UnityServiceErrorMessage> _mUnityServiceErrorMessagePub;
        [Inject] IPublisher<LobbyListFetchedMessage> _mLobbyListFetchedPub;

        const float KHeartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.
        float _mHeartbeatTime = 0;

        LifetimeScope _mServiceScope;
        LobbyAPIInterface _mLobbyApiInterface;

        RateLimitCooldown _mRateLimitQuery;
        RateLimitCooldown _mRateLimitJoin;
        RateLimitCooldown _mRateLimitQuickJoin;
        RateLimitCooldown _mRateLimitHost;

        public Lobby CurrentUnityLobby { get; private set; }

        ILobbyEvents _mLobbyEvents;

        bool _mIsTracking = false;

        LobbyEventConnectionState _mLobbyEventConnectionState = LobbyEventConnectionState.Unknown;

        public ISession CurrentSession;

        public void Start()
        {
            _mServiceScope = _mParentScope.CreateChild(builder =>
            {
                builder.Register<LobbyAPIInterface>(Lifetime.Singleton);
            });

            _mLobbyApiInterface = _mServiceScope.Container.Resolve<LobbyAPIInterface>();

            //See https://docs.unity.com/lobby/rate-limits.html
            _mRateLimitQuery = new RateLimitCooldown(1f);
            _mRateLimitJoin = new RateLimitCooldown(3f);
            _mRateLimitQuickJoin = new RateLimitCooldown(10f);
            _mRateLimitHost = new RateLimitCooldown(3f);
        }

        public void Dispose()
        {
            EndTracking();
            if (_mServiceScope != null)
            {
                _mServiceScope.Dispose();
            }
        }

        public void SetRemoteLobby(Lobby lobby)
        {
            CurrentUnityLobby = lobby;
            _mLocalLobby.ApplyRemoteData(lobby);
        }

        /// <summary>
        /// Initiates tracking of joined lobby's events. The host also starts sending heartbeat pings here.
        /// </summary>
        public void BeginTracking()
        {
            if (!_mIsTracking)
            {
                _mIsTracking = true;
                SubscribeToJoinedLobbyAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (_mLocalUser.IsHost)
                {
                    _mHeartbeatTime = 0;
                    _mUpdateRunner.Subscribe(DoLobbyHeartbeat, 1.5f);
                }
            }
        }

        /// <summary>
        /// Ends tracking of joined lobby's events and leaves or deletes the lobby. The host also stops sending heartbeat pings here.
        /// </summary>
        public void EndTracking()
        {
            if (_mIsTracking)
            {
                _mIsTracking = false;
                UnsubscribeToJoinedLobbyAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (_mLocalUser.IsHost)
                {
                    _mUpdateRunner.Unsubscribe(DoLobbyHeartbeat);
                }
            }

            if (CurrentUnityLobby != null)
            {
                if (_mLocalUser.IsHost)
                {
                    DeleteLobbyAsync();
                }
                else
                {
                    LeaveLobbyAsync();
                }
            }
        }

        // Note: MultiplayerSDK refactoring
        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public async Task<(bool Success, ISession Lobby)> TryCreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate)
        {
            if (!_mRateLimitHost.CanCall)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = /*await m_LobbyApiInterface.CreateLobby(AuthenticationService.Instance.PlayerId, lobbyName, maxPlayers, isPrivate, m_LocalUser.GetDataForUnityServices(), null);*/
                    await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions()
                    {
                        MaxPlayers = 2,
                        Name = lobbyName,
                        IsPrivate = isPrivate,
                        Password = null,//string.IsNullOrEmpty(Password) ? null : Password,
                        IsLocked = false, //Todos
                    }.WithRelayNetwork());
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    _mRateLimitHost.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        // Note: MultiplayerSDK refactoring
        /// <summary>
        /// Attempt to join an existing lobby. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public async Task<(bool Success, ISession Lobby)> TryJoinLobbyAsync(string lobbyId/*, string lobbyCode*/)
        {
            if (!_mRateLimitJoin.CanCall ||
                (lobbyId == null/* && lobbyCode == null*/))
            {
                Debug.LogWarning("Join Lobby hit the rate limit.");
                return (false, null);
            }

            Debug.Log($"joinning session with lobby code {lobbyId}");
            
            try
            {
                var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(lobbyId,
                    new JoinSessionOptions()
                    {
                        /*Password = string.IsNullOrEmpty(Password) ? null : Password,
                        PlayerProperties = PlayerData*/
                    });
                return (true, session);
                /*if (!string.IsNullOrEmpty(lobbyCode))
                {
                    var lobby = await m_LobbyApiInterface.JoinLobbyByCode(AuthenticationService.Instance.PlayerId, lobbyCode, m_LocalUser.GetDataForUnityServices());
                    return (true, lobby);
                }
                else
                {
                    var lobby = await m_LobbyApiInterface.JoinLobbyById(AuthenticationService.Instance.PlayerId, lobbyId, m_LocalUser.GetDataForUnityServices());
                    return (true, lobby);
                }*/
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    _mRateLimitJoin.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered onlineMode.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryQuickJoinLobbyAsync()
        {
            if (!_mRateLimitQuickJoin.CanCall)
            {
                Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = await _mLobbyApiInterface.QuickJoinLobby(AuthenticationService.Instance.PlayerId, _mLocalUser.GetDataForUnityServices());
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    _mRateLimitQuickJoin.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        void ResetLobby()
        {
            CurrentUnityLobby = null;
            if (_mLocalUser != null)
            {
                _mLocalUser.ResetState();
            }
            if (_mLocalLobby != null)
            {
                _mLocalLobby.Reset(_mLocalUser);
            }

            // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
        }

        void OnLobbyChanges(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                Debug.Log("Lobby deleted");
                ResetLobby();
                EndTracking();
            }
            else
            {
                Debug.Log("Lobby updated");
                changes.ApplyToLobby(CurrentUnityLobby);
                _mLocalLobby.ApplyRemoteData(CurrentUnityLobby);

                // as client, check if host is still in lobby
                if (!_mLocalUser.IsHost)
                {
                    foreach (var lobbyUser in _mLocalLobby.LobbyUsers)
                    {
                        if (lobbyUser.Value.IsHost)
                        {
                            return;
                        }
                    }

                    _mUnityServiceErrorMessagePub.Publish(new UnityServiceErrorMessage("Host left the lobby", "Disconnecting.", UnityServiceErrorMessage.Service.Lobby));
                    EndTracking();
                    // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
                }
            }
        }

        void OnKickedFromLobby()
        {
            Debug.Log("Kicked from Lobby");
            ResetLobby();
            EndTracking();
        }

        void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState lobbyEventConnectionState)
        {
            _mLobbyEventConnectionState = lobbyEventConnectionState;
            Debug.Log($"LobbyEventConnectionState changed to {lobbyEventConnectionState}");
        }

        async void SubscribeToJoinedLobbyAsync()
        {
            var lobbyEventCallbacks = new LobbyEventCallbacks();
            lobbyEventCallbacks.LobbyChanged += OnLobbyChanges;
            lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobby;
            lobbyEventCallbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
            // The LobbyEventCallbacks object created here will now be managed by the Lobby SDK. The callbacks will be
            // unsubscribed from when we call UnsubscribeAsync on the ILobbyEvents object we receive and store here.
            _mLobbyEvents = await _mLobbyApiInterface.SubscribeToLobby(_mLocalLobby.LobbyID, lobbyEventCallbacks);
        }

        async void UnsubscribeToJoinedLobbyAsync()
        {
            if (_mLobbyEvents != null && _mLobbyEventConnectionState != LobbyEventConnectionState.Unsubscribed)
            {
#if UNITY_EDITOR
                try
                {
                    await _mLobbyEvents.UnsubscribeAsync();
                }
                catch (WebSocketException e)
                {
                    // This exception occurs in the editor when exiting play mode without first leaving the lobby.
                    // This is because Wire closes the websocket internally when exiting playmode in the editor.
                    Debug.Log(e.Message);
                }
#else
                await _mLobbyEvents.UnsubscribeAsync();
#endif
            }
        }

        // Note: MultiplayerSDK refactoring
        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public async Task RetrieveAndPublishLobbyListAsync()
        {
            if (!_mRateLimitQuery.CanCall)
            {
                Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return;
            }

            try
            {
                /*var response = await m_LobbyApiInterface.QueryAllLobbies();*/
                var queryResults = await MultiplayerService.Instance.QuerySessionsAsync(new()
                {
                });
                _mLobbyListFetchedPub.Publish(new LobbyListFetchedMessage(queryResults.Sessions/*LocalLobby.CreateLocalLobbies(response)*/));
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    _mRateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }
        }

        public async Task<Lobby> ReconnectToLobbyAsync()
        {
            try
            {
                return await _mLobbyApiInterface.ReconnectToLobby(_mLocalLobby.LobbyID);
            }
            catch (LobbyServiceException e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound && !_mLocalUser.IsHost)
                {
                    PublishError(e);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to leave a lobby
        /// </summary>
        async void LeaveLobbyAsync()
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            try
            {
                await _mLobbyApiInterface.RemovePlayerFromLobby(uasId, _mLocalLobby.LobbyID);
            }
            catch (LobbyServiceException e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound && !_mLocalUser.IsHost)
                {
                    PublishError(e);
                }
            }
            finally
            {
                ResetLobby();
            }

        }

        public async void RemovePlayerFromLobbyAsync(string uasId)
        {
            if (_mLocalUser.IsHost)
            {
                try
                {
                    await _mLobbyApiInterface.RemovePlayerFromLobby(uasId, _mLocalLobby.LobbyID);
                }
                catch (LobbyServiceException e)
                {
                    PublishError(e);
                }
            }
            else
            {
                Debug.LogError("Only the host can remove other players from the lobby.");
            }
        }

        async void DeleteLobbyAsync()
        {
            if (_mLocalUser.IsHost)
            {
                try
                {
                    await _mLobbyApiInterface.DeleteLobby(_mLocalLobby.LobbyID);
                }
                catch (LobbyServiceException e)
                {
                    PublishError(e);
                }
                finally
                {
                    ResetLobby();
                }
            }
            else
            {
                Debug.LogError("Only the host can delete a lobby.");
            }
        }

        /// <summary>
        /// Attempt to push a set of key-value pairs associated with the local player which will overwrite any existing
        /// data for these keys. Lobby can be provided info about Relay (or any other remote allocation) so it can add
        /// automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerDataAsync(string allocationId, string connectionInfo)
        {
            if (!_mRateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                var result = await _mLobbyApiInterface.UpdatePlayer(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, _mLocalUser.GetDataForUnityServices(), allocationId, connectionInfo);

                if (result != null)
                {
                    CurrentUnityLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    _mRateLimitQuery.PutOnCooldown();
                }
                else if (e.Reason != LobbyExceptionReason.LobbyNotFound && !_mLocalUser.IsHost) // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Attempt to update the set of key-value pairs associated with a given lobby and unlocks it so clients can see it.
        /// </summary>
        public async Task UpdateLobbyDataAndUnlockAsync()
        {
            if (!_mRateLimitQuery.CanCall)
            {
                return;
            }

            var localData = _mLocalLobby.GetDataForUnityServices();

            var dataCurr = CurrentUnityLobby.Data;
            if (dataCurr == null)
            {
                dataCurr = new Dictionary<string, DataObject>();
            }

            foreach (var dataNew in localData)
            {
                if (dataCurr.ContainsKey(dataNew.Key))
                {
                    dataCurr[dataNew.Key] = dataNew.Value;
                }
                else
                {
                    dataCurr.Add(dataNew.Key, dataNew.Value);
                }
            }

            try
            {
                var result = await _mLobbyApiInterface.UpdateLobby(CurrentUnityLobby.Id, dataCurr, shouldLock: false);

                if (result != null)
                {
                    CurrentUnityLobby = result;
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    _mRateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        void DoLobbyHeartbeat(float dt)
        {
            _mHeartbeatTime += dt;
            if (_mHeartbeatTime > KHeartbeatPeriod)
            {
                _mHeartbeatTime -= KHeartbeatPeriod;
                try
                {
                    _mLobbyApiInterface.SendHeartbeatPing(CurrentUnityLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                    if (e.Reason != LobbyExceptionReason.LobbyNotFound && !_mLocalUser.IsHost)
                    {
                        PublishError(e);
                    }
                }
            }
        }

        void PublishError(LobbyServiceException e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})"; // Lobby error type, then HTTP error type.
            _mUnityServiceErrorMessagePub.Publish(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
        }
    }
}
