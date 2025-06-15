using System;
using System.Collections.Generic;
using System.Linq;
using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Theming
{
	public class KoboldThemeManager : MonoBehaviour
	{
		// Static instance field (not lazy)
		private static KoboldThemeManager _instance;
		private static bool _applicationIsQuitting;

		[Header("Theme Configuration")]
		[SerializeField] private UITheme[] _availableThemes;

		[SerializeField] private UITheme _defaultTheme;
		[SerializeField] private string _savedThemeKey = "KoboldSelectedTheme";

		[Header("Runtime")]
		[SerializeField] private UITheme _currentTheme;

		// Thread safety
		private readonly object _documentLock = new();
		private readonly Dictionary<VisualElement, UITheme> _elementThemeOverrides = new();

		// Registered UI Documents
		private readonly List<UIDocument> _registeredDocuments = new();

		// Thread-safe instance accessor
		public static KoboldThemeManager Instance
		{
			get
			{
				if (!Application.isPlaying || _applicationIsQuitting) return _instance; // Return null during shutdown

				if (_instance == null)
				{
					_instance = FindFirstObjectByType<KoboldThemeManager>();

					if (_instance == null)
					{
						Debug.LogWarning("[KoboldThemeManager] No instance found in scene. Creating one...");
						var go = new GameObject("KoboldThemeManager");
						_instance = go.AddComponent<KoboldThemeManager>();
						DontDestroyOnLoad(go);
					}
				}

				return _instance;
			}
		}

		public UITheme CurrentTheme => _currentTheme;
		public IReadOnlyList<UITheme> AvailableThemes => _availableThemes;

		private void Awake()
		{
			// Check if we should persist
			if (!KoboldPersistentObjectManager.RegisterPersistentObject(this))
			{
				Destroy(gameObject);
				return;
			}

			_instance = this;

			// Validate configuration
			if (!ValidateConfiguration()) return;

			DontDestroyOnLoad(gameObject);
			LoadSavedTheme();
		}

		private void OnEnable()
		{
			// Subscribe to scene events
			KoboldEventHandler.OnSceneLoadCompleted += OnSceneLoaded;
		}

		private void OnDisable()
		{
			// Unsubscribe from scene events
			KoboldEventHandler.OnSceneLoadCompleted -= OnSceneLoaded;
		}

		private void OnDestroy()
		{
			// Clean up singleton reference
			if (_instance == this) _instance = null;

			// Mark as quitting if this is the main instance being destroyed
			if (_instance == this) _applicationIsQuitting = true;
		}

		private void OnApplicationQuit()
		{
			_applicationIsQuitting = true;
		}

		// Reset static state when domain reloads
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			_instance = null;
			OnThemeChanged = null;
			_applicationIsQuitting = false;
			Debug.Log("[KoboldThemeManager] Static state reset");
		}

		// Events
		public static event Action<UITheme> OnThemeChanged;

		private bool ValidateConfiguration()
		{
			if (_availableThemes == null || _availableThemes.Length == 0)
			{
				Debug.LogError("[KoboldThemeManager] No themes available! Please configure themes in the inspector.");
				return false;
			}

			// Remove null themes
			_availableThemes = _availableThemes.Where(t => t != null).ToArray();

			if (_availableThemes.Length == 0)
			{
				Debug.LogError("[KoboldThemeManager] All configured themes are null!");
				return false;
			}

			// Ensure default theme is valid
			if (_defaultTheme == null || !_availableThemes.Contains(_defaultTheme))
			{
				_defaultTheme = _availableThemes[0];
				Debug.LogWarning($"[KoboldThemeManager] Default theme was invalid. Using {_defaultTheme.themeName}");
			}

			return true;
		}

#region Scene Management

		private void OnSceneLoaded(string sceneName)
		{
			Debug.Log($"[KoboldThemeManager] Scene loaded: {sceneName}. Auto-registering UIDocuments...");

			// Auto-register all UIDocuments in the new scene
			var documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
			foreach (var doc in documents) RegisterUIDocument(doc);
		}

#endregion

#region Audio Integration

		public void PlayUISound(UISoundType soundType)
		{
			if (_currentTheme == null) return;

			var clip = soundType switch
			{
				UISoundType.Click => _currentTheme.uiClickSound,
				UISoundType.Hover => _currentTheme.uiHoverSound,
				UISoundType.Error => _currentTheme.uiErrorSound,
				_ => null
			};

			if (clip != null)
				// Play through your audio system
				// TODO: Integrate with KoboldAudioManager when available
				// KoboldAudioManager.Instance?.PlayUISound(clip);
				// Fallback: Play at point
				AudioSource.PlayClipAtPoint(clip, Camera.main?.transform.position ?? Vector3.zero, 1.0f);
		}

#endregion

#region Theme Management

		public void SetTheme(UITheme newTheme)
		{
			if (newTheme == null)
			{
				Debug.LogError("[KoboldThemeManager] Cannot set null theme!");
				return;
			}

			if (newTheme == _currentTheme) return;

			_currentTheme = newTheme;
			ApplyThemeToAllDocuments();
			SaveThemePreference();

			try
			{
				OnThemeChanged?.Invoke(_currentTheme);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldThemeManager] Error in theme change event: {ex}");
			}

			Debug.Log($"[KoboldThemeManager] Applied theme: {_currentTheme.themeName}");
		}

		public void SetTheme(string themeName)
		{
			if (string.IsNullOrEmpty(themeName))
			{
				Debug.LogError("[KoboldThemeManager] Theme name cannot be null or empty!");
				return;
			}

			var theme = _availableThemes.FirstOrDefault(t => t != null && t.themeName == themeName);
			if (theme != null)
				SetTheme(theme);
			else
				Debug.LogWarning($"[KoboldThemeManager] Theme '{themeName}' not found!");
		}

