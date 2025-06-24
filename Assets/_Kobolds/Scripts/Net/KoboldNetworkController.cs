using System;
using System.Collections;
using FIMSpace.FProceduralAnimation;
using Kobold.Cam;
using Kobold.Gameplay;
using Kobold.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kobold.Net
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
		[SerializeField] private KoboldGameplayEvents _gameplayEvents;

		[Header("Initial Values")]
		[SerializeField] private float _initialHealth = 100f;

		[SerializeField] private float _maxHealth = 100f;
		[SerializeField] private string _playerNamePrefix = "Kobold";

		[SerializeField] private KoboldCameraController _cameraController;
		[SerializeField] private RagdollMover _ragdollMover;
		[SerializeField] private KoboldFlopControls _flopControls;
		[SerializeField] private UnburyController _unburyController;
		[SerializeField] private KoboldLatcher _koboldLatcher;
		[SerializeField] private RagdollAnimator2 _ragdollAnimator;

		[Header("Transform References")]
		[SerializeField] private Transform _mouthBone;

		[SerializeField] private Transform _leftHandBone;
		[SerializeField] private Transform _rightHandBone;
		[SerializeField] private Transform _cameraTrackingObject;
		[SerializeField] private Transform _ragdollTrackingObject;

		private bool _isPaused;

		// Used to track self-authored state changes
		private KoboldNetworkState _lastWrittenState;

		/// <summary>
		///     Main network variable containing all synchronized state.
		/// </summary>
		private NetworkVariable<KoboldNetworkState> _networkState;

		private bool _suppressNextStateEcho;

		[Header("Authority-Controlled Components")]
		private PlayerInput PlayerInput { get; set; }

		/// <summary>
		///     Gets the current network state.
		/// </summary>
		public KoboldNetworkState CurrentNetworkState => _networkState.Value;

		public KoboldCameraController CurrentCameraController => _cameraController;

		public KoboldLatcher CurrentLatcher => _koboldLatcher;

		private void Awake()
		{
			ValidateComponents();

			// Initialize NetworkVariable with a proper default state
			_networkState = new NetworkVariable<KoboldNetworkState>(
				KoboldNetworkState.CreateDefault(),
				NetworkVariableReadPermission.Everyone,
				NetworkVariableWritePermission.Owner);
		}

		private void Update()
		{
			if (!IsOwner) return;

			if (KoboldInputSystemManager.Instance.Inputs.Escape)
			{
				_isPaused = !_isPaused;
				if (_isPaused)
				{
					KoboldCanvasManager.Instance.OnPlayerPause();
					KoboldInputSystemManager.Instance.EnableUIMode();
				}
				else
				{
					KoboldCanvasManager.Instance.OnPlayerUnpause();
					KoboldInputSystemManager.Instance.EnableGameplayMode();
				}
			}

			KoboldInputSystemManager.Instance.Inputs.Escape = false;
		}

		private void FixedUpdate()
		{
			// Not owner, do nothing.
		}

		private void LateUpdate()
		{
			if (IsOwner) return;

			var currentState = _networkState.Value;
			if (currentState.LatchState == LatchState.Gnawing)
			{
				// Continuously apply the latch position to keep up with moving targets
				ApplyRemoteLatch(currentState);
			}
		}

		public event Action<KoboldNetworkState> OnNetworkStateChanged;

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

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
				// Set up state change callbacks
				_networkState.OnValueChanged += OnReplicatedStateChanged;
				StartCoroutine(WaitForRagdoll());
			}
		}

		private IEnumerator WaitForRagdoll()
		{
			// For non-owners, immediately sync from the network state
			var networkState = _networkState.Value;
			if (networkState.State != KoboldState.Uninitialized)
			{
				yield return new WaitWhile(() =>
					_ragdollAnimator?.Handler?.GetAnchorBoneController?.GameRigidbody != null);

				var autoGetUp = _ragdollAnimator.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
				if (autoGetUp != null)
					autoGetUp.Enabled = false;
				else
					Debug.LogError("RagdollAnimator2 component does not have RAF_AutoGetUp feature enabled");

				SyncFromNetworkState(networkState);
				Debug.Log($"[{name}] Non-owner synced from network state: {networkState.State}");
			}
		}

		public override void OnNetworkDespawn()
		{
			if (_stateManager != null)
				_stateManager.OnStateChanged -= OnLocalStateChanged;

			_networkState.OnValueChanged -= OnReplicatedStateChanged;

			// Unregister from camera manager if we're the local player
			if (IsOwner)
			{
				var cameraManager = KoboldCameraManager.Instance;
				if (cameraManager != null)
					cameraManager.RemoveLocalPlayer();
			}

			base.OnNetworkDespawn();
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

			PlayerInput = KoboldInputSystemManager.Instance.NewInputSystem;

			if (PlayerInput == null)
				Debug.LogError($"[{name}] PlayerInput is not assigned!");

			if (_ragdollMover == null)
				Debug.LogError($"[{name}] RagdollMover is not assigned!");

			if (_flopControls == null)
				Debug.LogError($"[{name}] KoboldFlopControls is not assigned!");

			if (_unburyController == null)
				Debug.LogError($"[{name}] UnburyController is not assigned!");

			if (_cameraController == null)
				Debug.LogError($"[{name}] KoboldCameraController is not assigned!");

			if (_mouthBone == null || _leftHandBone == null || _rightHandBone == null)
				Debug.LogWarning($"[{name}] Some transform bones are not assigned. Transform sync will be limited.");
		}

		private void ConfigureAuthorityComponents()
		{
			if (_cameraController != null)
				_cameraController.enabled = IsOwner;

			if (_ragdollMover != null)
				_ragdollMover.enabled = IsOwner;

			if (_flopControls != null)
				_flopControls.enabled = IsOwner;

			if (_unburyController != null)
				_unburyController.enabled = IsOwner;

			if (_koboldLatcher != null) 
				_koboldLatcher.enabled = IsOwner;
			
			var grabber = GetComponentInChildren<KoboldGrabber>();
			if (grabber != null) 
				grabber.enabled = IsOwner;

			var autoGetUp = _ragdollAnimator.Handler.GetExtraFeatureHelper<RAF_AutoGetUp>();
			if (autoGetUp != null)
				autoGetUp.Enabled = false;

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

				KoboldCanvasManager.Instance?.OnPlayerSpawned(this, _gameplayEvents, _unburyController);
			}

			// Log for debugging
			Debug.Log(
				$"[{name}] Configured for {(IsOwner ? "Local Player" : "Remote Player")} (Client {OwnerClientId})");
		}

		private void OnLocalStateChanged(KoboldState newState)
		{
			if (!IsOwner) return;

			// Update our network state when local state changes
			var currentState = _networkState.Value;
			_lastWrittenState = currentState;
			_suppressNextStateEcho = true;
			currentState.State = newState;
			_networkState.Value = currentState;

			// Send RPC for immediate state propagation
			NotifyStateChangeRpc(newState);

			Debug.Log($"[{name}] Local state changed to: {newState}");
		}

		private void OnReplicatedStateChanged(KoboldNetworkState previousState, KoboldNetworkState newState)
		{
			// Process state changes for both owners and non-owners
			// This ensures late-joiners get the correct state

			if (!IsOwner)
				// Non-owners should sync all changes
			{
				SyncFromNetworkState(newState);
			}
			else if (IsOwner)
			{
				if (_suppressNextStateEcho && newState.Equals(_lastWrittenState))
				{
					_suppressNextStateEcho = false;
					return; // Suppress local echo
				}

				Debug.LogError(
					$"[{name}] Unexpected replicated state change on owner object. This may indicate unauthorized modification. From {previousState.State} to {newState.State}");
			}

			{
				// If we ever hit this path as owner, something is wrong
			}

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

			if (_networkState.Value.Health <= 0f) Respawn();
		}

		private void Respawn()
		{
			var spawnPoint = KoboldPlayerSpawnPoints.Instance.GetRandomSpawnPoint();

			_ragdollAnimator.User_SetAllVelocity(Vector3.zero);
			_ragdollAnimator.User_SetAllBonesVelocity(Vector3.zero);
			var rb = _ragdollAnimator.GetComponent<Rigidbody>();
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			_ragdollAnimator.transform.position = spawnPoint.position;
			_ragdollAnimator.User_Teleport();

			SetHealth(_networkState.Value.MaxHealth);

			Debug.Log($"[{name}] Respawned at {spawnPoint.position}");
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
			if (previousState.LatchState == newState.LatchState) return;

			// This logic should run on all clients, including the owner, to properly reflect the networked state.
			UpdateRemoteLatchState(newState.LatchState);

			if (newState.LatchState == LatchState.Gnawing)
			{
				// This is a one-time setup call when the state transitions TO Gnawing.
				// The continuous update will be handled in LateUpdate.
				ApplyRemoteLatch(newState);
			}
			else if (previousState.LatchState == LatchState.Gnawing)
			{
				// Unlatch logic
				var boneProcessor = _ragdollAnimator.Handler
					?.User_GetBoneSetupBySourceAnimatorBone(_koboldLatcher.JawLatchMagnet.MagnetPoint.transform)
					?.BoneProcessor;
				if (boneProcessor?.rigidbody != null)
				{
					boneProcessor.rigidbody.isKinematic = false;
				}
			}
		}

		/// <summary>
		/// Updates the remote kobold's latch state for visual feedback.
		/// </summary>
		private void UpdateRemoteLatchState(LatchState latchState)
		{
			if (_koboldLatcher == null) return;

			// Update animator for remote players
			var animator = _koboldLatcher.GetComponent<Animator>();
			if (animator != null)
			{
				animator.SetBool("Grip_Jaw", latchState != LatchState.None);
			}

			// Update state manager for remote players
			if (_stateManager != null)
			{
				if (latchState == LatchState.Gnawing && _stateManager.CurrentState != KoboldState.Climbing)
				{
					_stateManager.SetState(KoboldState.Climbing);
				}
				else if (latchState == LatchState.None && _stateManager.CurrentState == KoboldState.Climbing)
				{
					_stateManager.SetState(KoboldState.Active);
				}
			}
		}

		private void ApplyRemoteLatch(KoboldNetworkState state)
		{
			if (_koboldLatcher == null) return;
			var bone = _ragdollAnimator.Handler
				?.User_GetBoneSetupBySourceAnimatorBone(_koboldLatcher.JawLatchMagnet.MagnetPoint.transform)
				?.BoneProcessor;
			if (bone?.rigidbody == null) return;
			var rb = bone.rigidbody;
			rb.isKinematic = true;

			if (state.LatchIsNetworked)
			{
				if (!state.LatchTarget.TryGet(out var networkObject)) return;

				var indexer = networkObject.GetComponent<LatchableColliderIndexer>();
				if (indexer == null)
				{
					Debug.LogError($"[KoboldNetworkController] Latch target {networkObject.name} is missing LatchableColliderIndexer! Attaching to root.");
					var worldPosFallback = networkObject.transform.TransformPoint(state.LatchLocalPosition);
					var worldRotFallback = networkObject.transform.rotation * state.LatchLocalRotation;
					rb.position = worldPosFallback;
					rb.rotation = worldRotFallback;
					return;
				}

				var targetCollider = indexer.GetColliderByIndex(state.LatchColliderIndex);
				if (targetCollider == null)
				{
					Debug.LogError($"[KoboldNetworkController] Latch collider index {state.LatchColliderIndex} is out of bounds for {networkObject.name}! Attaching to root.");
					var worldPosFallback = networkObject.transform.TransformPoint(state.LatchLocalPosition);
					var worldRotFallback = networkObject.transform.rotation * state.LatchLocalRotation;
					rb.position = worldPosFallback;
					rb.rotation = worldRotFallback;
					return;
				}

				// Use the specific collider's transform for correct offset
				var worldPos = targetCollider.transform.TransformPoint(state.LatchLocalPosition);
				var worldRot = targetCollider.transform.rotation * state.LatchLocalRotation;
				
				rb.position = worldPos;
				rb.rotation = worldRot;
			}
			else
			{
				// Static geometry logic
				rb.position = state.LatchWorldPosition;
				rb.rotation = state.LatchWorldRotation;
			}
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
		///     Updates the latch state. Only call on the owner.
		/// </summary>
		public void SetLatchState(LatchState latchState)
		{
			if (!IsOwner)
			{
				Debug.LogWarning("Attempted to set latch state on non-owner!");
				return;
			}

			var currentState = _networkState.Value;
			currentState.LatchState = latchState;
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

		public Transform GetRagdollFollowTarget()
		{
			return _ragdollTrackingObject != null ? _ragdollTrackingObject : transform;
		}

		[ServerRpc(RequireOwnership = false)]
		public void RequestDamageServerRpc(float damage)
		{
			if (IsOwner)
				SetHealth(CurrentNetworkState.Health - damage);
		}

		private void OnStateChanged(KoboldNetworkState previous, KoboldNetworkState current)
		{
			OnNetworkStateChanged?.Invoke(current);
			// Future: Sync health to health component
			// if (_healthComponent != null)
		}

		// Hybrid latch setter for both networked and static geometry
		public void SetHybridLatchTarget(NetworkObject latchTarget, int colliderIndex, Vector3 localPos, Quaternion localRot, bool isNetworked, Vector3 worldPos, Quaternion worldRot)
		{
			if (!IsOwner)
			{
				Debug.LogWarning("Attempted to set latch target on non-owner!");
				return;
			}
			var currentState = _networkState.Value;
			if (isNetworked && latchTarget != null)
			{
				currentState.LatchTarget = new NetworkObjectReference(latchTarget);
				currentState.LatchLocalPosition = localPos;
				currentState.LatchLocalRotation = localRot;
				currentState.LatchColliderIndex = colliderIndex;
				currentState.LatchWorldPosition = Vector3.zero;
				currentState.LatchWorldRotation = Quaternion.identity;
				currentState.LatchIsNetworked = true;
				if (_koboldLatcher != null && _koboldLatcher.enableLatchDebugLogging)
				{
					Debug.Log($"[KoboldNetworkController] SetHybridLatchTarget: NetObj={latchTarget.NetworkObjectId}, idx={colliderIndex}, localPos={localPos}, localRot={localRot}");
				}
			}
			else
			{
				currentState.LatchTarget = new NetworkObjectReference();
				currentState.LatchLocalPosition = Vector3.zero;
				currentState.LatchLocalRotation = Quaternion.identity;
				currentState.LatchColliderIndex = -1;
				currentState.LatchWorldPosition = worldPos;
				currentState.LatchWorldRotation = worldRot;
				currentState.LatchIsNetworked = false;
				if (_koboldLatcher != null && _koboldLatcher.enableLatchDebugLogging)
				{
					Debug.Log($"[KoboldNetworkController] SetHybridLatchTarget: Static world, worldPos={worldPos}, worldRot={worldRot}");
				}
			}
			_networkState.Value = currentState;
		}
	}
}
