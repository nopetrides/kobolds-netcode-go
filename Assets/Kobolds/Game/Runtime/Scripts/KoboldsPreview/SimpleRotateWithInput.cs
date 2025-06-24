using UnityEngine;

namespace P3T.Scripts.KoboldsPreview
{
	public class SimpleRotateWithInput : MonoBehaviour
	{
		// Update is called once per frame
		void Update()
		{
			if (Input.GetKey(KeyCode.Q))
			{
				transform.Rotate(Vector3.up * (Time.deltaTime * 90));
			}
			else if (Input.GetKey(KeyCode.E))
			{
				transform.Rotate(Vector3.up * (Time.deltaTime * -90));
			}
		}

		public void RotateRight()
		{
			transform.Rotate(Vector3.up * 15);
		}

		public void RotateLeft()
		{
			transform.Rotate(Vector3.up * -15);
		}
	}
}