#endregion

#region Document Registration

		public void RegisterUIDocument(UIDocument document)
		{
			if (document == null) return;

			lock (_documentLock)
			{
				if (!_registeredDocuments.Contains(document))
				{
					_registeredDocuments.Add(document);
					ApplyThemeToDocument(document);
					Debug.Log($"[KoboldThemeManager] Registered UIDocument: {document.name}");
				}
			}
		}

		public void UnregisterUIDocument(UIDocument document)
		{
			if (document == null) return;

			lock (_documentLock)
			{
				_registeredDocuments.Remove(document);
			}
		}

		public void RegisterVisualElement(VisualElement element, UITheme overrideTheme = null)
		{
			if (element == null) return;

			lock (_documentLock)
			{
				if (overrideTheme != null)
				{
					_elementThemeOverrides[element] = overrideTheme;
					ApplyThemeToElement(element, overrideTheme);
				}
				else
				{
					_elementThemeOverrides.Remove(element);
					ApplyThemeToElement(element, _currentTheme);
				}
			}
		}

#endregion

#region Theme Application

		private void ApplyThemeToAllDocuments()
		{
			List<UIDocument> documentsCopy;
			lock (_documentLock)
			{
				documentsCopy = new List<UIDocument>(_registeredDocuments);
			}

			foreach (var document in documentsCopy)
				if (document != null)
					ApplyThemeToDocument(document);

			// Clean up null references
			lock (_documentLock)
			{
				_registeredDocuments.RemoveAll(d => d == null);
			}
		}

		private void ApplyThemeToDocument(UIDocument document)
		{
			if (document?.rootVisualElement == null || _currentTheme == null) return;

			try
			{
				var root = document.rootVisualElement;

				// Clear existing theme stylesheets
				root.styleSheets.Clear();

				// Apply main stylesheet
				if (_currentTheme.mainStyleSheet != null) root.styleSheets.Add(_currentTheme.mainStyleSheet);

				// Apply additional stylesheets
				if (_currentTheme.additionalStyleSheets != null)
					foreach (var styleSheet in _currentTheme.additionalStyleSheets)
						if (styleSheet != null)
							root.styleSheets.Add(styleSheet);

				// Apply CSS variables
				ApplyThemeToElement(root, _currentTheme);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldThemeManager] Error applying theme to document {document.name}: {ex}");
			}
		}

		private void ApplyThemeToElement(VisualElement element, UITheme theme)
		{
			if (element == null || theme == null) return;

			try
			{
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
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldThemeManager] Error applying theme to element: {ex}");
			}
		}

#endregion

#region Persistence

		private void LoadSavedTheme()
		{
			if (_availableThemes == null || _availableThemes.Length == 0)
			{
				Debug.LogError("[KoboldThemeManager] No themes available!");
				return;
			}

			var savedThemeName = PlayerPrefs.GetString(_savedThemeKey, "");

			if (!string.IsNullOrEmpty(savedThemeName))
			{
				var savedTheme = _availableThemes.FirstOrDefault(t => t != null && t.themeName == savedThemeName);
				if (savedTheme != null)
				{
					_currentTheme = savedTheme;
					Debug.Log($"[KoboldThemeManager] Loaded saved theme: {_currentTheme.themeName}");
					return;
				}
			}

			// Fall back to default theme
			_currentTheme = _defaultTheme != null ? _defaultTheme : _availableThemes[0];
			Debug.Log($"[KoboldThemeManager] Using default theme: {_currentTheme.themeName}");
		}

		private void SaveThemePreference()
		{
			if (_currentTheme != null)
			{
				PlayerPrefs.SetString(_savedThemeKey, _currentTheme.themeName);
				PlayerPrefs.Save();
			}
		}

#endregion

#region Editor Support

#if UNITY_EDITOR
		[ContextMenu("Log Theme Status")]
		private void LogThemeStatus()
		{
			Debug.Log(
				$"[KoboldThemeManager] Current Theme: {(_currentTheme != null ? _currentTheme.themeName : "None")}");
			Debug.Log($"[KoboldThemeManager] Available Themes: {_availableThemes?.Length ?? 0}");
			Debug.Log($"[KoboldThemeManager] Registered Documents: {_registeredDocuments.Count}");
		}

		[ContextMenu("Force Refresh All Documents")]
		private void ForceRefreshAllDocuments()
		{
			ApplyThemeToAllDocuments();
			Debug.Log("[KoboldThemeManager] Force refreshed all documents");
		}
#endif

#endregion
	}

	public enum UISoundType
	{
		Click,
		Hover,
		Error
	}
}
