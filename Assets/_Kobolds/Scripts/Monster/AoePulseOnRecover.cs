using System.Collections;
using UnityEngine;

namespace Kobold.Monster
{
	public class AoePulseOnRecover : MonoBehaviour
	{
		[SerializeField] private float _pulseRadius = 5f;
		[SerializeField] private float _damageAmount = 100f;
		[SerializeField] private GameObject _sphereVisualPrefab; // A prefab for the sphere visual effect
		
		private GameObject _sphereVisualInstance;               // Instance of the visual sphere
		private Transform _sphereTransform;                     // Cached transform of the sphere
		private Collider[] _hitBuffer = new Collider[20];       // Buffer for overlap detection
		
		/// <summary>
		/// Starts visualizing the sphere growth during the charge phase.
		/// </summary>
		/// <param name="duration">Duration of the sphere charge</param>
		public void StartChargeVisual(float duration)
		{
			if (_sphereVisualInstance == null)
			{
				_sphereVisualInstance = Instantiate(_sphereVisualPrefab, transform.position, Quaternion.identity, transform);
				_sphereTransform = _sphereVisualInstance.transform;
				_sphereTransform.localScale = Vector3.zero; // Start at zero scale
			}

			// Grow the sphere over time
			StartCoroutine(GrowSphere(duration));
		}

		/// <summary>
		/// Coroutine to grow the sphere visually over time.
		/// </summary>
		private IEnumerator GrowSphere(float duration)
		{
			float t = 0f;

			while (t < duration)
			{
				t += Time.deltaTime;
				float scale = Mathf.Lerp(0f, _pulseRadius * 2f, t / duration); // Diameter-based scaling
				_sphereTransform.localScale = new Vector3(scale, scale, scale);
				yield return null;
			}
		}
		
		/// <summary>
		/// Trigger the actual pulse, causing damage to all players in range.
		/// </summary>
		public void TriggerPulse()
		{
			// Trigger visual impact
			if (_sphereVisualInstance != null) Destroy(_sphereVisualInstance);

			Pulse();
		}

		/// <summary>
		/// Performs the pulse attack by detecting overlapping players inside the radius.
		/// </summary>
		private void Pulse()
		{
			var hits = Physics.OverlapSphereNonAlloc(transform.position, _pulseRadius, _hitBuffer, LayerMask.GetMask("Latch"));

			for (int i = 0; i < hits; i++)
			{
				var koboldController = _hitBuffer[i].GetComponentInParent<Kobold.Net.KoboldNetworkController>();
				if (koboldController != null)
				{
					koboldController.ApplyAoePulseDamage(_damageAmount); // Players apply the damage (including to themselves)
				}
			}

			_hitBuffer = new Collider[20];
		}
		
#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = new Color(1f, 0.25f, 0f, 0.4f);
			Gizmos.DrawSphere(transform.position, _pulseRadius);

			Gizmos.color = new Color(1f, 0.25f, 0f, 1f);
			Gizmos.DrawWireSphere(transform.position, _pulseRadius);
		}
#endif

	}
}
