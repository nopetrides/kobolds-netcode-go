﻿using System;
using System.Collections;
using Kobold.Services;
using Kobold.UI.Theming;
using Kobold.Vivox;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kobold.GameManagement
{
	/// <summary>
	///     Manages the boot sequence and ensures all systems are initialized in the correct order
	/// </summary>
	public class KoboldBootInitializer : MonoBehaviour
	{
		private static bool _hasBootedThisSession;

		[Header("Boot Configuration")]
		[SerializeField] private float _minimumLoadTime = 1.5f;

		[SerializeField] private string _mainMenuSceneName = "MainMenu";

		[Header("Required Systems")]
		[SerializeField] private bool _requireThemeManager = true;

		[SerializeField] private bool _requireServicesHelper = true;
		[SerializeField] private bool _requireInputManager = true;
		[SerializeField] private bool _requireVivoxManager = true;

		private float _bootStartTime;
		private bool _isBooting;

		private void Awake()
		{
			// Check if we've already booted this session
			if (_hasBootedThisSession)
			{
				Debug.LogWarning("[KoboldBootInitializer] Already booted this session. Skipping...");
				Destroy(gameObject);
				return;
			}

			// Ensure we only have one boot initializer
			if (FindObjectsByType<KoboldBootInitializer>(FindObjectsSortMode.None).Length > 1)
			{
				Debug.LogError("[KoboldBootInitializer] Multiple boot initializers found! Only one should exist.");
				Destroy(gameObject);
				return;
			}

			_bootStartTime = Time.time;
		}

		private void Start()
		{
			StartBootSequence();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			_hasBootedThisSession = false;
			Debug.Log("[KoboldBootInitializer] Static state reset");
		}

		private void StartBootSequence()
		{
			if (_isBooting)
			{
				Debug.LogWarning("[KoboldBootInitializer] Boot sequence already in progress");
				return;
			}

			_isBooting = true;
			_hasBootedThisSession = true;
			StartCoroutine(BootSequenceCoroutine());
		}

		private IEnumerator BootSequenceCoroutine()
		{
			Debug.Log("[KoboldBootInitializer] Starting boot sequence...");

			// Clean up any leftover DontDestroyOnLoad objects from previous sessions
			CleanupPreviousSessionObjects();

			// Step 1: Initialize Theme Manager
			if (_requireThemeManager) yield return InitializeThemeManager();

			// Step 2: Verify other required systems are present
			if (!VerifyRequiredSystems())
			{
				Debug.LogError("[KoboldBootInitializer] Required systems missing. Boot sequence failed.");
				yield break;
			}

			// Step 3: Wait for Services Helper to initialize
			if (_requireServicesHelper) yield return WaitForServicesHelper();

			// Step 4: Ensure minimum load time for smooth transition
			var elapsedTime = Time.time - _bootStartTime;
			if (elapsedTime < _minimumLoadTime) yield return new WaitForSeconds(_minimumLoadTime - elapsedTime);

			// Step 5: Load main menu
			Debug.Log("[KoboldBootInitializer] Boot sequence complete. Loading main menu...");
			LoadMainMenu();
		}

		private void CleanupPreviousSessionObjects()
		{
			// Find all DontDestroyOnLoad objects
			var ddolRoot = GetDontDestroyOnLoadRoot();
			if (ddolRoot != null)
			{
				// Clean up duplicate service objects
				CleanupDuplicateObjects<KoboldServicesHelper>(ddolRoot);
				CleanupDuplicateObjects<KoboldThemeManager>(ddolRoot);
				CleanupDuplicateObjects<KoboldInputSystemManager>(ddolRoot);
				CleanupDuplicateObjects<KoboldVivoxManager>(ddolRoot);
			}
		}

		private GameObject GetDontDestroyOnLoadRoot()
		{
			var temp = new GameObject();
			DontDestroyOnLoad(temp);
			var ddolRoot = temp.scene.GetRootGameObjects();
			Destroy(temp);
			return ddolRoot.Length > 0 ? ddolRoot[0] : null;
		}

		private void CleanupDuplicateObjects<T>(GameObject ddolRoot) where T : MonoBehaviour
		{
			var objects = FindObjectsByType<T>(FindObjectsSortMode.None);
			if (objects.Length > 1)
			{
				Debug.LogWarning(
					$"[KoboldBootInitializer] Found {objects.Length} instances of {typeof(T).Name}. Cleaning up duplicates...");

				// Keep the newest one (in current scene if possible)
				T keepObject = null;
				foreach (var obj in objects)
					if (obj.gameObject.scene == gameObject.scene)
					{
						keepObject = obj;
						break;
					}

				if (keepObject == null) keepObject = objects[0];

				// Destroy the others
				foreach (var obj in objects)
					if (obj != keepObject)
					{
						Debug.Log(
							$"[KoboldBootInitializer] Destroying duplicate {typeof(T).Name} on {obj.gameObject.name}");
						Destroy(obj.gameObject);
					}
			}
		}

		private IEnumerator InitializeThemeManager()
		{
			Debug.Log("[KoboldBootInitializer] Initializing Theme Manager...");

			// Check if theme manager already exists
			var existingThemeManager = FindFirstObjectByType<KoboldThemeManager>();
			if (existingThemeManager == null)
			{
				// Look for a prefab in the scene that has KoboldThemeManager
				var themeManagerPrefab = GameObject.Find("ThemeManager");
				if (themeManagerPrefab != null && themeManagerPrefab.GetComponent<KoboldThemeManager>() != null)
					Debug.Log("[KoboldBootInitializer] Found existing ThemeManager GameObject with component");
				else
					Debug.LogWarning(
						"[KoboldBootInitializer] No ThemeManager found. Please add KoboldThemeManager to the Boot scene with configured themes!");
				// Don't create one automatically - it needs to be configured
			}
			else
			{
				Debug.Log("[KoboldBootInitializer] Theme Manager already exists");
			}

			yield return null;
		}

		private bool VerifyRequiredSystems()
		{
			var allSystemsPresent = true;

			if (_requireServicesHelper && FindFirstObjectByType<KoboldServicesHelper>() == null)
			{
				Debug.LogError("[KoboldBootInitializer] KoboldServicesHelper not found!");
				allSystemsPresent = false;
			}

			if (_requireInputManager && FindFirstObjectByType<KoboldInputSystemManager>() == null)
			{
				Debug.LogError("[KoboldBootInitializer] KoboldInputSystemManager not found!");
				allSystemsPresent = false;
			}

			if (_requireVivoxManager && FindFirstObjectByType<KoboldVivoxManager>() == null)
				Debug.LogWarning("[KoboldBootInitializer] KoboldVivoxManager not found (optional)");
			// Don't fail boot for optional Vivox
			return allSystemsPresent;
		}

		private IEnumerator WaitForServicesHelper()
		{
			Debug.Log("[KoboldBootInitializer] Waiting for Services Helper initialization...");

			var servicesHelper = FindFirstObjectByType<KoboldServicesHelper>();
			if (servicesHelper == null)
			{
				Debug.LogError("[KoboldBootInitializer] Services Helper not found!");
				yield break;
			}

			// Wait for Unity Services to be initialized
			var timeout = 10f;
			var elapsed = 0f;

			while (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
			{
				if (elapsed > timeout)
				{
					Debug.LogError("[KoboldBootInitializer] Services initialization timeout!");
					yield break;
				}

				elapsed += Time.deltaTime;
				yield return null;
			}

			Debug.Log("[KoboldBootInitializer] Services Helper ready");
		}

		private void LoadMainMenu()
		{
			try
			{
				// Subscribe to scene loaded event for cleanup
				SceneManager.sceneLoaded += OnMainMenuLoaded;

				// Load the main menu scene
				//SceneManager.LoadScene(_mainMenuSceneName);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldBootInitializer] Failed to load main menu: {ex}");
			}
		}

		private void OnMainMenuLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.name == _mainMenuSceneName)
			{
				SceneManager.sceneLoaded -= OnMainMenuLoaded;
				Debug.Log("[KoboldBootInitializer] Main menu loaded successfully");

				// Notify that scene load is complete
				KoboldEventHandler.SceneLoadCompleted(scene.name);
			}
		}

#region Editor Support

#if UNITY_EDITOR
		[ContextMenu("Test Boot Sequence")]
		private void TestBootSequence()
		{
			if (Application.isPlaying)
				StartBootSequence();
			else
				Debug.LogWarning("Boot sequence can only be tested in Play mode");
		}

		private void OnValidate()
		{
			if (_minimumLoadTime < 0f) _minimumLoadTime = 0f;

			if (string.IsNullOrEmpty(_mainMenuSceneName)) _mainMenuSceneName = "MainMenu";
		}
#endif

#endregion
	}
}
