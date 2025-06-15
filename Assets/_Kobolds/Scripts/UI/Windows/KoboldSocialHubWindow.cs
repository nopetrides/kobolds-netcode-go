using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Kobold.UI.Components;

namespace Kobold.UI.Windows
{
    /// <summary>
    /// Social Hub window for joining multiplayer sessions
    /// </summary>
    public class KoboldSocialHubWindow : KoboldWindow
    {
        private KoboldButton _backButton;
        private TextField _playerNameField;
        private TextField _sessionNameField;
        private KoboldButton _joinButton;
        
        private string _defaultPlayerName = "Kobold";
        private string _defaultSessionName = "KoboldHub";
        
        public KoboldSocialHubWindow() : base("SocialHub")
        {
            BuildUI();
        }
        
        private void BuildUI()
        {
            // Back button
            _backButton = new KoboldButton("← Back");
            _backButton.AddToClassList("back-button");
            _backButton.AnimationDelay = 0.05f;
            _backButton.Clicked += OnBackClicked;
			ContentContainer.Add(_backButton);
            
            // Title
            var title = new Label("Join Social Hub");
            title.AddToClassList("kobold-title");
            ContentContainer.Add(title);
            
            var spacer = new VisualElement();
            spacer.AddToClassList("kobold-spacer-medium");
            ContentContainer.Add(spacer);
            
            // Player Name Section
            var playerNameLabel = new Label("Player Name");
            playerNameLabel.AddToClassList("kobold-label");
            ContentContainer.Add(playerNameLabel);
            
            _playerNameField = new TextField();
            _playerNameField.AddToClassList("kobold-input");
            _playerNameField.value = PlayerPrefs.GetString("PlayerName", _defaultPlayerName);
            ContentContainer.Add(_playerNameField);
            
            var spacer2 = new VisualElement();
            spacer2.AddToClassList("kobold-spacer-small");
            ContentContainer.Add(spacer2);
            
            // Session Name Section
            var sessionNameLabel = new Label("Session Name");
            sessionNameLabel.AddToClassList("kobold-label");
            ContentContainer.Add(sessionNameLabel);
            
            _sessionNameField = new TextField();
            _sessionNameField.AddToClassList("kobold-input");
            _sessionNameField.value = PlayerPrefs.GetString("LastSession", _defaultSessionName);
            ContentContainer.Add(_sessionNameField);
            
            var spacer3 = new VisualElement();
            spacer3.AddToClassList("kobold-spacer-large");
            ContentContainer.Add(spacer3);
            
            // Join Button
            _joinButton = new KoboldButton("Join Hub");
            _joinButton.AddToClassList("primary");
            _joinButton.AnimationDelay = 0.2f;
            _joinButton.Clicked += OnJoinClicked;
            ContentContainer.Add(_joinButton);
        }
        
        private void OnBackClicked()
        {
            KoboldWindowManager.Instance.NavigateBack();
        }
        
        private void OnJoinClicked()
        {
            var playerName = _playerNameField.value;
            var sessionName = _sessionNameField.value;
            
            // Validate input
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Debug.LogError("Player name cannot be empty!");
                // TODO: Show error UI
                return;
            }
            
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                Debug.LogError("Session name cannot be empty!");
                // TODO: Show error UI
                return;
            }
            
            // Save preferences
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetString("LastSession", sessionName);
            PlayerPrefs.Save();
            
            // Fire event to connect
            try
            {
                KoboldEventHandler.StartSocialHubPressed(playerName, sessionName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error starting social hub connection: {ex}");
            }
        }
        
        public override void Show()
        {
            base.Show();
            
            // Refresh values when showing
            _playerNameField.value = PlayerPrefs.GetString("PlayerName", _defaultPlayerName);
            _sessionNameField.value = PlayerPrefs.GetString("LastSession", _defaultSessionName);
        }
    }
}