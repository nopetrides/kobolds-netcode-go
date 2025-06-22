using Kobold.Net;
using UnityEngine;

namespace Kobold.UI
{
	public class KoboldMainMenuManager : MonoBehaviour
	{
		[SerializeField] private KoboldMainMenuCanvas _mainMenu;
		[SerializeField] private KoboldSettings _settingsMenu;

		// local player events
		private KoboldGameplayEvents _gameplayEvents;
		private KoboldNetworkController _networkController;

		protected void Awake()
		{
			InitializeMenus();
			SetState(HudState.MainMenu);
		}

		private void OnDestroy()
		{
			_mainMenu.OnSettings -= OnSettings;
			_settingsMenu.OnClose -= OnMainMenu;
		}

		private void InitializeMenus()
		{
			_mainMenu.OnSettings += OnSettings;
			_settingsMenu.OnClose += OnMainMenu;
		}

		private void SetState(HudState s)
		{
			_mainMenu.gameObject.SetActive(s == HudState.MainMenu);
			_settingsMenu.gameObject.SetActive(s == HudState.Settings);
		}

		private void OnMainMenu()
		{
			SetState(HudState.MainMenu);
		}

		private void OnSettings()
		{
			SetState(HudState.Settings);
		}

		private enum HudState
		{
			MainMenu,
			Settings
		}
	}
}
