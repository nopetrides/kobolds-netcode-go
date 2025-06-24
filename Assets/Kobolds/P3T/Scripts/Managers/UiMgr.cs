using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using P3T.Scripts.UI;
using P3T.Scripts.Utils;
using UnityEngine;

namespace P3T.Scripts.Managers
{
	/// <summary>
	///     The UI manager for showing various menus and state
	/// </summary>
	public class UiMgr : Singleton<UiMgr>
	{
		[Header("Timing and sorting")] [SerializeField]
		private float FadeInDuration = 0.5f;

		[SerializeField] private float FadeOutDuration = 0.5f;
		[SerializeField] private int SortGap = 10;

		[SerializeField] private Transform MenuRoot;

		private readonly Dictionary<Type, MenuBase> _menuInstances = new();

		private readonly Stack<MenuBase> _activeMenus = new();
		private readonly Dictionary<Type, MenuBase> _disabledMenus = new();

		/// <summary>
		///     Clear the stack and close all menus
		/// </summary>
		public void CloseAllMenus()
		{
			while (_activeMenus.Count > 0)
			{
				var menu = _activeMenus.Pop();
				menu.PerformFullFadeOut(FadeOutDuration);
				_disabledMenus.Add(menu.GetType(), menu);
			}
		}

		public async Task<MenuBase> ShowMenu<T>() where T : MenuBase
		{
			return await ShowMenu(typeof(T));
		}

		/// <summary>
		///     Show a menu by adding it to the stack
		/// </summary>
		/// <param name="menuToOpen"></param>
		/// <param name="onMenuOpenStarting"></param>
		/// <param name="onMenuOpenComplete"></param>
		/// <param name="fadeIn"></param>
		/// <returns></returns>
		public async Task<MenuBase> ShowMenu(Type menuToOpen,
			Action onMenuOpenStarting = null,
			Action onMenuOpenComplete = null,
			bool fadeIn = true)
		{
			var menuBase = await PushMenu(menuToOpen);

			if (menuBase != null)
			{
				onMenuOpenStarting?.Invoke();
				if (fadeIn)
					menuBase.PerformFullFadeIn(FadeInDuration, onMenuOpenComplete);
				else
					onMenuOpenComplete?.Invoke();
			}

			return menuBase;
		}

		/// <summary>
		///     Half fade the screen when long processing happens
		///     Usually only needed if contacting the internet
		/// </summary>
		/// <param name="onComplete"></param>
		/// <returns></returns>
		public async Task<MenuBase> ShowHalfFader(Action onComplete)
		{
			var screenFadeOverlay = await ShowMenu(typeof(ScreenFadeOverlay), fadeIn: false);
			if (screenFadeOverlay != null)
				screenFadeOverlay.PerformHalfFadeIn(FadeInDuration, onComplete);

			return screenFadeOverlay;
		}

		/// <summary>
		///     Internal function.
		///     Pushes the given menu to the stack
		/// </summary>
		/// <returns></returns>
		private async Task<MenuBase> PushMenu(Type menuType)
		{
			var menuInstance = await GetMenuInstance(menuType);

			if (_activeMenus.Contains(menuInstance))
			{
				Debug.LogError($"Already opened menu {menuType.Name}");
				return menuInstance;
			}

			if (_disabledMenus.ContainsKey(menuType)) _disabledMenus.Remove(menuType);

			int sortOverride;

			if (_activeMenus.TryPeek(out var currentTop))
				sortOverride = currentTop.SortOrder + SortGap;
			else
				sortOverride = 0;

			menuInstance.SortOrder = sortOverride;

			menuInstance.PerformFullFadeIn(FadeInDuration);
			_activeMenus.Push(menuInstance);

			return menuInstance;
		}

		private async Task<MenuBase> GetMenuInstance(Type menuType)
		{
			// Check if object already exists
			if (!_menuInstances.TryGetValue(menuType, out MenuBase menuInstance))
			{
				// load and instantiate the game object
				string key = menuType.ToString().Split('.').Last();

				var prefab = await P3TAssetLoader.LoadAndReturnStoredAssetByKeyAsync(key);

				if (prefab == null)
				{
					Debug.LogError($"Failed to get prefab for type {key}");
					return null;
				}

				var createdObject = Instantiate(prefab, MenuRoot);

				menuInstance = createdObject.GetComponent<MenuBase>();
				if (menuInstance == null)
				{
					Debug.LogError($"Could not get {nameof(MenuBase)} from {createdObject.name}");
					return null;
				}
				
				menuInstance.OnInstantiate();
				_menuInstances.Add(menuType, menuInstance);

				return menuInstance;
			}

			return menuInstance;
		}

		/// <summary>
		///     Hide a given menu
		/// </summary>
		/// <param name="menuToClose"></param>
		/// <param name="onMenuFullyHidden"></param>
		/// <param name="fadeOut"></param>
		public void HideMenu(Type menuToClose, Action onMenuFullyHidden = null, bool fadeOut = true)
		{
			var menu = PopMenu(menuToClose);
			if (menu == null)
				return;

			if (fadeOut)
				menu.PerformFullFadeOut(FadeOutDuration, onMenuFullyHidden);
			else
				onMenuFullyHidden?.Invoke();
		}

		/// <summary>
		///     Internal function.
		///     Removes a menu from the stack
		/// </summary>
		/// <param name="menu"></param>
		/// <returns></returns>
		private MenuBase PopMenu(Type menu)
		{
			if (!_menuInstances.TryGetValue(menu, out var uiObj))
			{
				Debug.LogError($"Menu {menu} was never created");
				return null;
			}

			if (_activeMenus.TryPeek(out var peekedUI))
				if (peekedUI != uiObj)
				{
					Debug.LogError(
						$"The top of the stack {peekedUI.name} wasn't the object we wanted to hide {uiObj.name}");
					return null;
				}

			if (_activeMenus.TryPop(out var poppedUI))
				if (!_disabledMenus.TryAdd(menu, poppedUI))
					Debug.LogError(
						$"Failed to add {menu} to the disabled menus list. Was it already marked as disabled?");

			return poppedUI;
		}
	}
}