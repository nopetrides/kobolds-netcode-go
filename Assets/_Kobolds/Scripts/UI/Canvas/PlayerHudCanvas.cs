using System.Collections.Generic;
using Kobold.Bosses;
using Kobold.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kobold.UI
{
	// Defines the behaviour for the player's heads-up display canvas.
	public class PlayerHudCanvas : MonoBehaviour
	{
		// Header for organizing health bar fields in the Inspector.
		[Header("Health Bars")]
		// Reference to the UI Image used as the player's health bar.
		[SerializeField] private Image _playerHealthBar;

		// Reference to the UI Image used as the boss's health bar.
		[SerializeField] private Image _bossHealthBar;

		// Header for organizing timer fields in the Inspector.
		[Header("Timer")]
		// Reference to the TextMeshProUGUI element for displaying the timer.
		[SerializeField] private TextMeshProUGUI _timerText;

		// Header for organizing latch state UI fields in the Inspector.
		[Header("Latch State")]
		// Image to show for the 'None' latch state.
		[SerializeField] private Image _latchNoneImage;

		// Image to show for the 'Open' latch state.
		[SerializeField] private Image _latchLatchReadyImage;

		// Image to show for the 'Gnaw' latch state.
		[SerializeField] private Image _latchGnawingImage;

		/// <summary>
		///     Overlay that shows the progress of the gnaw mechanic.
		/// </summary>
		/// <returns></returns>
		[SerializeField] private GnawElement _gnawOverlay;

		private MonsterBossController _bossController;

		// Stores the elapsed time for the timer.
		private float _elapsedTime;
		private KoboldGameplayEvents _gameplayEvents;
		private KoboldLatcher _latcher;

		// Dictionary to map latch states to their corresponding UI Images for efficient access.
		private Dictionary<LatchState, Image> _latchStateImages;
		private KoboldNetworkController _networkController;

		// Awake is called when the script instance is being loaded.
		private void Awake()
		{
			// Initialize the dictionary mapping latch states to their images.
			_latchStateImages = new Dictionary<LatchState, Image>
			{
				// Map the None state to its image.
				{LatchState.None, _latchNoneImage},
				// Map the Open state to its image.
				{LatchState.Open, _latchLatchReadyImage},
				// Map the Gnawing state to its image.
				{LatchState.Gnawing, _latchGnawingImage}
			};

			// Set the initial latch state display to 'None'.
			SetLatchState(LatchState.None);
		}

		// Update is called once per frame.
		private void Update()
		{
			// Increment the elapsed time by the time since the last frame.
			_elapsedTime += Time.deltaTime;
			// Update the timer display with the new elapsed time.
			UpdateTimerText();
		}

		private void OnDestroy()
		{
			if (_networkController != null) _networkController.OnNetworkStateChanged -= UpdatePlayerHealthFromState;

			if (_bossController != null) _bossController.OnHealthChanged -= UpdateBossHealth;

			if (_latcher != null) _latcher.OnLatchStateChanged -= HandleLatchStateChange;
		}

		public void Initialize(
			MonsterBossController bossController, KoboldNetworkController networkController,
			KoboldGameplayEvents gameplayEvents, KoboldLatcher latcher)
		{
			Debug.Log($"[PlayerHudCanvas] Initialize called with latcher: {(latcher != null ? latcher.name : "NULL")}");
    
			_bossController = bossController;
			_networkController = networkController;
			_gameplayEvents = gameplayEvents;
			_latcher = latcher;

			if (_networkController != null)
			{
				_networkController.OnNetworkStateChanged += UpdatePlayerHealthFromState;
				UpdatePlayerHealthFromState(_networkController.CurrentNetworkState);
			}

			if (_bossController != null)
			{
				_bossController.OnHealthChanged += UpdateBossHealth;
				UpdateBossHealth(_bossController.CurrentHealth, _bossController.MaxHealth);
			}

			if (_latcher != null)
			{
				Debug.Log($"[PlayerHudCanvas] Subscribing to OnLatchStateChanged event from latcher: {_latcher.name}");
        
				_latcher.OnLatchStateChanged += HandleLatchStateChange;
				// Set initial state
				SetLatchState(_latcher.CurrentLatchState);
			}
			else 
			{
				Debug.LogError("[PlayerHudCanvas] Initialize called with null latcher!");
			}
		}

		private void HandleLatchStateChange(LatchState newState)
		{
			SetLatchState(newState);
		}

		private void UpdatePlayerHealthFromState(KoboldNetworkState state)
		{
			UpdatePlayerHealth(state.Health, state.MaxHealth);
		}

		// Updates the player health bar's fill amount.
		public void UpdatePlayerHealth(float currentHealth, float maxHealth)
		{
			// Ensure the health bar image reference is not null.
			if (_playerHealthBar != null)
				// Calculate the health ratio and clamp it between 0 and 1.
				_playerHealthBar.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
		}

		// Updates the boss health bar's fill amount.
		public void UpdateBossHealth(float currentHealth, float maxHealth)
		{
			// Ensure the boss health bar image reference is not null.
			if (_bossHealthBar != null)
				// Calculate the health ratio and clamp it between 0 and 1.
				_bossHealthBar.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
		}

		// Updates the timer text to display the elapsed time.
		private void UpdateTimerText()
		{
			// Ensure the timer text reference is not null.
			if (_timerText != null)
			{
				// Calculate minutes from the total elapsed seconds.
				var minutes = Mathf.FloorToInt(_elapsedTime / 60F);
				// Calculate the remaining seconds.
				var seconds = Mathf.FloorToInt(_elapsedTime % 60F);
				// Format the time as MM:SS and update the text.
				_timerText.text = $"{minutes:00}:{seconds:00}";
			}
		}

		// Sets the visibility of latch state images based on the provided state.
		public void SetLatchState(LatchState state)
		{
			Debug.Log($"[PlayerHudCanvas] HandleLatchStateChange called with state:{state}");
			// Iterate over all latch state images in the dictionary.
			foreach (var entry in _latchStateImages)
				// Check if the image for the current state in the loop is assigned.
				if (entry.Value != null)
					// Activate the image's GameObject only if its key matches the desired state.
					entry.Value.gameObject.SetActive(entry.Key == state);
		}

		/// <summary>
		///     TODO
		/// </summary>
		public void ShowGnawOverlay()
		{
			// TODO
			_gnawOverlay.Initialize(null);
		}

		/// <summary>
		///     TODO
		/// </summary>
		public void HideGnawOverlay()
		{
			// TODO
			_gnawOverlay.Initialize(null);
		}

		/// <summary>
		///     TODO
		/// </summary>
		/// <param name="progress"></param>
		public void SetGnawOverlayProgress(float progress)
		{
		}
	}
}
