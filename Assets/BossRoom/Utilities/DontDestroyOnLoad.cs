using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
	public class DontDestroyOnLoad : MonoBehaviour
	{
		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
