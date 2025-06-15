#if UNITY_STANDALONE_OSX || UNITY_IOS
using System.Collections;
#endif
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.UI;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Services.Vivox;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class VivoxManager : MonoBehaviour
    {
        const int KAudibleDistance = 20;
        const int KConventionalDistance = 1;
        const float KAudioFadeByDistance = 1f;

        string _mTextChannelName;
        string _mVoiceChannelName;

#if UNITY_STANDALONE_OSX || UNITY_IOS
        bool m_MicPermissionChecked;

#endif

        internal static VivoxManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

#if UNITY_STANDALONE_OSX || UNITY_IOS
        IEnumerator RequestMicrophonePermissionsIOsMacOS()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            m_MicPermissionChecked = true;
        }
#endif

        internal async Task Initialize()
        {
            await VivoxService.Instance.InitializeAsync();
            BindGlobalEvents(true);
        }

        async void LoginVivox(Task t, string sessionName)
        {
#if UNITY_STANDALONE_OSX || UNITY_IOS
            // Vivox is not allowed to access the microphone on iOS and macOS without user permission.
            StartCoroutine(RequestMicrophonePermissionsIOsMacOS());
            while (m_MicPermissionChecked == false)
            {
                await Task.Yield();
            }
#endif
            _mTextChannelName = sessionName + "_text";
            _mVoiceChannelName = sessionName + "_voice";
            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                DisplayName = AuthenticationService.Instance.PlayerName,
                PlayerId = AuthenticationService.Instance.PlayerId
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        async void OnLoggedInVivox()
        {
            await JoinChannels();
        }

        async Task JoinChannels()
        {
            var positionalChannelProperties = new Channel3DProperties(KAudibleDistance, KConventionalDistance, KAudioFadeByDistance, AudioFadeModel.InverseByDistance);
            BindChannelEvents(true);
            await VivoxService.Instance.JoinPositionalChannelAsync(_mVoiceChannelName, ChatCapability.AudioOnly, positionalChannelProperties);
            await VivoxService.Instance.JoinGroupChannelAsync(_mTextChannelName, ChatCapability.TextOnly);
        }

        void OnParticipantLeftChannel(VivoxParticipant vivoxParticipant)
        {
            var channelOptions = new ChannelOptions();
            // UI only needs to react to VoiceChannel participants.
            if (vivoxParticipant.ChannelName != _mVoiceChannelName)
                return;

            GameplayEventHandler.ParticipantLeftVoiceChat(vivoxParticipant);
        }

        void OnParticipantAddedToChannel(VivoxParticipant vivoxParticipant)
        {
            // UI only needs to react to VoiceChannel participants.
            if (vivoxParticipant.ChannelName != _mVoiceChannelName)
                return;

            GameplayEventHandler.ParticipantJoinedVoiceChat(vivoxParticipant);
        }

        void OnChannelJoined(string channelName)
        {
            if (channelName == _mTextChannelName)
                GameplayEventHandler.SetTextChatReady(true, _mTextChannelName);
        }

        async void LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false, _mTextChannelName);
            await VivoxService.Instance.LogoutAsync();
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(_mTextChannelName, message);
        }

        void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            var senderName = UIUtils.ExtractPlayerNameFromAuthUserName(vivoxMessage.SenderDisplayName);
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        internal void SetPlayer3DPosition(GameObject avatar)
        {
            VivoxService.Instance.Set3DPosition(avatar, _mVoiceChannelName, false);
        }

        void BindGlobalEvents(bool doBind)
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= LoginVivox;
            VivoxService.Instance.LoggedIn -= OnLoggedInVivox;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            GameplayEventHandler.OnExitedSession -= LogoutVivox;

            if (doBind)
            {
                GameplayEventHandler.OnConnectToSessionCompleted += LoginVivox;
                VivoxService.Instance.LoggedIn += OnLoggedInVivox;
                VivoxService.Instance.ChannelJoined += OnChannelJoined;
                GameplayEventHandler.OnExitedSession += LogoutVivox;
            }
        }

        void BindChannelEvents(bool doBind)
        {
            VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantLeftChannel;
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;

            if (doBind)
            {
                VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAddedToChannel;
                VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantLeftChannel;
                GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
                VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
            }
        }

        void OnDestroy()
        {
            BindGlobalEvents(false);
            BindChannelEvents(false);
        }
    }
}
