using System;
using UnityEngine;

namespace Kobold
{
	/// <summary>
	/// Defines the possible states for a Kobold character.
	/// Used for network synchronization and gameplay logic.
	/// </summary>
	public enum KoboldState : byte
	{
		Uninitialized = 0,
		/// <summary>
		/// Kobold is buried and must struggle to escape.
		/// Full ragdoll physics active.
		/// </summary>
		Unburying = 1,
        
		/// <summary>
		/// Kobold has full player control and can move around.
		/// Uses RagdollMover for movement.
		/// </summary>
		Active = 2,
        
		/// <summary>
		/// Kobold is latched onto a surface by its mouth.
		/// Body ragdolls while mouth stays fixed.
		/// </summary>
		Climbing = 3,
		
		/// <summary>
		/// The Kobold did the flop and is rag-dolling around
		/// </summary>
		Flopping = 4,
	}

	public class KoboldStateManager : MonoBehaviour
	{
		[SerializeField] private KoboldState currentState = KoboldState.Uninitialized;

		public KoboldState CurrentState => currentState;

		public bool IsInState(KoboldState state) => currentState == state;

		public bool CanGrip => currentState == KoboldState.Active || currentState == KoboldState.Climbing;

		public bool IsClimbing => currentState == KoboldState.Climbing;
		
		public Action<KoboldState> OnStateChanged;


		public void SetState(KoboldState newState)
		{
			currentState = newState;
			// Optional: fire UnityEvent or C# event for subscribers
			Debug.Log($"Kobold state changed to: {CurrentState}");
			OnStateChanged?.Invoke(currentState);
		}

		public void OnUnburyComplete()
		{
			SetState(KoboldState.Active);
		}
	}
}
