using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class HomeScreenView : UIView
    {
        TextField _mPlayerNameField;
        TextField _mSessionNameField;
        Button _mStartButton;
        Button _mQuitButton;

        const int KAuthenticationMaxNameLength = 50;

        void Start()
        {
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
        }

        public override void Initialize(VisualElement viewRoot)
        {
            base.Initialize(viewRoot);
            _mPlayerNameField = MRoot.Q<TextField>("tf_player_name");
            _mSessionNameField = MRoot.Q<TextField>("tf_session_name");
            _mStartButton = MRoot.Q<Button>("bt_start");
            _mQuitButton = MRoot.Q<Button>("bt_quit");
            _mStartButton.SetEnabled(false);
        }

        protected override void RegisterEvents()
        {
            _mPlayerNameField.RegisterValueChangedCallback(evt => OnFieldChanged());
            _mSessionNameField.RegisterValueChangedCallback(evt => OnFieldChanged());
            _mStartButton.clicked += HandleStartButtonPressed;
            _mQuitButton.clicked += HandleQuitButtonPressed;
        }

        protected override void UnregisterEvents()
        {
            _mPlayerNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
            _mSessionNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
            _mStartButton.clicked -= HandleStartButtonPressed;
            _mQuitButton.clicked -= HandleQuitButtonPressed;
        }

        void OnFieldChanged()
        {
            _mPlayerNameField.value = SanitizePlayerName(_mPlayerNameField.value);
            string sessionName = _mSessionNameField.value;
            _mStartButton.SetEnabled(!string.IsNullOrEmpty(_mPlayerNameField.value) && !string.IsNullOrEmpty(sessionName));
        }

        void HandleStartButtonPressed()
        {
            string playerName = _mPlayerNameField.value;
            string sessionName = _mSessionNameField.value;
            _mStartButton.enabledSelf = false;
            GameplayEventHandler.StartButtonPressed(playerName, sessionName);
        }

        static string SanitizePlayerName(string dirtyString)
        {
            var output = Regex.Replace(dirtyString, @"\s", "");
            return output[..Math.Min(output.Length, KAuthenticationMaxNameLength)];
        }

        void HandleQuitButtonPressed()
        {
            GameplayEventHandler.QuitGamePressed();
        }

        void OnConnectToSessionCompleted(Task task, string sessionName )
        {
            if (!task.IsCompletedSuccessfully)
            {
                _mStartButton.enabledSelf = true;
            }
        }
    }
}
