using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Kobold.UI.Components;

namespace Kobold.UI.Windows
{
    /// <summary>
    /// Main menu window implementation using the component system
    /// </summary>
    public class KoboldMainMenuWindow : KoboldWindow
    {
        private KoboldButton _socialHubButton;
        private KoboldButton _quickMissionButton;
        private KoboldButton _settingsButton;
        private KoboldButton _quitButton;
        
        public KoboldMainMenuWindow() : base("MainMenu")
        {
            BuildUI();
        }
        
        private void BuildUI()
        {
            // Title
            var title = new Label("KOBOLDS");
            title.AddToClassList("kobold-title");
			ContentContainer.Add(title);
            
            // Spacer
            var spacer = new VisualElement();
            spacer.AddToClassList("kobold-spacer-large");
            ContentContainer.Add(spacer);
            
            // Social Hub Button
            _socialHubButton = new KoboldButton("Social Hub");
            _socialHubButton.AddToClassList("primary");
            _socialHubButton.AnimationDelay = 0.1f;
            _socialHubButton.Clicked += OnSocialHubClicked;
            ContentContainer.Add(_socialHubButton);
            
            // Quick Mission Button
            _quickMissionButton = new KoboldButton("Quick Mission");
            _quickMissionButton.AnimationDelay = 0.15f;
            _quickMissionButton.Clicked += OnQuickMissionClicked;
            ContentContainer.Add(_quickMissionButton);
            
            // Settings Button
            _settingsButton = new KoboldButton("Settings");
            _settingsButton.AnimationDelay = 0.2f;
            _settingsButton.Clicked += OnSettingsClicked;
            ContentContainer.Add(_settingsButton);
            
            // Quit Button
            _quitButton = new KoboldButton("Quit");
            _quitButton.AddToClassList("danger");
            _quitButton.AnimationDelay = 0.25f;
            _quitButton.Clicked += OnQuitClicked;
            ContentContainer.Add(_quitButton);
        }
        
        private void OnSocialHubClicked()
        {
            KoboldWindowManager.Instance.ShowWindow("SocialHub");
        }
        
        private void OnQuickMissionClicked()
        {
            // Get player name from preferences
            var playerName = PlayerPrefs.GetString("PlayerName", "Kobold");
            KoboldEventHandler.QuickMissionPressed(playerName, "QuickMatch");
        }
        
        private void OnSettingsClicked()
        {
            KoboldWindowManager.Instance.ShowWindow("Settings");
        }
        
        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}