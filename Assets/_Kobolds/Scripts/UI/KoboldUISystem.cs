using System;
using System.Collections;
using System.Collections.Generic;
using Kobold.UI.Configuration;
using Kobold.UI.Theming;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Kobold.UI
{
	public class KoboldUISystem : MonoBehaviour
	{
		[Header("Configuration")]
		[SerializeField] private KoboldUIConfiguration _config;

		[Header("Window Definitions")]
		[SerializeField] private List<UIWindowDefinition> _windowDefinitions = new();

		[Header("Scene Hierarchy Target (Optional)")]
		[SerializeField] private Transform _menuContainer;

		private readonly Dictionary<KoboldMenu, GameObject> _loadedMenus = new();
		private readonly Dictionary<KoboldMenu, IUIPresenter> _presenters = new();
		private readonly Dictionary<KoboldMenu, VisualElement> _roots = new();

		private Dictionary<KoboldMenu, UIWindowDefinition> _menus;

		public static KoboldUISystem Instance { get; private set; }
		public KoboldMenu CurrentMenu { get; private set; } = KoboldMenu.None;

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);

			if (_config == null)
			{
				Debug.LogError($"[{nameof(KoboldUISystem)}] No UI Configuration assigned!");
				enabled = false;
				return;
			}

			BuildMenuDictionary();
		}

		private void Start()
		{
			var defaultMenu = GetDefaultMenu();
			if (defaultMenu != KoboldMenu.None)
				ShowMenu(defaultMenu);
		}

		private void OnEnable()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnDestroy()
		{
			foreach (var presenter in _presenters.Values)
				presenter?.Cleanup();

			if (Instance == this)
				Instance = null;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			UnloadAllMenus();
		}

		private void BuildMenuDictionary()
		{
			_menus = new Dictionary<KoboldMenu, UIWindowDefinition>();

			foreach (var def in _windowDefinitions)
			{
				if (def == null || def.menuType == KoboldMenu.None)
					continue;

				if (_menus.ContainsKey(def.menuType))
				{
					Debug.LogWarning($"Duplicate menu type {def.menuType} found. Skipping.");
					continue;
				}

				_menus[def.menuType] = def;
			}
		}

		public void ShowMenu(KoboldMenu menuType)
		{
			Debug.Log($"[KoboldUISystem] Starting ShowMenu menu {menuType}");

			// Store the previous menu
			var previousMenu = CurrentMenu;

			// Update current menu
			CurrentMenu = menuType;

			// If going to None, hide current and return
			if (menuType == KoboldMenu.None)
			{
				if (previousMenu != KoboldMenu.None && _loadedMenus.TryGetValue(previousMenu, out var currentGo))
				{
					currentGo.SetActive(false);
					if (_presenters.TryGetValue(previousMenu, out var presenter)) presenter?.OnHide();
				}

				return;
			}

			// Load if needed
			if (!_loadedMenus.ContainsKey(menuType))
			{
				Debug.Log($"[KoboldUISystem] Menu {menuType} not loaded yet, loading...");
				LoadMenu(menuType);
				// Don't hide the current menu yet - wait until new one is ready
				return;
			}

			// Menu is loaded, do the transition
			PerformMenuTransition(previousMenu, menuType);
		}

		private void PerformMenuTransition(KoboldMenu fromMenu, KoboldMenu toMenu)
		{
			Debug.Log($"[KoboldUISystem] Performing transition from {fromMenu} to {toMenu}");

			// Hide previous menu
			if (fromMenu != KoboldMenu.None && _loadedMenus.TryGetValue(fromMenu, out var previousGo))
			{
				previousGo.SetActive(false);
				if (_presenters.TryGetValue(fromMenu, out var previousPresenter)) previousPresenter?.OnHide();
			}

			// Show new menu
			if (_loadedMenus.TryGetValue(toMenu, out var newGo))
			{
				newGo.SetActive(true);

				// Animate if needed
				if (_config.autoAnimateWindows && _roots.TryGetValue(toMenu, out var veRoot))
					AnimateWindow(veRoot, _menus[toMenu]);

				// Call OnShow after the menu is active
				if (_presenters.TryGetValue(toMenu, out var presenter)) presenter?.OnShow();
			}
			else
			{
				Debug.LogWarning($"[KoboldUISystem] Failed to show menu {toMenu} - not in loaded menus");
			}
		}

		private void LoadMenu(KoboldMenu menuType)
		{
			if (!_menus.TryGetValue(menuType, out var def) || def.uxmlAsset == null)
			{
				Debug.LogError($"[KoboldUISystem] Missing UXML for {menuType}");
				return;
			}

			var go = new GameObject($"{menuType}Menu");
			if (_menuContainer != null) go.transform.SetParent(_menuContainer, false);

			var document = go.AddComponent<UIDocument>();
			document.visualTreeAsset = def.uxmlAsset;

			if (_config.defaultPanelSettings != null)
				document.panelSettings = _config.defaultPanelSettings;

			// Activate to ensure root is generated
			go.SetActive(true);

			// Wait for next frame or for panel attachment
			StartCoroutine(InitializeMenuWhenReady(menuType, def, go, document));
		}

		private IEnumerator InitializeMenuWhenReady(KoboldMenu menuType, UIWindowDefinition def, GameObject go, UIDocument document)
		{
			// Wait a frame for UIDocument to initialize
			yield return null;

			var root = document.rootVisualElement;
			if (root == null)
			{
				Debug.LogError($"[KoboldUISystem] Root visual element missing for {menuType}");
				Destroy(go);
				yield break;
			}

			// Wait for panel attachment
			int waitFrames = 0;
			while (root.panel == null && waitFrames < 60) // Max 1 second wait
			{
				Debug.Log($"[KoboldUISystem] Waiting for panel attachment for {menuType}... (frame {waitFrames})");
				yield return null;
				waitFrames++;
			}

			if (root.panel == null)
			{
				Debug.LogError($"[KoboldUISystem] Panel never attached for {menuType}!");
				Destroy(go);
				yield break;
			}

			Debug.Log($"[KoboldUISystem] Panel attached for {menuType}!");

			// Theme application
			KoboldThemeManager.Instance?.RegisterUIDocument(document);

			// Find the actual content root
			VisualElement contentRoot = root;
			if (root.childCount == 1)
			{
				contentRoot = root[0];
				Debug.Log($"[KoboldUISystem] Using child as content root: {contentRoot.name}");
			}

			// Presenter hookup
			var presenter = def.CreatePresenter();
			presenter?.Initialize(contentRoot, _config);

			_loadedMenus[menuType] = go;
			_presenters[menuType] = presenter;
			_roots[menuType] = root;

			// Start hidden
			go.SetActive(false);

			// Get the previous menu before we do the transition
			var previousMenu = CurrentMenu == menuType ? KoboldMenu.None : CurrentMenu;

			// If this menu is supposed to be shown, do the transition now
			if (CurrentMenu == menuType)
			{
				PerformMenuTransition(previousMenu, menuType);
			}
		}

		public void HideCurrentMenu()
		{
			ShowMenu(KoboldMenu.None);
		}

		private KoboldMenu GetDefaultMenu()
		{
			foreach (var kvp in _menus)
				if (kvp.Value.isDefaultWindow)
					return kvp.Key;

			return _menus.ContainsKey(KoboldMenu.MainMenu) ? KoboldMenu.MainMenu : KoboldMenu.None;
		}

		private void AnimateWindow(VisualElement window, UIWindowDefinition def)
		{
			var duration = def.animationDuration > 0 ? def.animationDuration : _config.defaultAnimationDuration;

			window.AddToClassList($"animate-{def.animationType.ToString().ToLower()}");
			window.AddToClassList("animating");

			switch (def.animationType)
			{
				case WindowAnimationType.Fade:
					window.style.opacity = 0f;
					StartCoroutine(AnimateValue(0f, 1f, duration, v => window.style.opacity = v));
					break;

				case WindowAnimationType.Scale:
					window.style.scale = new Scale(Vector2.zero);
					StartCoroutine(
						AnimateValue(0f, 1f, duration, v => window.style.scale = new Scale(Vector2.one * v)));
					break;

				case WindowAnimationType.SlideLeft:
					window.style.translate = new Translate(Length.Percent(-100), 0);
					StartCoroutine(
						AnimateValue(
							-100f, 0f, duration, v => window.style.translate = new Translate(Length.Percent(v), 0)));
					break;

				case WindowAnimationType.SlideRight:
					window.style.translate = new Translate(Length.Percent(100), 0);
					StartCoroutine(
						AnimateValue(
							100f, 0f, duration, v => window.style.translate = new Translate(Length.Percent(v), 0)));
					break;

				case WindowAnimationType.SlideUp:
					window.style.translate = new Translate(0, Length.Percent(100));
					StartCoroutine(
						AnimateValue(
							100f, 0f, duration, v => window.style.translate = new Translate(0, Length.Percent(v))));
					break;

				case WindowAnimationType.SlideDown:
					window.style.translate = new Translate(0, Length.Percent(-100));
					StartCoroutine(
						AnimateValue(
							-100f, 0f, duration, v => window.style.translate = new Translate(0, Length.Percent(v))));
					break;
			}

			StartCoroutine(RemoveAnimationClass(window, duration));
		}

		private IEnumerator AnimateValue(float from, float to, float duration, Action<float> apply)
		{
			var elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				var t = Mathf.Clamp01(elapsed / duration);
				t = EaseOutCubic(t);
				apply(Mathf.Lerp(from, to, t));
				yield return null;
			}

			apply(to);
		}

		private IEnumerator RemoveAnimationClass(VisualElement el, float delay)
		{
			yield return new WaitForSeconds(delay);
			el.RemoveFromClassList("animating");
		}

		private float EaseOutCubic(float t)
		{
			return 1f - Mathf.Pow(1f - t, 3);
		}

		public void PlayUISound(UISoundType type)
		{
			if (_config.enableUISounds)
				KoboldThemeManager.Instance?.PlayUISound(type);
		}

		public KoboldUIConfiguration GetConfiguration()
		{
			return _config;
		}

		public void UnloadAllMenus()
		{
			foreach (var presenter in _presenters.Values)
				try
				{
					presenter?.OnHide();
					presenter?.Cleanup();
				}
				catch (Exception e)
				{
					Debug.LogError($"[KoboldUISystem] Error during presenter cleanup: {e}");
				}

			foreach (var go in _loadedMenus.Values)
				if (go != null)
					Destroy(go);

			_loadedMenus.Clear();
			_presenters.Clear();
			_roots.Clear();
			CurrentMenu = KoboldMenu.None;

			Debug.Log("[KoboldUISystem] All menus unloaded.");
		}
	}
}
