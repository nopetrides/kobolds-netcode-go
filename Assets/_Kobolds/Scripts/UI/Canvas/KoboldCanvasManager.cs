using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kobold
{
	public class KoboldCanvasManager : MonoBehaviour
	{
		[FormerlySerializedAs("UnburyUI")] [SerializeField]
		private UnburyUIFeedback _unburyUI;

		[SerializeField] private PlayerHudCanvas _playerHudCanvas;
		public static KoboldCanvasManager Instance { get; private set; }
		
		// local player events
		private KoboldGameplayEvents _gameplayEvents;

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
		}

		public void OnPlayerSpawned(UnburyController unburyController)
		{
			_gameplayEvents = unburyController.GetComponent<KoboldGameplayEvents>();
			if (_gameplayEvents != null)
			{
				_gameplayEvents.OnUnburyComplete += OnUnburyComplete;
			}
			else
			{
				Debug.LogError("KoboldGameplayEvents not found on the player prefab with UnburyController.", unburyController);
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
		}

		/// <summary>
		///     Hide the pause menu
		/// </summary>
		public void OnPlayerUnpause()
		{
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
