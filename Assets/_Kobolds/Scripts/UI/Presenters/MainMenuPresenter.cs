using System;
using Kobold.GameManagement;
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
			Debug.Log($"[MainMenuPresenter] Root panel: {_root?.panel != null}");

			// The root might be the container, so let's find the actual menu window
			_mainMenu = _root.Q<VisualElement>("main-menu-window");

			if (_mainMenu == null)
			{
				// Maybe the root IS the main menu window?
				if (_root?.name == "main-menu-window")
				{
					_mainMenu = _root;
				}
				// Or maybe it's the first child
				else if (_root?.childCount > 0 && _root[0].name == "main-menu-window")
				{
					_mainMenu = _root[0];
				}
				else
				{
					Debug.LogError("[MainMenuPresenter] Could not find main-menu-window element!");
					return;
				}
			}

			// Check if panel is attached to the menu element
			Debug.Log($"[MainMenuPresenter] Main menu panel: {_mainMenu.panel != null}");

			// Continue with initialization...
			Debug.Log($"[MainMenuPresenter] Found main menu with {_mainMenu.childCount} children");

			// Debug: Log the full structure
			LogElementStructure(_mainMenu, 0);

			// Bind all buttons
			BindButton(
				"social-hub-button", () =>
				{
					KoboldUISystem.Instance.ShowMenu(KoboldMenu.SocialHub);
					PlayClickSound();
				});

			BindButton(
				"quick-mission-button", () =>
				{
					var playerName = PlayerPrefs.GetString("PlayerName", _config.defaultPlayerName);
					KoboldEventHandler.QuickMissionPressed(playerName, "QuickMatch");
					PlayClickSound();
				});

			BindButton(
				"settings-button", () =>
				{
					KoboldUISystem.Instance.ShowMenu(KoboldMenu.Settings);
					PlayClickSound();
				});

			BindButton(
				"quit-button", () =>
				{
					QuitGame();
					PlayClickSound();
				});

			// Update version label if it exists
			var versionLabel = _mainMenu.Q<Label>("version-label");
			if (versionLabel != null) versionLabel.text = $"Version {Application.version}";
				
			// After binding buttons
			_root.schedule.Execute(() => 
			{
				var panel = _root.panel;
				Debug.Log($"[MainMenuPresenter] Panel focus owner: {panel?.focusController?.focusedElement?.tabIndex ?? -1}");
    
				// Force focus to the panel
				_root.Focus();
    
				// Try focusing a button directly
				var socialButton = _mainMenu.Q<Button>("social-hub-button");
				if (socialButton != null)
				{
					socialButton.Focus();
					Debug.Log($"[MainMenuPresenter] Forced focus to social button");
				}
			}).ExecuteLater(100); // Wait a bit for everything to settle
		}

		public void OnShow()
		{
			Debug.Log("[MainMenuPresenter] Main menu shown");
    
			// Use _mainMenu for scheduling, not _root
			if (_mainMenu?.panel != null)
			{
				_mainMenu.schedule.Execute(() => 
				{
					Debug.Log("[MainMenuPresenter] Schedule callback fired!");
            
					var button = _mainMenu.Q<Button>("social-hub-button");
					if (button != null)
					{
						Debug.Log($"[MainMenuPresenter] OnShow check - Button enabled: {button.enabledInHierarchy}, Clickable: {button.enabledSelf}");
                
						// Try re-binding to see if it works
						button.clicked += () => Debug.Log("[MainMenuPresenter] Re-bound handler fired!");
					}
				}).ExecuteLater(100);
			}
			else
			{
				Debug.LogError($"[MainMenuPresenter] Main menu panel is null in OnShow!");
			}
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
			var button = _mainMenu.Q<Button>(buttonName);
    
			if (button != null)
			{
				// Simple binding
				button.clicked += action;
        
				// Debug to confirm
				Debug.Log($"[MainMenuPresenter] Bound button '{buttonName}' - Enabled: {button.enabledInHierarchy}");
        
				// Add a test click handler to verify events are firing
				button.clicked += () => Debug.Log($"[MainMenuPresenter] Button '{buttonName}' was clicked!");
			}
			else
			{
				Debug.LogError($"[MainMenuPresenter] Button '{buttonName}' not found!");
			}
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
