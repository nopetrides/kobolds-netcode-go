using FIMSpace.FProceduralAnimation;
using Kobold.Net;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Kobold
{
	/// <summary>
	/// Handles ragdoll network synchronization based on kobold state.
	/// Manages which transforms are networked and how ragdoll physics are applied.
	/// </summary>
	[RequireComponent(typeof(KoboldNetworkController))]
	public class KoboldRagdollNetworkSync : NetworkBehaviour
	{
		[Header("References")]
		[SerializeField] private KoboldNetworkController _networkController;
		[SerializeField] private KoboldStateManager _stateManager;
		[SerializeField] private RagdollAnimator2 _ragdollAnimator;
		[SerializeField] private KoboldLatcher _latcher;
		
		[Header("Network Transforms")]
		[SerializeField] private NetworkTransform _mainTransform;
		[SerializeField] private NetworkTransform _ragdollRootTransform;
		
		private NetworkTransform _currentActiveTransform;
		private Rigidbody _currentActiveRigidbody;

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			
			if (_networkController == null)
				_networkController = GetComponent<KoboldNetworkController>();
			
			if (_stateManager == null)
				_stateManager = GetComponent<KoboldStateManager>();
			
			if (_ragdollAnimator == null)
				_ragdollAnimator = GetComponent<RagdollAnimator2>();
			
			if (_latcher == null)
				_latcher = GetComponent<KoboldLatcher>();

			// Subscribe to state changes
			if (IsOwner)
			{
				if (_stateManager != null)
					_stateManager.OnStateChanged += OnStateChanged;

				if (_latcher != null)
					_latcher.OnLatchStateChanged += OnLatchStateChanged;
			}

			// Initialize with current state
			UpdateNetworkSync();
		}

		public override void OnNetworkDespawn()
		{
			if (_stateManager != null)
				_stateManager.OnStateChanged -= OnStateChanged;
			
			if (_latcher != null)
				_latcher.OnLatchStateChanged -= OnLatchStateChanged;

			base.OnNetworkDespawn();
		}

		private void OnStateChanged(KoboldState newState)
		{
			UpdateNetworkSync();
		}

		private void OnLatchStateChanged(LatchState newState)
		{
			UpdateNetworkSync();
		}

		/// <summary>
		/// Updates which transform is networked based on current state.
		/// </summary>
		private void UpdateNetworkSync()
		{
			if (!IsSpawned) return;

			// Determine which transform should be active based on state
			NetworkTransform newActiveTransform = null;
			Rigidbody newActiveRigidbody = null;

			switch (_stateManager.CurrentState)
			{
				case KoboldState.Active:
					// Use main transform for normal movement
					newActiveTransform = _mainTransform;
					newActiveRigidbody = GetComponent<Rigidbody>();
					break;

				case KoboldState.Climbing:
					// Use jaw bone transform when latched
					if (_latcher != null && _latcher.IsLatched)
					{
						var bone = _ragdollAnimator.Handler?.User_GetBoneSetupBySourceAnimatorBone(_latcher.JawLatchMagnet.MagnetPoint.transform)?.BoneProcessor;
						if (bone?.rigidbody != null)
						{
							newActiveRigidbody = bone.rigidbody;
							// Note: We don't have a NetworkTransform for individual bones
							// The position is synced via NetworkVariable in KoboldNetworkState
						}
					}
					break;

				case KoboldState.Unburying:
				case KoboldState.Flopping:
					// Use ragdoll root transform for physics states
					newActiveTransform = _ragdollRootTransform;
					newActiveRigidbody = _ragdollAnimator.Handler?.GetAnchorBoneController?.GameRigidbody;
					break;
			}

			// Update active transform
			if (_currentActiveTransform != newActiveTransform)
			{
				if (_currentActiveTransform != null)
					_currentActiveTransform.enabled = false;
				
				if (newActiveTransform != null)
					newActiveTransform.enabled = true;
				
				_currentActiveTransform = newActiveTransform;
			}

			// Update active rigidbody
			_currentActiveRigidbody = newActiveRigidbody;

			Debug.Log($"[KoboldRagdollNetworkSync] Updated network sync for state {_stateManager.CurrentState}, active transform: {_currentActiveTransform?.name ?? "none"}");
		}

		/// <summary>
		/// Gets the currently active rigidbody for network synchronization.
		/// </summary>
		public Rigidbody GetActiveRigidbody()
		{
			return _currentActiveRigidbody;
		}

		/// <summary>
		/// Gets the currently active transform for network synchronization.
		/// </summary>
		public NetworkTransform GetActiveNetworkTransform()
		{
			return _currentActiveTransform;
		}
	}
} 