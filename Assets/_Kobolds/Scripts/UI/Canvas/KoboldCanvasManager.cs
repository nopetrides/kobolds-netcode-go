using Kobold.Bosses;
using Kobold.GameManagement;
using Kobold.Net;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kobold.UI
{
	public class KoboldCanvasManager : MonoBehaviour
	{
		[FormerlySerializedAs("UnburyUI")] [SerializeField]
		private UnburyUIFeedback _unburyUI;

		[SerializeField] private PlayerHudCanvas _playerHudCanvas;
		[SerializeField] private PauseMenu _pauseMenu;
		[SerializeField] private KoboldSettings _settingsMenu;

		// local player events
		private KoboldGameplayEvents _gameplayEvents;
		private KoboldNetworkController _networkController;
		public static KoboldCanvasManager Instance { get; private set; }

		protected void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Debug.LogError("Multiple KoboldCanvasManagers found. Destroying duplicate.");
				Destroy(gameObject);
				return;
			}

			KoboldEventHandler.OnAllBossesDefeated += OnGameComplete;

			InitializeMenus();

			SetState(HudState.Unbury);
		}

		private void OnDestroy()
		{
			KoboldEventHandler.OnAllBossesDefeated -= OnGameComplete;
			if (_gameplayEvents) _gameplayEvents.OnUnburyComplete -= OnUnburyComplete;
			if (_pauseMenu) _pauseMenu.OnResume -= OnPlayerUnpause;
			if (_pauseMenu) _pauseMenu.OnSettings -= OnSettings;
			if (_settingsMenu) _settingsMenu.OnClose -= OnPlayerPause;
		}

		private void InitializeMenus()
		{
			_pauseMenu.OnResume += OnPlayerUnpause;
			_pauseMenu.OnSettings += OnSettings;
			_settingsMenu.OnClose += OnPlayerPause;
		}

		private void SetState(HudState s)
		{
			_unburyUI.gameObject.SetActive(s == HudState.Unbury);
			_playerHudCanvas.gameObject.SetActive(s == HudState.InGame);
			_pauseMenu.gameObject.SetActive(s == HudState.Pause);
			_settingsMenu.gameObject.SetActive(s == HudState.Settings);
		}

		public void OnPlayerSpawned(UnburyController unburyController)
		{
			_networkController = unburyController.GetComponent<KoboldNetworkController>();
			_gameplayEvents = unburyController.GetComponent<KoboldGameplayEvents>();

			if (_gameplayEvents != null)
				_gameplayEvents.OnUnburyComplete += OnUnburyComplete;
			else
				Debug.LogError(
					"KoboldGameplayEvents not found on the player prefab with UnburyController.", unburyController);

			if (_networkController == null)
				Debug.LogError(
					"KoboldNetworkController not found on the player prefab with UnburyController.", unburyController);

			_unburyUI.gameObject.SetActive(true);
			_unburyUI.Initialize(unburyController);
		}

		/// <summary>
		///     Called when the local player finishes unburying
		/// </summary>
		public void OnUnburyComplete()
		{
			SetState(HudState.InGame);
			if (BossManager.Instance != null && BossManager.Instance.GetAllBosses()?.Count > 0)
				_playerHudCanvas.Initialize(
					BossManager.Instance.GetAllBosses()[0], _networkController, _gameplayEvents,
					_networkController?.GetComponent<KoboldLatcher>());
			else
				Debug.LogError("Failed to initialize PlayerHudCanvas: Boss not found.");
		}

		/// <summary>
		///     Called when the boss dies
		/// </summary>
		public void OnGameComplete()
		{
			// We'll show results later, for now let's just return to main menu
			KoboldEventHandler.ReturnToMainMenuPressed();
		}

		/// <summary>
		///     Show the pause menu
		/// </summary>
		public void OnPlayerPause()
		{
			SetState(HudState.Pause);
		}

		/// <summary>
		///     Hide the pause menu
		/// </summary>
		public void OnPlayerUnpause()
		{
			SetState(HudState.InGame);
		}

		public void OnSettings()
		{
			SetState(HudState.Settings);
		}

		private enum HudState
		{
			Unbury,
			InGame,
			Pause,
			Settings,
			GameOver
		}
	}
}
