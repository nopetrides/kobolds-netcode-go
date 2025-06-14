using Kobolds.Cam;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kobolds.Net
{
	/// <summary>
	///     Main network controller for Kobolds.
	///     Handles state synchronization and authority-based component management.
	/// </summary>
	[RequireComponent(typeof(NetworkObject))]
	public partial class KoboldNetworkController : NetworkBehaviour
	{
		[Header("State Management")]
		[SerializeField] private KoboldStateManager _stateManager;

		[Header("Initial Values")]
		[SerializeField] private float _initialHealth = 100f;

		[SerializeField] private float _maxHealth = 100f;
		[SerializeField] private string _playerNamePrefix = "Kobold";

		[Header("Authority-Controlled Components")]
		[SerializeField] private PlayerInput _playerInput;

		[SerializeField] private KoboldCameraController _cameraController;
		[SerializeField] private RagdollMover _ragdollMover;

		[Header("Transform References")]
		[SerializeField] private Transform _mouthBone;

		[SerializeField] private Transform _leftHandBone;
		[SerializeField] private Transform _rightHandBone;
		[SerializeField] private Transform _cameraTrackingObject;

		/// <summary>
		///     Main network variable containing all synchronized state.
		/// </summary>
		private NetworkVariable<KoboldNetworkState> _networkState;

		/// <summary>
		///     Gets the current network state.
		/// </summary>
		public KoboldNetworkState CurrentNetworkState => _networkState.Value;

		public KoboldCameraController CurrentCameraController => _cameraController;

		private void Awake()
		{
			ValidateComponents();

			// Initialize NetworkVariable with a proper default state
			_networkState = new NetworkVariable<KoboldNetworkState>(
				KoboldNetworkState.CreateDefault(),
				NetworkVariableReadPermission.Everyone,
				NetworkVariableWritePermission.Owner);
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// Set up state change callbacks
			_networkState.OnValueChanged += OnNetworkStateChanged;

			// Configure components based on authority first
			ConfigureAuthorityComponents();

			if (IsOwner)
			{
				// For owners, we need to initialize our network state based on current game state
				if (_stateManager != null)
				{
					_stateManager.OnStateChanged += OnLocalStateChanged;

					// Initialize the network state with our current local state
					var currentGameplayState = _stateManager.CurrentState;

					// Only set initial values if this is truly a fresh spawn
					if (currentGameplayState != KoboldState.Uninitialized)
					{
						var initialState = new KoboldNetworkState
						{
							State = currentGameplayState,
							Health = _initialHealth,
							MaxHealth = _maxHealth,
							PlayerName = $"{_playerNamePrefix}_{NetworkManager.LocalClientId}",
							GrabbedObject = new NetworkObjectReference(),
							LatchTarget = new NetworkObjectReference(),
							LatchLocalPosition = Vector3.zero,
							LatchLocalRotation = Quaternion.identity
						};

						_networkState.Value = initialState;
						Debug.Log(
							$"[{name}] Owner initialized network state with current game state: {currentGameplayState}");
					}
				}
			}
			else
			{
				// For non-owners, immediately sync from the network state
				var networkState = _networkState.Value;
				if (networkState.State != KoboldState.Uninitialized)
				{
					SyncFromNetworkState(networkState);
					Debug.Log($"[{name}] Non-owner synced from network state: {networkState.State}");
				}
			}
		}

		public override void OnNetworkDespawn()
		{
			if (_stateManager != null)
				_stateManager.OnStateChanged -= OnLocalStateChanged;

			_networkState.OnValueChanged -= OnNetworkStateChanged;

			// Unregister from camera manager if we're the local player
			if (IsOwner)
			{
				var cameraManager = KoboldCameraManager.Instance;
				if (cameraManager != null)
					cameraManager.RemoveLocalPlayer();
			}

			base.OnNetworkDespawn();
		}

		private void InitializeOwnerState()
		{
			// Check if we already have a valid state (e.g., from previous session or server)
			var currentNetworkState = _networkState.Value;

			// Only initialize if we haven't been initialized yet (check for default/uninitialized state)
			if (currentNetworkState.State == KoboldState.Uninitialized && currentNetworkState.MaxHealth == 0f)
			{
				// Get current state from StateManager or use default
				var currentGameplayState = _stateManager != null ? _stateManager.CurrentState : KoboldState.Unburying;

				// Create initial state with all values
				var initialState = new KoboldNetworkState
				{
					State = currentGameplayState,
					Health = _initialHealth,
					MaxHealth = _maxHealth,
					PlayerName = $"{_playerNamePrefix}_{NetworkManager.LocalClientId}",
					GrabbedObject = new NetworkObjectReference(),
					LatchTarget = new NetworkObjectReference(),
					LatchLocalPosition = Vector3.zero,
					LatchLocalRotation = Quaternion.identity
				};

				// Set the NetworkVariable value
				_networkState.Value = initialState;

				Debug.Log(
					$"[{name}] Initialized owner state: State={initialState.State}, Health={initialState.Health}/{initialState.MaxHealth}");
			}
			else
			{
				// We already have a state, sync our local components to match
				Debug.Log(
					$"[{name}] Owner already has network state: State={currentNetworkState.State}, Health={currentNetworkState.Health}/{currentNetworkState.MaxHealth}");

				// Sync our local state manager to match the network state
				if (_stateManager != null && _stateManager.CurrentState != currentNetworkState.State)
					_stateManager.SetState(currentNetworkState.State);
			}
		}

		private void SyncFromNetworkState(KoboldNetworkState networkState)
		{
			// Sync state manager
			if (_stateManager != null && _stateManager.CurrentState != networkState.State)
			{
				_stateManager.SetState(networkState.State);
				ApplyRemoteRagdollState(networkState.State);
			}

			// Future: Sync health to health component
			// if (_healthComponent != null)
			//     _healthComponent.SetHealth(networkState.Health, networkState.MaxHealth);

			Debug.Log(
				$"[{name}] Synced from network state: State={networkState.State}, Health={networkState.Health}/{networkState.MaxHealth}");
		}

		private void ValidateComponents()
		{
			if (_stateManager == null)
				Debug.LogError($"[{name}] KoboldStateManager is not assigned!");

			if (_playerInput == null)
				Debug.LogError($"[{name}] PlayerInput is not assigned!");

			if (_cameraController == null)
				Debug.LogError($"[{name}] KoboldCameraController is not assigned!");

			if (_mouthBone == null || _leftHandBone == null || _rightHandBone == null)
				Debug.LogWarning($"[{name}] Some transform bones are not assigned. Transform sync will be limited.");
		}

		private void ConfigureAuthorityComponents()
		{
			// Only enable input and camera for the local player
			if (_playerInput != null)
				_playerInput.enabled = IsOwner;

			if (_cameraController != null)
				_cameraController.enabled = IsOwner;

			if (_ragdollMover != null)
				_ragdollMover.enabled = IsOwner;

			// If we're the owner, initialize local player setup
			if (IsOwner)
			{
				// Name the GameObject for easier debugging
				gameObject.name = $"Kobold_Local_Client{NetworkManager.LocalClientId}";

				// Register with camera manager
				var cameraManager = KoboldCameraManager.Instance;
				if (cameraManager != null)
				{
					cameraManager.AssignToLocalPlayer(this);
					Debug.Log($"[{name}] Registered with camera manager");
				}
				else
				{
					Debug.LogError($"[{name}] No KoboldCameraManager found in scene! Cameras will not work properly.");
				}
			}

			// Log for debugging
			Debug.Log(
				$"[{name}] Configured for {(IsOwner ? "Local Player" : "Remote Player")} (Client {OwnerClientId})");
		}

		private void InitializeLocalPlayer()
		{
			// Name the GameObject for easier debugging
			gameObject.name = $"Kobold_Local_Client{NetworkManager.LocalClientId}";

			// Register with camera manager
			var cameraManager = KoboldCameraManager.Instance;
			if (cameraManager != null)
			{
				cameraManager.AssignToLocalPlayer(this);
				Debug.Log($"[{name}] Registered with camera manager");
			}
			else
			{
				Debug.LogError($"[{name}] No KoboldCameraManager found in scene! Cameras will not work properly.");
			}
		}

		private void OnLocalStateChanged(KoboldState newState)
		{
			if (!IsOwner) return;

			// Update our network state when local state changes
			var currentState = _networkState.Value;
			currentState.State = newState;
			_networkState.Value = currentState;

			// Send RPC for immediate state propagation
			NotifyStateChangeRpc(newState);

			Debug.Log($"[{name}] Local state changed to: {newState}");
		}

		private void OnNetworkStateChanged(KoboldNetworkState previousState, KoboldNetworkState newState)
		{
			// Process state changes for both owners and non-owners
			// This ensures late-joiners get the correct state

			if (!IsOwner)
				// Non-owners should sync all changes
				SyncFromNetworkState(newState);
			else if (previousState.State == KoboldState.Uninitialized && newState.State != KoboldState.Uninitialized)
				// Owner receiving initial state from server (in case of reconnection scenarios)
				Debug.Log($"[{name}] Owner received initial state from server: {newState.State}");

			// Handle grab state changes for all players
			HandleGrabStateChange(previousState, newState);

			// Handle latch state changes for all players
			HandleLatchStateChange(previousState, newState);
		}

		/// <summary>
		///     Updates health value. Only call on the owner.
		/// </summary>
		public void SetHealth(float health)
		{
			if (!IsOwner)
			{
				Debug.LogWarning("Attempted to set health on non-owner!");
				return;
			}

			var currentState = _networkState.Value;
			currentState.Health = Mathf.Clamp(health, 0f, currentState.MaxHealth);
			_networkState.Value = currentState;
		}

		/// <summary>
		///     Updates max health value. Only call on the owner.
		/// </summary>
		public void SetMaxHealth(float maxHealth)
		{
			if (!IsOwner)
			{
				Debug.LogWarning("Attempted to set max health on non-owner!");
				return;
			}

			var currentState = _networkState.Value;
			currentState.MaxHealth = maxHealth;
			currentState.Health = Mathf.Min(currentState.Health, maxHealth);
			_networkState.Value = currentState;
		}

		/// <summary>
		///     Handles changes in grabbed object state.
		/// </summary>
		private void HandleGrabStateChange(KoboldNetworkState previousState, KoboldNetworkState newState)
		{
			// Object was grabbed
			if (!previousState.GrabbedObject.TryGet(out _) && newState.GrabbedObject.TryGet(out var grabbedObj))
				Debug.Log($"[{name}] Remote player grabbed object: {grabbedObj.name}");
			// Visual feedback for grabbed object could be added here
			// Object was released
			else if (previousState.GrabbedObject.TryGet(out _) && !newState.GrabbedObject.TryGet(out _))
				Debug.Log($"[{name}] Remote player released object");
		}

		/// <summary>
		///     Handles changes in latch state.
		/// </summary>
		private void HandleLatchStateChange(KoboldNetworkState previousState, KoboldNetworkState newState)
		{
			// Started latching
			if (!previousState.LatchTarget.TryGet(out _) && newState.LatchTarget.TryGet(out var latchTarget))
				Debug.Log($"[{name}] Remote player latched to: {latchTarget.name}");
			// Apply latch visual state if needed
			// Stopped latching
			else if (previousState.LatchTarget.TryGet(out _) && !newState.LatchTarget.TryGet(out _))
				Debug.Log($"[{name}] Remote player unlatched");
		}

		/// <summary>
		///     Updates the grabbed object reference. Only call on the owner.
		/// </summary>
		public void SetGrabbedObject(NetworkObject grabbedObject)
		{
			if (!IsOwner)
			{
				Debug.LogWarning("Attempted to set grabbed object on non-owner!");
				return;
			}

			var currentState = _networkState.Value;
			currentState.GrabbedObject = grabbedObject != null ?
				new NetworkObjectReference(grabbedObject) :
				new NetworkObjectReference();
			_networkState.Value = currentState;
		}

		/// <summary>
		///     Updates the latch target. Only call on the owner.
		/// </summary>
		public void SetLatchTarget(NetworkObject latchTarget, Vector3 localPos, Quaternion localRot)
		{
			if (!IsOwner)
			{
				Debug.LogWarning("Attempted to set latch target on non-owner!");
				return;
			}

			var currentState = _networkState.Value;
			currentState.LatchTarget = latchTarget != null ?
				new NetworkObjectReference(latchTarget) :
				new NetworkObjectReference();
			currentState.LatchLocalPosition = localPos;
			currentState.LatchLocalRotation = localRot;
			_networkState.Value = currentState;
		}

		/// <summary>
		///     Gets the mouth bone transform for latching calculations.
		/// </summary>
		public Transform GetMouthBone()
		{
			return _mouthBone;
		}

		/// <summary>
		///     Gets the hand bone transform for grabbing.
		/// </summary>
		public Transform GetHandBone(bool leftHand)
		{
			return leftHand ? _leftHandBone : _rightHandBone;
		}

		/// <summary>
		///     Gets the camera follow target for Cinemachine cameras.
		/// </summary>
		public Transform GetCameraFollowTarget()
		{
			return _cameraTrackingObject != null ? _cameraTrackingObject : transform;
		}
	}
}
