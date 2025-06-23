using System;
using Unity.Netcode;
using UnityEngine;

namespace Kobold
{
	/// <summary>
	///     Central event system for Kobold gameplay events.
	///     Only fires events on the authoritative client.
	/// </summary>
	public class KoboldGameplayEvents : MonoBehaviour
	{
		[SerializeField] private NetworkObject _networkObject;

		private void Awake()
		{
			if (_networkObject == null) Debug.LogError($"[{name}] No NetworkObject found in parent!");
		}

		// Grab/Release events
		public event Action<GameObject, GripType> OnObjectGrabbed;
		public event Action<GripType> OnObjectReleased;

		// Latch events  
		public event Action<Collider, Vector3, Quaternion> OnLatched;
		public event Action OnDetached;
		
		// New latch state events
		public event Action<LatchState> OnLatchStateChanged;
		public event Action<LatchState, LatchState> OnLatchStateTransitioned; // from, to

		// Unbury events
		public event Action<float> OnUnburyProgress;
		public event Action OnUnburyComplete;

		public event Action OnFlop;
		public event Action OnGetUp;

		/// <summary>
		///     Check if we should fire events (only on owner).
		/// </summary>
		private bool ShouldFireEvents()
		{
			return _networkObject != null && _networkObject.IsOwner;
		}

		// Called by KoboldGrabber when grabbing
		public void NotifyGrab(GameObject grabbedObject, GripType gripType)
		{
			if (!ShouldFireEvents()) return;
			OnObjectGrabbed?.Invoke(grabbedObject, gripType);
		}

		// Called by KoboldGrabber when releasing
		public void NotifyRelease(GripType gripType)
		{
			if (!ShouldFireEvents()) return;
			OnObjectReleased?.Invoke(gripType);
		}

		// Called by KoboldLatcher when latching
		public void NotifyLatch(Collider target, Vector3 localPos, Quaternion localRot)
		{
			if (!ShouldFireEvents()) return;
			OnLatched?.Invoke(target, localPos, localRot);
		}

		// Called by KoboldLatcher when detaching
		public void NotifyDetach()
		{
			if (!ShouldFireEvents()) return;
			OnDetached?.Invoke();
		}

		// Called by KoboldLatcher when latch state changes
		public void NotifyLatchStateChanged(LatchState newState)
		{
			if (!ShouldFireEvents()) return;
			OnLatchStateChanged?.Invoke(newState);
		}

		// Called by KoboldLatcher when latch state transitions
		public void NotifyLatchStateTransitioned(LatchState fromState, LatchState toState)
		{
			if (!ShouldFireEvents()) return;
			OnLatchStateTransitioned?.Invoke(fromState, toState);
		}

		// Called by UnburyController during struggle
		public void NotifyUnburyProgress(float progress)
		{
			if (!ShouldFireEvents()) return;
			OnUnburyProgress?.Invoke(progress);
		}

		// Called by UnburyController when unbury completes
		public void NotifyUnburyComplete()
		{
			if (!ShouldFireEvents()) return;
			OnUnburyComplete?.Invoke();
		}

		public void NotifyFlop()
		{
			if (!ShouldFireEvents()) return;
			OnFlop?.Invoke();
		}

		public void NotifyGetUp()
		{
			if (!ShouldFireEvents()) return;
			OnGetUp?.Invoke();
		}
	}
}
