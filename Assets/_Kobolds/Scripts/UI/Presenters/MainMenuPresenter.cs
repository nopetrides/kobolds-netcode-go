using System;
using Kobold.GameManagement;
using Kobold.UI.Components;
using Kobold.UI.Configuration;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Presenters
{
	/// <summary>
	///     Main menu presenter
	/// </summary>
	public class MainMenuPresenter : IUIPresenter
	{
		private KoboldUIConfiguration _config;
		private VisualElement _mainMenu;
		private VisualElement _root;

		public void Initialize(VisualElement root, KoboldUIConfiguration config)
		{
			_root = root;
			_config = config;

			// Debug: Log the structure
			Debug.Log($"[MainMenuPresenter] Root element: {_root?.name}, children: {_root?.childCount}");

			// Find the main menu window
			_mainMenu = _root.Q<VisualElement>("main-menu-window");

			if (_mainMenu == null)
			{
				// Maybe the root IS the main menu window?
				if (_root.name == "main-menu-window")
				{
					_mainMenu = _root;
				}
				else
				{
					Debug.LogError("[MainMenuPresenter] Could not find main-menu-window element!");
					return;
				}
			}

			Debug.Log($"[MainMenuPresenter] Found main menu with {_mainMenu.childCount} children");

			// Debug: Log the full structure
			LogElementStructure(_mainMenu, 0);

			// Bind all buttons
			BindButton(
				"social-hub-button", () =>
				{
					PlayClickSound();
					KoboldUISystem.Instance.ShowMenu(KoboldMenu.SocialHub);
				});

			BindButton(
				"quick-mission-button", () =>
				{
					PlayClickSound();
					var playerName = PlayerPrefs.GetString("PlayerName", _config.defaultPlayerName);
					KoboldEventHandler.QuickMissionPressed(playerName, "QuickMatch");
				});

			BindButton(
				"settings-button", () =>
				{
					PlayClickSound();
					KoboldUISystem.Instance.ShowMenu(KoboldMenu.Settings);
				});

			BindButton(
				"quit-button", () =>
				{
					PlayClickSound();
					QuitGame();
				});

			// Update version label if it exists
			var versionLabel = _mainMenu.Q<Label>("version-label");
			if (versionLabel != null) versionLabel.text = $"Version {Application.version}";
		}

		public void OnShow()
		{
			// Called when main menu is shown
			Debug.Log("[MainMenuPresenter] Main menu shown");
		}

		public void OnHide()
		{
			// Called when main menu is hidden
		}

		public void Cleanup()
		{
			// Cleanup if needed
		}

		private void LogElementStructure(VisualElement element, int depth)
		{
			var indent = new string(' ', depth * 2);
			Debug.Log(
				$"{indent}{element.GetType().Name}: '{element.name}' (classes: {string.Join(", ", element.GetClasses())})");

			foreach (var child in element.Children()) LogElementStructure(child, depth + 1);
		}

		private void BindButton(string buttonName, Action action)
		{
			// First try to find the KoboldButtonElement
			var koboldButtonElement = _mainMenu.Q<KoboldButtonElement>(buttonName);

			if (koboldButtonElement != null)
			{
				// Debug what's inside
				Debug.Log(
					$"[MainMenuPresenter] Found KoboldButtonElement '{buttonName}' with {koboldButtonElement.childCount} children");
				foreach (var child in koboldButtonElement.Children())
					Debug.Log($"  - Child: {child.GetType().Name} '{child.name}'");

				// The KoboldButtonElement should have created a KoboldButton as a child
				var koboldButton = koboldButtonElement.Q<KoboldButton>();
				if (koboldButton != null)
				{
					// Subscribe to the KoboldButton's Clicked event
					koboldButton.Clicked += action;
					Debug.Log($"[MainMenuPresenter] Successfully bound KoboldButton '{buttonName}'");
					return;
				}

				// Maybe it needs more time to initialize?
				koboldButtonElement.schedule.Execute(() =>
				{
					var delayedButton = koboldButtonElement.Q<KoboldButton>();
					if (delayedButton != null)
					{
						delayedButton.Clicked += action;
						Debug.Log($"[MainMenuPresenter] Successfully bound KoboldButton '{buttonName}' (delayed)");
					}
					else
					{
						Debug.LogWarning($"[MainMenuPresenter] Still couldn't find KoboldButton inside '{buttonName}'");
					}
				}).ExecuteLater(100);

				return;
			}

			// Fallback: try standard button search
			var button = _mainMenu.Q<Button>(buttonName);
			if (button != null)
			{
				button.clicked += action;
				Debug.Log($"[MainMenuPresenter] Successfully bound standard button '{buttonName}'");
				return;
			}

			Debug.LogWarning($"[MainMenuPresenter] Button '{buttonName}' not found!");
		}

		private void PlayClickSound()
		{
			if (_config.enableUISounds) KoboldUISystem.Instance.PlayUISound(UISoundType.Click);
		}

		private void QuitGame()
		{
#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
		}
	}
}
