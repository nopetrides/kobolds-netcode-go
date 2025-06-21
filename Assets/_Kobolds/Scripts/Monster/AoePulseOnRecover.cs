using System.Collections;
using Kobold.Net;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Monster
{
	public class AoePulseOnRecover : MonoBehaviour
	{
		[SerializeField] private float _pulseRadius = 5f;
		[SerializeField] private float _expandDuration = 0.5f;
		[SerializeField] private float _damageAmount = 100f;

		public void TriggerPulse()
		{
			StartCoroutine(Pulse());
		}

		private IEnumerator Pulse()
		{
			var t = 0f;
			while (t < _expandDuration)
			{
				t += Time.deltaTime;
				var radius = Mathf.Lerp(0f, _pulseRadius, t / _expandDuration);
				CheckOverlap(radius);
				yield return null;
			}
		}

		private void CheckOverlap(float currentRadius)
		{
			// Use session owner check for distributed authority
			if (!NetworkManager.Singleton.IsServer) return;

			var hits = Physics.OverlapSphere(transform.position, currentRadius, LayerMask.GetMask("Player"));

			foreach (var hit in hits)
			{
				var controller = hit.GetComponentInParent<KoboldNetworkController>();
				if (controller != null && controller.HasAuthority) // Use HasAuthority instead of IsOwner
				{
					controller.RequestDamageServerRpc(_damageAmount);
				}
			}
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
