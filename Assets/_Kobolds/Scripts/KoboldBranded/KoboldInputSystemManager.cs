using System.Threading.Tasks;
using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Kobold
{
	public class KoboldInputSystemManager : MonoBehaviour
	{
		[SerializeField] private PlayerInput _playerInput;
		[SerializeField] private KoboldInputs _koboldInput;

		[Header("Cursor Settings")]
		[SerializeField]
		private bool _startInGameplayMode;

		public static KoboldInputSystemManager Instance { get; private set; }

		public PlayerInput NewInputSystem => _playerInput;
		public KoboldInputs Inputs => _koboldInput;

		public bool IsInUIMode { get; private set; } = true;

		public bool IsInGameplayMode => !IsInUIMode;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(this);
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void Start()
		{
			// Subscribe to scene/state events
			KoboldEventHandler.OnConnectToSessionCompleted += OnSessionConnected;
			KoboldEventHandler.OnExitedSession += OnSessionExited;

			// Start in appropriate mode
			if (_startInGameplayMode)
				EnableGameplayMode();
			else
				EnableUIMode();
		}

		private void OnDestroy()
		{
			if (Instance == this) Instance = null;

			KoboldEventHandler.OnConnectToSessionCompleted -= OnSessionConnected;
			KoboldEventHandler.OnExitedSession -= OnSessionExited;
		}

		public void EnableUIMode()
		{
			IsInUIMode = true;
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			NewInputSystem.SwitchCurrentActionMap("UI");
			Debug.Log("[InputSystemManager] Switched to: " + _playerInput.currentActionMap.name);


			// You could add an event here to notify KoboldInputs components
			// KoboldEventHandler.InputModeChanged?.Invoke(false);
		}

		public void EnableGameplayMode()
		{
			IsInUIMode = false;
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
			NewInputSystem.SwitchCurrentActionMap("Player");
			Debug.Log("[InputSystemManager] Switched to: " + _playerInput.currentActionMap.name);

			// You could add an event here to notify KoboldInputs components
			// KoboldEventHandler.InputModeChanged?.Invoke(true);
		}

		// Automatically switch modes based on game state
		private void OnSessionConnected(Task task, string sessionName)
		{
			if (task.IsCompletedSuccessfully)
				// We're in game now, enable gameplay mode
				EnableGameplayMode();
			// clear this in case there was some buffered input
			Inputs.Escape = false;
		}

		private void OnSessionExited()
		{
			// Back to menu, enable UI mode
			EnableUIMode();
		}
	}
}
