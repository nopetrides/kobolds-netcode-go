using System;
using UnityEngine;
using UnityEngine.UIElements;
using Kobold.GameManagement;
using Kobold.UI.Theming;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kobold.UI.Views
{
    public class KoboldHomeScreenView : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument _document;
        
        [Header("Configuration")]
        [SerializeField] private string _defaultPlayerName = "Kobold";
        [SerializeField] private string _defaultSessionName = "KoboldHub";
        
        private VisualElement _root;
        private TextField _playerNameField;
        private TextField _sessionNameField;
        private Button _socialHubButton;
        private Button _quickMissionButton;
        private Button _settingsButton;
        private Button _quitButton;
        
        // Panels
        private VisualElement _mainPanel;
        private VisualElement _socialHubPanel;
        private VisualElement _settingsPanel;
        
        void OnEnable()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();
                
            _root = _document.rootVisualElement;
            
            SetupMainMenu();
            ShowMainPanel();
        }
        
        void SetupMainMenu()
        {
            // Get references
            _mainPanel = _root.Q<VisualElement>("main-panel");
            _socialHubPanel = _root.Q<VisualElement>("social-hub-panel");
            _settingsPanel = _root.Q<VisualElement>("settings-panel");
            
            // Main menu buttons
            _socialHubButton = _root.Q<Button>("social-hub-button");
            _quickMissionButton = _root.Q<Button>("quick-mission-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _quitButton = _root.Q<Button>("quit-button");
            
            // Social hub panel elements
            _playerNameField = _root.Q<TextField>("player-name-field");
            _sessionNameField = _root.Q<TextField>("session-name-field");
            
            // Set default values
            if (_playerNameField != null)
                _playerNameField.value = PlayerPrefs.GetString("PlayerName", _defaultPlayerName);
            if (_sessionNameField != null)
                _sessionNameField.value = PlayerPrefs.GetString("LastSession", _defaultSessionName);
            
            // Register callbacks
            RegisterCallbacks();
        }
        
        void RegisterCallbacks()
        {
            _socialHubButton?.RegisterCallback<ClickEvent>(evt => ShowSocialHubPanel());
            _quickMissionButton?.RegisterCallback<ClickEvent>(evt => OpenQuickMission());
            _settingsButton?.RegisterCallback<ClickEvent>(evt => ShowSettingsPanel());
            _quitButton?.RegisterCallback<ClickEvent>(evt => QuitGame());
            
            // Social hub panel
            var joinHubButton = _root.Q<Button>("join-hub-button");
            joinHubButton?.RegisterCallback<ClickEvent>(evt => JoinSocialHub());
            
            var backFromHubButton = _root.Q<Button>("back-from-hub-button");
            backFromHubButton?.RegisterCallback<ClickEvent>(evt => ShowMainPanel());
            
            // Settings panel
            var backFromSettingsButton = _root.Q<Button>("back-from-settings-button");
            backFromSettingsButton?.RegisterCallback<ClickEvent>(evt => ShowMainPanel());
        }
        
        void ShowMainPanel()
        {
            _mainPanel?.RemoveFromClassList("hidden");
            _socialHubPanel?.AddToClassList("hidden");
            _settingsPanel?.AddToClassList("hidden");
        }
        
        void ShowSocialHubPanel()
        {
            _mainPanel?.AddToClassList("hidden");
            _socialHubPanel?.RemoveFromClassList("hidden");
            _settingsPanel?.AddToClassList("hidden");
            
            // Play UI sound
            KoboldThemeManager.Instance?.PlayUISound(UISoundType.Click);
        }
        
        void ShowSettingsPanel()
        {
            _mainPanel?.AddToClassList("hidden");
            _socialHubPanel?.AddToClassList("hidden");
            _settingsPanel?.RemoveFromClassList("hidden");
            
            KoboldThemeManager.Instance?.PlayUISound(UISoundType.Click);
        }
        
        void JoinSocialHub()
        {
            var playerName = _playerNameField?.value ?? _defaultPlayerName;
            var sessionName = _sessionNameField?.value ?? _defaultSessionName;
            
            // Validate input
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Debug.LogError("Player name cannot be empty!");
                KoboldThemeManager.Instance?.PlayUISound(UISoundType.Error);
                return;
            }
            
            // Save preferences
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetString("LastSession", sessionName);
            
            // Disable buttons while connecting
            SetButtonsEnabled(false);
            
            // Fire event to connect
            KoboldEventHandler.StartSocialHubPressed(playerName, sessionName);
            KoboldThemeManager.Instance?.PlayUISound(UISoundType.Click);
        }
        
        void OpenQuickMission()
        {
            // For now, just load a default player name
            var playerName = PlayerPrefs.GetString("PlayerName", _defaultPlayerName);
            
            // This would open mission browser or quick join
            KoboldEventHandler.QuickMissionPressed(playerName, "QuickMatch");
            KoboldThemeManager.Instance?.PlayUISound(UISoundType.Click);
        }
        
        void QuitGame()
        {
            KoboldThemeManager.Instance?.PlayUISound(UISoundType.Click);
            
            #if UNITY_EDITOR
                EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        void SetButtonsEnabled(bool enabled)
        {
            _socialHubButton?.SetEnabled(enabled);
            _quickMissionButton?.SetEnabled(enabled);
            _settingsButton?.SetEnabled(enabled);
            _quitButton?.SetEnabled(enabled);
        }
        
        void OnDisable()
        {
            // Unregister callbacks if needed
        }
    }
}