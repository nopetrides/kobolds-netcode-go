using UnityEngine;

namespace Kobold.UI
{
	public class KoboldLoadingSpinner : MonoBehaviour
	{
		[SerializeField] private float degreesPerSecond = 360f; // degrees per second
		
		private float _rotation;
		private bool _spinning;

		private void Update()
		{
			if (!_spinning) return;

			_rotation += degreesPerSecond * Time.unscaledDeltaTime;
			_rotation %= 360f;

			// Apply rotation to RectTransform
			transform.localRotation = Quaternion.Euler(0, 0, -_rotation); // negative to rotate clockwise
		}

		private void OnEnable()
		{
			StartSpinning();
		}

		private void OnDisable()
		{
			StopSpinning();
		}

		private void StartSpinning()
		{
			_rotation = 0f;
			_spinning = true;
		}

		private void StopSpinning()
		{
			_spinning = false;
		}
	}
}
