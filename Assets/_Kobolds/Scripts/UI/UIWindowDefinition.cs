using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Configuration
{
    /// <summary>
    /// Defines a UI window with its menu type, UXML and presenter binding
    /// </summary>
    [CreateAssetMenu(fileName = "UIWindowDefinition", menuName = "Kobold/UI/Window Definition")]
    public class UIWindowDefinition : ScriptableObject
    {
        [Header("Window Identity")]
        [Tooltip("Menu type for this window")]
        public KoboldMenu menuType = KoboldMenu.None;
        
        [Tooltip("Display name for this window")]
        public string windowName = "Window Name";
        
        [Header("Assets")]
        [Tooltip("UXML template for this window")]
        public VisualTreeAsset uxmlAsset;
        
        [Header("Behavior")]
        [Tooltip("Should this window be shown by default?")]
        public bool isDefaultWindow;
        
        [Header("Animation")]
        [Tooltip("Custom animation duration (0 = use default)")]
        public float animationDuration = 0f;
        
        [Tooltip("Animation type")]
        public WindowAnimationType animationType = WindowAnimationType.Fade;
		
		[Header("Prefab-Based Window (Optional)")]
		public GameObject windowPrefab;
        
        /// <summary>
        /// Creates a presenter instance based on the type
        /// </summary>
        public IUIPresenter CreatePresenter()
        {
            return menuType switch
            {
				KoboldMenu.MainMenu => new Presenters.MainMenuPresenter(),
				KoboldMenu.SocialHub => new Presenters.SocialHubPresenter(),
				KoboldMenu.Settings => new Presenters.SettingsPresenter(),
                // Add more presenter types as needed
                _ => null
            };
        }
    }
    
    public enum WindowAnimationType
    {
        None,
        Fade,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        Scale
    }
}