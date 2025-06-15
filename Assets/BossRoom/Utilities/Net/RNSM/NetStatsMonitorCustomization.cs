using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;



namespace Unity.Multiplayer.Samples.Utilities
{
	public class NetStatsMonitorCustomization : MonoBehaviour
	{
		private const int KNbTouchesToOpenWindow = 3;

		[SerializeField] private RuntimeNetStatsMonitor m_Monitor;

		private void Start()
		{
			m_Monitor.Visible = false;
		}

		private void Update()
		{
			// TODO REFACTOR
			if (Keyboard.current.spaceKey.wasPressedThisFrame ||
				(Touch.activeTouches.Count >= KNbTouchesToOpenWindow && AnyTouchDown()))
				m_Monitor.Visible =
					!m_Monitor
						.Visible; // toggle. Using "Visible" instead of "Enabled" to make sure RNSM keeps updating in the background
			// while not visible. This way, when bring it back visible, we can make sure values are up to date.
		}

		private static bool AnyTouchDown()
		{
			foreach (var touch in Touch.activeTouches)
				if (touch.phase == TouchPhase.Began)
					return true;

			return false;
		}
	}
}
