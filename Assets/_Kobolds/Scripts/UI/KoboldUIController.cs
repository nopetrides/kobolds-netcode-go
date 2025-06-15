using System;
using UnityEngine;
using UnityEngine.UIElements;
using Kobold.UI.Configuration;
using Kobold.UI.Theming;

namespace Kobold.UI
{
    /// <summary>
    /// Minimal UI controller that only manages window visibility
    /// </summary>
    [Obsolete]
    public class KoboldUIController : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private KoboldUIConfiguration _uiConfig;
        
        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _root;
        private VisualElement _currentWindow;
        
        public static KoboldUIController Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            _root = _uiDocument.rootVisualElement;
            _root.Clear();
            
            // Load all windows into the document
            LoadWindows();
            
            // Register with theme manager
            var themeManager = KoboldThemeManager.Instance;
            themeManager?.RegisterUIDocument(_uiDocument);
            
            // Show main menu by default
            ShowWindow("main-menu");
        }
        
        private void LoadWindows()
        {
            
        }
        
        public void ShowWindow(string windowName)
        {
            // Hide current window
            if (_currentWindow != null)
                _currentWindow.style.display = DisplayStyle.None;
            
            // Find and show new window
            _currentWindow = _root.Q<VisualElement>(windowName);
            if (_currentWindow != null)
                _currentWindow.style.display = DisplayStyle.Flex;
            else
                Debug.LogWarning($"Window '{windowName}' not found!");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
                
            var themeManager = KoboldThemeManager.Instance;
            if (themeManager != null && _uiDocument != null)
                themeManager.UnregisterUIDocument(_uiDocument);
        }
    }
}