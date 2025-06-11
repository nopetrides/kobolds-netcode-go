using UnityEngine;

namespace Kobolds
{
	public enum KoboldState
	{
		Uninitialized,
		Unburying,
		Active,
		Climbing,
		RagdollOnly
	}

	public class KoboldStateManager : MonoBehaviour
	{
		[SerializeField] private KoboldState currentState = KoboldState.Uninitialized;

		public KoboldState CurrentState => currentState;

		public bool IsInState(KoboldState state) => currentState == state;

		public bool CanGrip => currentState == KoboldState.Active || currentState == KoboldState.Climbing;

		public bool IsClimbing => currentState == KoboldState.Climbing;


		public void SetState(KoboldState newState)
		{
			currentState = newState;
			// Optional: fire UnityEvent or C# event for subscribers
			Debug.Log($"Kobold state changed to: {CurrentState}");
		}

		public void OnUnburyComplete()
		{
			SetState(KoboldState.Active);
		}
	}
}
