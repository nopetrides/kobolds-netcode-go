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
	public class KoboldNetworkController : NetworkBehaviour
	{
		[Header("State Management")]
		[SerializeField] private KoboldStateManager StateManager;

		[Header("Authority-Controlled Components")]
		[SerializeField] private PlayerInput PlayerInput;

		[SerializeField] private KoboldCameraController CameraController;
		[SerializeField] private RagdollMover RagdollMover;

		[Header("Transform References")]
		[SerializeField] private Transform MouthBone;

		[SerializeField] private Transform LeftHandBone;
		[SerializeField] private Transform RightHandBone;
		[SerializeField] private Transform CameraTrackingObject;

		/// <summary>
		///     Main network variable containing all synchronized state.
		/// </summary>
		private readonly NetworkVariable<KoboldNetworkState> _networkState = new(
			readPerm: NetworkVariableReadPermission.Everyone,
			writePerm: NetworkVariableWritePermission.Owner);

		/// <summary>
		///     Gets the current network state.
		/// </summary>
		public KoboldNetworkState CurrentNetworkState => _networkState.Value;
		public KoboldCameraController CurrentCameraController => CameraController;

		private void Awake()
		{
			ValidateComponents();
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// Set up state change callbacks
			_networkState.OnValueChanged += OnNetworkStateChanged;

			if (StateManager != null) StateManager.OnStateChanged += OnLocalStateChanged;

			// Configure components based on authority
			ConfigureAuthorityComponents();
		}

		public override void OnNetworkDespawn()
		{
			if (StateManager != null) StateManager.OnStateChanged -= OnLocalStateChanged;

			_networkState.OnValueChanged -= OnNetworkStateChanged;

			// Unregister from camera manager if we're the local player
			if (IsOwner)
			{
				var cameraManager = KoboldCameraManager.Instance;
				if (cameraManager != null) cameraManager.RemoveLocalPlayer();
			}

			base.OnNetworkDespawn();
		}

		private void ValidateComponents()
		{
			if (StateManager == null)
				Debug.LogError($"[{name}] KoboldStateManager is not assigned!");

			if (PlayerInput == null)
				Debug.LogError($"[{name}] PlayerInput is not assigned!");

			if (CameraController == null)
				Debug.LogError($"[{name}] KoboldCameraController is not assigned!");

			if (MouthBone == null || LeftHandBone == null || RightHandBone == null)
				Debug.LogWarning($"[{name}] Some transform bones are not assigned. Transform sync will be limited.");
		}

		private void ConfigureAuthorityComponents()
		{
			// Only enable input and camera for the local player
			if (PlayerInput != null)
				PlayerInput.enabled = IsOwner;

			if (CameraController != null)
				CameraController.enabled = IsOwner;

			if (RagdollMover != null)
				RagdollMover.enabled = IsOwner;

			// If we're the owner, initialize our state
			if (IsOwner) InitializeLocalPlayer();

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

			Debug.Log($"[{name}] Local state changed to: {newState}");
		}

		private void OnNetworkStateChanged(KoboldNetworkState previousState, KoboldNetworkState newState)
		{
			// Don't process our own state changes twice
			if (IsOwner) return;

			// Update the state manager for remote players
			if (StateManager != null && previousState.State != newState.State)
			{
				StateManager.SetState(newState.State);
				Debug.Log($"[{name}] Remote state changed to: {newState.State}");
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
		///     Gets the mouth bone transform for latching calculations.
		/// </summary>
		public Transform GetMouthBone()
		{
			return MouthBone;
		}

		/// <summary>
		///     Gets the hand bone transform for grabbing.
		/// </summary>
		public Transform GetHandBone(bool leftHand)
		{
			return leftHand ? LeftHandBone : RightHandBone;
		}

		/// <summary>
		///     Gets the camera follow target for Cinemachine cameras.
		/// </summary>
		public Transform GetCameraFollowTarget()
		{
			return CameraTrackingObject != null ? CameraTrackingObject : transform;
		}
	}
}
