using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kobolds.Net
{
	/// <summary>
	/// Main network controller for Kobolds.
	/// Handles state synchronization and authority-based component management.
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
		[SerializeField] private Transform CameraTrackingObject;
        
        [Header("Transform References")]
        [SerializeField] private Transform MouthBone;
        [SerializeField] private Transform LeftHandBone;
        [SerializeField] private Transform RightHandBone;
        
        /// <summary>
        /// Main network variable containing all synchronized state.
        /// </summary>
        private NetworkVariable<KoboldNetworkState> _networkState = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);
        
        /// <summary>
        /// Gets the current network state.
        /// </summary>
        public KoboldNetworkState CurrentNetworkState => _networkState.Value;

        private void Awake()
        {
            ValidateComponents();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Set up state change callbacks
            _networkState.OnValueChanged += OnNetworkStateChanged;
            
            if (StateManager != null)
            {
                StateManager.OnStateChanged += OnLocalStateChanged;
            }
            
            // Configure components based on authority
            ConfigureAuthorityComponents();
            
            // If we're the owner, initialize our state
            if (IsOwner)
            {
                InitializeLocalPlayer();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (StateManager != null)
            {
                StateManager.OnStateChanged -= OnLocalStateChanged;
            }
            
            _networkState.OnValueChanged -= OnNetworkStateChanged;
            
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
            
            // Log for debugging
            Debug.Log($"[{name}] Configured for {(IsOwner ? "Local Player" : "Remote Player")} (Client {OwnerClientId})");
        }

        private void InitializeLocalPlayer()
        {
            // Assign main camera to our camera controller
            var mainCamera = Camera.main;
            if (mainCamera != null && CameraController != null)
            {
                CameraController.SetCamera(mainCamera, CameraTrackingObject);
                Debug.Log($"[{name}] Assigned main camera to local player");
            }
            else
            {
                Debug.LogError($"[{name}] Failed to find main camera or camera controller is null!");
            }
            
            // Name the GameObject for easier debugging
            gameObject.name = $"Kobold_Local_Client{NetworkManager.LocalClientId}";
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
        /// Updates the grabbed object reference. Only call on the owner.
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
        /// Updates the latch target. Only call on the owner.
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
        /// Gets the mouth bone transform for latching calculations.
        /// </summary>
        public Transform GetMouthBone() => MouthBone;

        /// <summary>
        /// Gets the hand bone transform for grabbing.
        /// </summary>
        public Transform GetHandBone(bool leftHand) => leftHand ? LeftHandBone : RightHandBone;
	}
}
