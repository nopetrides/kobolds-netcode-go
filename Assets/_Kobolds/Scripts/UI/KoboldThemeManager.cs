using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Kobold.GameManagement;

namespace Kobold.UI.Theming
{
    public class KoboldThemeManager : MonoBehaviour
    {
        private static KoboldThemeManager _instance;
        public static KoboldThemeManager Instance => _instance;
        
        [Header("Theme Configuration")]
        [SerializeField] private UITheme[] availableThemes;
        [SerializeField] private UITheme defaultTheme;
        [SerializeField] private string savedThemeKey = "KoboldSelectedTheme";
        
        [Header("Runtime")]
        [SerializeField] private UITheme currentTheme;
        
        // Events
        public static event Action<UITheme> OnThemeChanged;
        
        // Registered UI Documents
        private readonly List<UIDocument> _registeredDocuments = new List<UIDocument>();
        private readonly Dictionary<VisualElement, UITheme> _elementThemeOverrides = new Dictionary<VisualElement, UITheme>();
        
        public UITheme CurrentTheme => currentTheme;
        public IReadOnlyList<UITheme> AvailableThemes => availableThemes;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadSavedTheme();
        }
        
        void Start()
        {
            // Subscribe to scene events to handle UI document registration
            KoboldEventHandler.OnSceneLoadCompleted += OnSceneLoaded;
        }
        
        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            KoboldEventHandler.OnSceneLoadCompleted -= OnSceneLoaded;
        }
        
        #region Theme Management
        
        public void SetTheme(UITheme newTheme)
        {
            if (newTheme == null || newTheme == currentTheme) return;
            
            currentTheme = newTheme;
            ApplyThemeToAllDocuments();
            SaveThemePreference();
            
            OnThemeChanged?.Invoke(currentTheme);
            Debug.Log($"[ThemeManager] Applied theme: {currentTheme.themeName}");
        }
        
        public void SetTheme(string themeName)
        {
            var theme = availableThemes.FirstOrDefault(t => t.themeName == themeName);
            if (theme != null)
            {
                SetTheme(theme);
            }
            else
            {
                Debug.LogWarning($"[ThemeManager] Theme '{themeName}' not found!");
            }
        }
        
        #endregion
        
        #region Document Registration
        
        public void RegisterUIDocument(UIDocument document)
        {
            if (!_registeredDocuments.Contains(document))
            {
                _registeredDocuments.Add(document);
                ApplyThemeToDocument(document);
            }
        }
        
        public void UnregisterUIDocument(UIDocument document)
        {
            _registeredDocuments.Remove(document);
        }
        
        public void RegisterVisualElement(VisualElement element, UITheme overrideTheme = null)
        {
            if (overrideTheme != null)
            {
                _elementThemeOverrides[element] = overrideTheme;
                ApplyThemeToElement(element, overrideTheme);
            }
            else
            {
                ApplyThemeToElement(element, currentTheme);
            }
        }
        
        #endregion
        
        #region Theme Application
        
        private void ApplyThemeToAllDocuments()
        {
            foreach (var document in _registeredDocuments)
            {
                if (document != null)
                {
                    ApplyThemeToDocument(document);
                }
            }
            
            // Clean up null references
            _registeredDocuments.RemoveAll(d => d == null);
        }
        
        private void ApplyThemeToDocument(UIDocument document)
        {
            if (document?.rootVisualElement == null || currentTheme == null) return;
            
            var root = document.rootVisualElement;
            
            // Clear existing theme stylesheets
            root.styleSheets.Clear();
            
            // Apply main stylesheet
            if (currentTheme.mainStyleSheet != null)
            {
                root.styleSheets.Add(currentTheme.mainStyleSheet);
            }
            
            // Apply additional stylesheets
            if (currentTheme.additionalStyleSheets != null)
            {
                foreach (var styleSheet in currentTheme.additionalStyleSheets)
                {
                    if (styleSheet != null)
                    {
                        root.styleSheets.Add(styleSheet);
                    }
                }
            }
            
            // Apply CSS variables
            ApplyThemeToElement(root, currentTheme);
        }
        
		private void ApplyThemeToElement(VisualElement element, UITheme theme)
		{
			if (element == null || theme == null) return;
    
			// Apply inline styles directly
			element.style.color = new StyleColor(theme.textColor);
			element.style.backgroundColor = new StyleColor(theme.backgroundColor);
    
			// Apply theme class (this is where USS will pick up the theme)
			element.RemoveFromClassList("theme-default");
			element.RemoveFromClassList("theme-dark");
			element.RemoveFromClassList("theme-light");
			element.AddToClassList($"theme-{theme.themeName.ToLower()}");
    
			// Store theme data as userData for USS to reference
			element.userData = theme;
		}
        
        #endregion
        
        #region Persistence
        
        private void LoadSavedTheme()
        {
            if (availableThemes == null || availableThemes.Length == 0)
            {
                Debug.LogError("[ThemeManager] No themes available!");
                return;
            }
            
            string savedThemeName = PlayerPrefs.GetString(savedThemeKey, "");
            
            if (!string.IsNullOrEmpty(savedThemeName))
            {
                var savedTheme = availableThemes.FirstOrDefault(t => t.themeName == savedThemeName);
                if (savedTheme != null)
                {
                    currentTheme = savedTheme;
                    return;
                }
            }
            
            // Fall back to default theme
            currentTheme = defaultTheme != null ? defaultTheme : availableThemes[0];
        }
        
        private void SaveThemePreference()
        {
            if (currentTheme != null)
            {
                PlayerPrefs.SetString(savedThemeKey, currentTheme.themeName);
                PlayerPrefs.Save();
            }
        }
        
        #endregion
        
        #region Scene Management
        
        private void OnSceneLoaded(string sceneName)
        {
            // Auto-register all UIDocuments in the new scene
            var documents = FindObjectsOfType<UIDocument>();
            foreach (var doc in documents)
            {
                RegisterUIDocument(doc);
            }
        }
        
        #endregion
        
        #region Audio Integration
        
        public void PlayUISound(UISoundType soundType)
        {
            if (currentTheme == null) return;
            
            AudioClip clip = soundType switch
            {
                UISoundType.Click => currentTheme.uiClickSound,
                UISoundType.Hover => currentTheme.uiHoverSound,
                UISoundType.Error => currentTheme.uiErrorSound,
                _ => null
            };
            
            if (clip != null)
            {
                // Play through your audio system
                // AudioManager.Instance.PlayUISound(clip);
            }
        }
        
        #endregion
    }
    
    public enum UISoundType
    {
        Click,
        Hover,
        Error
    }
}