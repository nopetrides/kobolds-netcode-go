using System.Collections;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
	public class AutoHide : MonoBehaviour
	{
		[SerializeField] private float m_TimeToHideSeconds = 5f;

		// Start is called before the first frame update
		private void Start()
		{
			StartCoroutine(HideAfterSeconds());
		}

		private IEnumerator HideAfterSeconds()
		{
			yield return new WaitForSeconds(m_TimeToHideSeconds);
			gameObject.SetActive(false);
		}
	}
}
