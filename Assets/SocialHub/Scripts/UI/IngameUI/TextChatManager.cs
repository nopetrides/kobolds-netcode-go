using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Multiplayer.Samples.SocialHub.GameManagement.GameplayEventHandler;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class TextChatManager : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_Asset;

        // Serializable for Bindings.
        [SerializeField, HideInInspector]
        List<ChatMessage> m_Messages = new();

        ListView _mMessageView;
        TextField _mMessageInputField;
        Button _mSendButton;
        VisualElement _mRoot;
        VisualElement _mTextChatView;

        const int KFocusDelay = 10;
        bool _mIsChatActive;

        void OnEnable()
        {
            _mRoot = m_UIDocument.rootVisualElement.Q<VisualElement>("textchat-container");
            m_Asset.CloneTree(_mRoot);

            _mRoot.Q<Button>("visibility-button").clicked += ToggleChat;
            _mTextChatView = _mRoot.Q<VisualElement>("text-chat");

            _mSendButton = _mRoot.Q<Button>("submit");
            _mSendButton.clicked += SendMessage;

            _mMessageInputField = _mRoot.Q<TextField>("input-text");

#if !UNITY_IOS && !UNITY_ANDROID
            _mMessageInputField.RegisterCallback<FocusInEvent>(OnTextfieldFocusIn);
            _mMessageInputField.RegisterCallback<FocusOutEvent>(OnTextfieldFocusOut);
            _mMessageInputField.RegisterCallback<KeyDownEvent>(OnTextEnter, TrickleDown.TrickleDown);
#endif

            _mMessageView = _mRoot.Q<ListView>("message-list");
            _mMessageView.dataSource = this;
            _mMessageView.SetBinding("itemsSource", new DataBinding
            {
                dataSourcePath = new PropertyPath("m_Messages"),
                bindingMode = BindingMode.TwoWay
            });

            SetViewFocusable(_mIsChatActive);
            _mTextChatView.SetEnabled(false);
            BindSessionEvents(true);

            m_Messages.Clear();
            m_Messages.Add(new ChatMessage("Sample Devs", "Hey, we hope you enjoy our sample :)"));
        }

        void OnTextEnter(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                SendMessage();
                _mMessageInputField.schedule.Execute(() => _mMessageInputField.Focus()).ExecuteLater(KFocusDelay);
            }
        }

        void OnTextfieldFocusIn(FocusInEvent _)
        {
            InputSystemManager.Instance.EnableUIInputs();
        }

        void OnTextfieldFocusOut(FocusOutEvent _)
        {
            InputSystemManager.Instance.EnableGameplayInputs();
        }

#if UNITY_IOS || UNITY_ANDROID
        void Update()
        {
            if (m_MessageInputField is { touchScreenKeyboard: { status: TouchScreenKeyboard.Status.Done } })
            {
                SendMessage();
            }
        }
#endif

        void OnDisable()
        {
            _mSendButton.clicked -= SendMessage;
            _mMessageInputField.UnregisterCallback<FocusInEvent>(OnTextfieldFocusIn);
            _mMessageInputField.UnregisterCallback<FocusOutEvent>(OnTextfieldFocusOut);
            _mMessageInputField.UnregisterCallback<KeyDownEvent>(OnTextEnter, TrickleDown.TrickleDown);
            BindSessionEvents(false);
        }

        void ToggleChat()
        {
            _mIsChatActive = !_mIsChatActive;
            SetViewFocusable(_mIsChatActive);

            if (_mIsChatActive)
            {
                _mTextChatView.AddToClassList("text-chat--visible");
                return;
            }

            _mTextChatView.RemoveFromClassList("text-chat--visible");
        }

        void SetViewFocusable(bool focusable)
        {
            _mTextChatView.focusable = _mIsChatActive;
            _mMessageInputField.focusable = focusable;
            _mSendButton.focusable = focusable;
            _mMessageView.focusable = focusable;
        }

        void SendMessage()
        {
            if (!string.IsNullOrEmpty(_mMessageInputField.text))
            {
                SendTextMessage(_mMessageInputField.value);
                _mMessageInputField.value = "";
            }
        }

        void BindSessionEvents(bool doBind)
        {
            if (doBind)
            {
                OnChatIsReady += OnOnChatIsReady;
                OnTextMessageReceived -= OnChannelMessageReceived;
                OnTextMessageReceived += OnChannelMessageReceived;
            }
            else
            {
                OnChatIsReady -= OnOnChatIsReady;
                OnTextMessageReceived -= OnChannelMessageReceived;
            }
        }

        void OnOnChatIsReady(bool isReady, string channelName)
        {
            _mTextChatView.SetEnabled(isReady);
        }

        void OnChannelMessageReceived(string sender, string message, bool fromSelf)
        {
            m_Messages.Add(fromSelf ? new ChatMessage("me", message) : new ChatMessage(sender, message));
        }
    }

    [Serializable]
    class ChatMessage
    {
        public string Name;
        public string Message;

        public ChatMessage(string name, string message)
        {
            Name = name;
            Message = message;
        }
    }
}
