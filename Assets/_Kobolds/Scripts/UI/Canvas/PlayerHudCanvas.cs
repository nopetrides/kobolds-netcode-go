// Import necessary namespaces for Unity functionality, UI elements, and TextMeshPro.

using System.Collections.Generic;
using Kobold;
using Kobold.Bosses;
using Kobold.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Defines the behaviour for the player's heads-up display canvas.
public class PlayerHudCanvas : MonoBehaviour
{
	// Enum to represent the possible states of a latching mechanic.
	public enum LatchState
	{
		// State when nothing is latched.
		None,

		// State when something is successfully latched.
		Searching,

		// State when a latch has been broken.
		Gnawing
	}

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

	// Image to show for the 'Searching' latch state.
	[SerializeField] private Image _latchLatchReadyImage;

	// Image to show for the 'Gnaw' latch state.
	[SerializeField] private Image _latchGnawingImage;
	
	/// <summary>
	/// Overlay that shows the progress of the gnaw mechanic.
	/// </summary>
	/// <returns></returns>
	[SerializeField] private GnawElement _gnawOverlay;

	// Stores the elapsed time for the timer.
	private float _elapsedTime;

	// Dictionary to map latch states to their corresponding UI Images for efficient access.
	private Dictionary<LatchState, Image> _latchStateImages;

	private MonsterBossController _bossController;
	private KoboldNetworkController _networkController;
	private KoboldGameplayEvents _gameplayEvents;
	private KoboldLatcher _latcher;

	// Awake is called when the script instance is being loaded.
	private void Awake()
	{
		// Initialize the dictionary mapping latch states to their images.
		_latchStateImages = new Dictionary<LatchState, Image>
		{
			// Map the None state to its image.
			{LatchState.None, _latchNoneImage},
			// Map the Searching state to its image.
			{LatchState.Searching, _latchLatchReadyImage},
			// Map the Gnawing state to its image.
			{LatchState.Gnawing, _latchGnawingImage}
		};

		// Set the initial latch state display to 'None'.
		SetLatchState(LatchState.None);
	}

	private void OnDestroy()
	{
		if (_networkController != null)
		{
			_networkController.OnNetworkStateChanged -= UpdatePlayerHealthFromState;
		}
		if (_bossController != null)
		{
			_bossController.OnHealthChanged -= UpdateBossHealth;
		}
		if (_gameplayEvents != null)
		{
			_gameplayEvents.OnLatched -= HandleLatch;
			_gameplayEvents.OnDetached -= HandleDetach;
		}
		if (_latcher != null)
		{
			_latcher.OnLatchableTargetChanged -= HandleLatchableTargetChanged;
		}
	}

	public void Initialize(MonsterBossController bossController, KoboldNetworkController networkController, KoboldGameplayEvents gameplayEvents, KoboldLatcher latcher)
	{
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

		if (_gameplayEvents != null)
		{
			_gameplayEvents.OnLatched += HandleLatch;
			_gameplayEvents.OnDetached += HandleDetach;
		}
		if (_latcher != null)
		{
			_latcher.OnLatchableTargetChanged += HandleLatchableTargetChanged;
		}
	}
	
	private void HandleLatchableTargetChanged(bool isTargetInRange)
	{
		if (_latcher.IsLatched) return;
		
		if (isTargetInRange)
		{
			SetLatchState(LatchState.Searching);
		}
		else
		{
			SetLatchState(LatchState.None);
		}
	}

	private void HandleLatch(Collider target, Vector3 localPos, Quaternion localRot)
	{
		SetLatchState(LatchState.Gnawing);
	}

	private void HandleDetach()
	{
		SetLatchState(LatchState.None);
	}

	private void UpdatePlayerHealthFromState(KoboldNetworkState state)
	{
		UpdatePlayerHealth(state.Health, state.MaxHealth);
	}

	// Update is called once per frame.
	private void Update()
	{
		// Increment the elapsed time by the time since the last frame.
		_elapsedTime += Time.deltaTime;
		// Update the timer display with the new elapsed time.
		UpdateTimerText();
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
		// Iterate over all latch state images in the dictionary.
		foreach (var entry in _latchStateImages)
			// Check if the image for the current state in the loop is assigned.
			if (entry.Value != null)
				// Activate the image's GameObject only if its key matches the desired state.
				entry.Value.gameObject.SetActive(entry.Key == state);
	}
	
	/// <summary>
	/// TODO
	/// </summary>
	public void ShowGnawOverlay()
	{
		// TODO
		_gnawOverlay.Initialize(null);
	}
	
	/// <summary>
	/// TODO
	/// </summary>
	public void HideGnawOverlay()
	{
		// TODO
		_gnawOverlay.Initialize(null);
	}

	/// <summary>
	/// TODO
	/// </summary>
	/// <param name="progress"></param>
	public void SetGnawOverlayProgress(float progress)
	{
		
	}
}
