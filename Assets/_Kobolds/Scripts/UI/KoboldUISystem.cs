using System;
using System.Collections;
using System.Collections.Generic;
using Kobold.UI.Components;
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
		
		void OnEnable()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		void OnDisable()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			UnloadAllMenus();
		}

		private void OnDestroy()
		{
			foreach (var presenter in _presenters.Values)
				presenter?.Cleanup();

			if (Instance == this)
				Instance = null;
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
			// Hide current
			if (CurrentMenu != KoboldMenu.None && _loadedMenus.TryGetValue(CurrentMenu, out var currentGo))
			{
				currentGo.SetActive(false);
				_presenters[CurrentMenu]?.OnHide();
			}

			if (menuType == KoboldMenu.None)
			{
				CurrentMenu = KoboldMenu.None;
				return;
			}

			// Load if needed
			if (!_loadedMenus.ContainsKey(menuType))
				LoadMenu(menuType);

			if (_loadedMenus.TryGetValue(menuType, out var newGo))
			{
				newGo.SetActive(true);
				CurrentMenu = menuType;

				// Animate if needed
				if (_config.autoAnimateWindows && _roots.TryGetValue(menuType, out var veRoot))
					AnimateWindow(veRoot, _menus[menuType]);

				_presenters[menuType]?.OnShow();
			}
			else
			{
				Debug.LogWarning($"[KoboldUISystem] Failed to show menu {menuType}");
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

			var root = document.rootVisualElement;
			if (root == null)
			{
				Debug.LogError($"[KoboldUISystem] Root visual element missing for {menuType}");
				Destroy(go);
				return;
			}

			// Ensure it's hidden initially
			go.SetActive(false);

			// Theme application
			KoboldThemeManager.Instance?.RegisterUIDocument(document);

			// Presenter hookup
			var presenter = def.CreatePresenter();
			presenter?.Initialize(root, _config);

			_loadedMenus[menuType] = go;
			_presenters[menuType] = presenter;
			_roots[menuType] = root;
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
			{
				try
				{
					presenter?.OnHide();
					presenter?.Cleanup();
				}
				catch (Exception e)
				{
					Debug.LogError($"[KoboldUISystem] Error during presenter cleanup: {e}");
				}
			}

			foreach (var go in _loadedMenus.Values)
			{
				if (go != null)
					Destroy(go);
			}

			_loadedMenus.Clear();
			_presenters.Clear();
			_roots.Clear();
			CurrentMenu = KoboldMenu.None;

			Debug.Log("[KoboldUISystem] All menus unloaded.");
		}

	}
}
