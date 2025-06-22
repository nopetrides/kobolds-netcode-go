using Kobold.GameManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Kobold.Bosses;
using Kobold.Net;

namespace Kobold
{
	public class KoboldCanvasManager : MonoBehaviour
	{
		[FormerlySerializedAs("UnburyUI")] [SerializeField]
		private UnburyUIFeedback _unburyUI;

		[SerializeField] private PlayerHudCanvas _playerHudCanvas;
		[SerializeField] private PauseMenu _pauseMenu;
		public static KoboldCanvasManager Instance { get; private set; }
		
		// local player events
		private KoboldGameplayEvents _gameplayEvents;
		private KoboldNetworkController _networkController;

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
			_pauseMenu.Initialize(this);
			_pauseMenu.gameObject.SetActive(false);
			SetState(HudState.Unbury);
		}

		private void OnDestroy()
		{
			KoboldEventHandler.OnAllBossesDefeated -= OnGameComplete;
			if (_gameplayEvents != null)
			{
				_gameplayEvents.OnUnburyComplete -= OnUnburyComplete;
			}
		}

		private void SetState(HudState s)
		{
			_unburyUI.gameObject.SetActive(s == HudState.Unbury);
			_playerHudCanvas.gameObject.SetActive(s == HudState.InGame);
			_pauseMenu.gameObject.SetActive(s == HudState.Pause);
		}

		public void OnPlayerSpawned(UnburyController unburyController)
		{
			_networkController = unburyController.GetComponent<KoboldNetworkController>();
			_gameplayEvents = unburyController.GetComponent<KoboldGameplayEvents>();
			
			if (_gameplayEvents != null)
			{
				_gameplayEvents.OnUnburyComplete += OnUnburyComplete;
			}
			else
			{
				Debug.LogError("KoboldGameplayEvents not found on the player prefab with UnburyController.", unburyController);
			}

			if (_networkController == null)
			{
				Debug.LogError("KoboldNetworkController not found on the player prefab with UnburyController.", unburyController);
			}
			
			_unburyUI.gameObject.SetActive(true);
			_unburyUI.Initialize(unburyController);
		}

		/// <summary>
		///		Called when the local player finishes unburying
		/// </summary>
		public void OnUnburyComplete()
		{
			SetState(HudState.InGame);
			if (BossManager.Instance != null && BossManager.Instance.GetAllBosses()?.Count > 0)
			{
				_playerHudCanvas.Initialize(BossManager.Instance.GetAllBosses()[0], _networkController, _gameplayEvents, _networkController?.GetComponent<KoboldLatcher>());
			}
			else
			{
				Debug.LogError("Failed to initialize PlayerHudCanvas: Boss not found.");
			}
		}

		/// <summary>
		///		Called when the boss dies
		/// </summary>
		public void OnGameComplete()
		{
			// We'll show results later, for now let's just return to main menu
			KoboldEventHandler.ReturnToMainMenuPressed();
		}

		/// <summary>
		///		Show the pause menu
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

		private enum HudState
		{
			Unbury,
			InGame,
			Pause,
			GameOver
		}
	}
}
