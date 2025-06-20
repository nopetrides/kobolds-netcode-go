using System;
using System.Collections;
using FIMSpace.FProceduralAnimation;
using Kobold.Cam;
using Kobold.Gameplay;
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

		[Header("Initial Values")]
		[SerializeField] private float _initialHealth = 100f;

		[SerializeField] private float _maxHealth = 100f;
		[SerializeField] private string _playerNamePrefix = "Kobold";

		[Header("Authority-Controlled Components")]
		private PlayerInput PlayerInput { get; set; }

		[SerializeField] private KoboldCameraController _cameraController;
		[SerializeField] private RagdollMover _ragdollMover;
		[SerializeField] private KoboldFlopControls _flopControls;
		[SerializeField] private UnburyController _unburyController;

		[Header("Transform References")]
		[SerializeField] private Transform _mouthBone;

		[SerializeField] private Transform _leftHandBone;
		[SerializeField] private Transform _rightHandBone;
		[SerializeField] private Transform _cameraTrackingObject;
		[SerializeField] private Transform _ragdollTrackingObject;

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
			_networkState.OnValueChanged += OnReplicatedStateChanged;

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
				StartCoroutine(WaitForRagdoll());
			}
		}

		private IEnumerator WaitForRagdoll()
		{
			// For non-owners, immediately sync from the network state
			var networkState = _networkState.Value;
			if (networkState.State != KoboldState.Uninitialized)
			{
				yield return new WaitWhile(() => _ragdollAnimator?.Handler?.GetAnchorBoneController?.GameRigidbody != null);
				
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
				SyncFromNetworkState(newState);
			else if (IsOwner)
			{
				// If we ever hit this path as owner, something is wrong
				Debug.LogError($"[{name}] Unexpected: state changed externally on owner object. From {previousState.State} to {newState.State}");
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
			
			if (_networkState.Value.Health <= 0f)
			{
				Respawn();
			}
		}
		
		/// <summary>
		/// Sets the speed for animator
		/// </summary>
		/// <param name="speed"></param>
		public void SetMoveSpeed(float speed)
		{
			var state = _networkState.Value;
			state.MoveSpeed = speed;
			_networkState.Value = state;
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
			// Started latching
			if (!previousState.LatchTarget.TryGet(out _) && newState.LatchTarget.TryGet(out var latchTarget))
			{
				Debug.Log($"[{name}] Remote player latched to: {latchTarget.name}");
				
				ApplyRemoteLatch(newState);
			}
			// Stopped latching
			else if (previousState.LatchTarget.TryGet(out _) && !newState.LatchTarget.TryGet(out _))
				Debug.Log($"[{name}] Remote player unlatched");
		}
		
		private void ApplyRemoteLatch(KoboldNetworkState state)
		{
			if (_koboldLatcher == null) return;
			if (!state.LatchTarget.TryGet(out var target)) return;

			// Snap the latch bone to the latched position
			var bone = _ragdollAnimator.Handler?.User_GetBoneSetupBySourceAnimatorBone(_koboldLatcher.JawLatchMagnet.MagnetPoint.transform)?.BoneProcessor;
			if (bone?.rigidbody == null) return;

			Vector3 worldPos = target.transform.TransformPoint(state.LatchLocalPosition);
			Quaternion worldRot = target.transform.rotation * state.LatchLocalRotation;

			var rb = bone.rigidbody;
			rb.isKinematic = true;
			rb.position = worldPos;
			rb.rotation = worldRot;
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

	}
}
