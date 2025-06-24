using System.Collections;
using System.Collections.Generic;
using Kobold.Monster;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	/// <summary>
	///     Authority-controlled networked kinematic body mover using animator.
	///     Only responsible for updating position information so that NetworkTransform and NetworkRigidbody
	///     sync the clients.
	/// </summary>
	public class BossMover : NetworkBehaviour
	{
		private static readonly int Walk = Animator.StringToHash("Walk");
		[SerializeField] private Animator Animator;
		[SerializeField] private List<Rigidbody> OnToppledRigidbodies;
		[SerializeField] private Rigidbody CoreRigidbody;

		private readonly Dictionary<Rigidbody, Transform> _originalPoseTargets = new();

		private void Awake()
		{
			// foreach (var rb in OnToppledRigidbodies)
			// {
			// 	var t = new GameObject($"{rb.name}_PoseTarget").transform;
			// 	t.position = rb.position;
			// 	t.rotation = rb.rotation;
			// 	t.SetParent(transform); // maintain hierarchy space
			// 	_originalPoseTargets[rb] = t;
			// }
		}

		private void Start()
		{
			Debug.Log($"[BossMover] Start() has authority: {HasAuthority}");
		}
		
		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			Initialize();
		}

		private void Initialize()
		{
			Debug.Log($"[BossMover] Initialize() has authority: {HasAuthority}");
			
			// Only authority controls the animator
			if (Animator != null)
			{
				Animator.enabled = HasAuthority;
				if (HasAuthority)
				{
					Animator.SetBool(Walk, true); // Loops a movement animation
				}
			}
		}

		private void Update()
		{
			// Use HasAuthority instead of IsOwner for distributed authority
			if (!HasAuthority || !Animator) return;

			// Current position and direction to center
			Vector3 toCenter = -transform.position;
			float distance = toCenter.magnitude;

			// Check if we're not on the circle edge
			if (Mathf.Abs(distance - 10) > 0.01f)
			{
				// Compute direction to rotate toward (tangent to the circle)
				Vector3 outward = toCenter.normalized;
				Vector3 tangent = Vector3.Cross(Vector3.up, outward); // Clockwise around Y axis

				// Smoothly rotate forward to match the tangent direction
				Quaternion targetRotation = Quaternion.LookRotation(tangent, Vector3.up);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 5 * Time.deltaTime);
			}
		}

		protected override void OnOwnershipChanged(ulong previousOwner, ulong currentOwner)
		{
			Debug.Log($"[BossMover] OnOwnershipChanged has authority: {HasAuthority} ({previousOwner}, {currentOwner})");
			base.OnOwnershipChanged(previousOwner, currentOwner);
			
			// Reconfigure animator when authority changes
			if (Animator != null)
			{
				Animator.enabled = HasAuthority;
			}
		}


		/// <summary>
		///     Makes the boss ToppledRigidbodies not kinematic.
		///     If at zero hp the core is handled through <see cref="PlayCoreRevealMotion" />.
		///     Also see <see cref="MonsterBossRPCHandler.PlayToppleEffect()" /> for non-motion visuals/>
		/// </summary>
		public void PlayToppleMotion()
		{
			// Use HasAuthority for distributed authority
			if (!HasAuthority)
			{
				Debug.LogWarning("[BossMover] Motion called by non-authority. Ignoring.");
				return;
			}

			Debug.Log("[BossMover] PlayToppleMotion()");
			Animator.SetBool(Walk, false);
			
			foreach (var rb in OnToppledRigidbodies)
			{
				rb.isKinematic = false; // make all rigidbodies not kinematic so they fall
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}

		/// <summary>
		///     Makes the boss ToppledRigidbodies kinematic.
		///     If the core is visible, it becomes hidden
		///     Also see <see cref="MonsterBossRPCHandler.PlayRecoveryEffect()" /> for non-motion visuals/>
		/// </summary>
		public void PlayRecoveryEffectMotion()
		{
			// Use HasAuthority for distributed authority
			if (!HasAuthority)
			{
				Debug.LogWarning("[BossMover] Motion called by non-authority. Ignoring.");
				return;
			}

			Debug.Log("[BossMover] PlayRecoveryEffectMotion()");

			foreach (var rb in OnToppledRigidbodies)
				if (_originalPoseTargets.TryGetValue(rb, out var target))
					StartCoroutine(InterpolateLimbToPose(rb, target));

			Animator.SetBool(Walk, true); // Loops a movement animation
		}

		public IEnumerator InterpolateLimbToPose(Rigidbody limb, Transform target)
		{
			var t = 0f;
			var startPos = limb.position;
			var startRot = limb.rotation;
			limb.isKinematic = true;

			while (t < 1f)
			{
				t += Time.deltaTime / 2f; // 2s duration
				limb.position = Vector3.Lerp(startPos, target.position, t);
				limb.rotation = Quaternion.Slerp(startRot, target.rotation, t);
				yield return null;
			}
		}


		/// <summary>
		///     Shake and have the entire boss fall apart
		///     Also see <see cref="MonsterBossRPCHandler.PlayDeathEffect()" /> for non-motion visuals/>
		/// </summary>
		public void PlayDeathMotion()
		{
			// Use HasAuthority for distributed authority
			if (!HasAuthority)
			{
				Debug.LogWarning("[BossMover] Motion called by non-authority. Ignoring.");
				return;
			}

			Debug.Log("[BossMover] PlayDeathMotion()");
			Animator.SetBool(Walk, false); // Loops a movement animation
			// TODO: shake and have the entire boss fall apart
			// Animator should already be disabled, but just in case
			//Animator.enabled = false;
			foreach (var rb in gameObject.GetComponentsInChildren<Rigidbody>())
			{
				rb.isKinematic = false; // make all rigidbodies not kinematic so they fall
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
			
			StartCoroutine(DespawnAfterDelay(5f));
		}

		private IEnumerator DespawnAfterDelay(float delay)
		{
			yield return new WaitForSeconds(delay);
			if (HasAuthority && NetworkObject != null && NetworkObject.IsSpawned)
			{
				Debug.Log("[BossMover] Despawning boss object after delay.");
				NetworkObject.Despawn();
			}
		}

		/// <summary>
		///     Make the core object visible, turn kinematic off.
		///     Also see <see cref="MonsterBossRPCHandler.PlayCoreReveal()" /> for non-motion visuals/>
		/// </summary>
		public void PlayCoreRevealMotion()
		{
			// Use HasAuthority for distributed authority
			if (!HasAuthority)
			{
				Debug.LogWarning("[BossMover] Motion called by non-authority. Ignoring.");
				return;
			}

			Debug.Log("[BossMover] PlayCoreRevealMotion()");

			if (CoreRigidbody != null)
			{
				CoreRigidbody.isKinematic = false;
				CoreRigidbody.useGravity = true;

				var c = CoreRigidbody.GetComponent<Collider>();
				if (c) c.enabled = true;
			}

			if (CoreRigidbody) CoreRigidbody.gameObject.SetActive(true);
		}


		/// <summary>
		///     Vibrates the limbs to warn about the incoming shockwave.
		///     Also see <see cref="MonsterBossRPCHandler.PlayAoePulse()" /> for non rigidbody visuals/>
		/// </summary>
		public void PlayAoePulseMotion()
		{
			// Use HasAuthority for distributed authority
			if (!HasAuthority) return;
			Debug.Log("[BossMover] PlayAOEPulseMotion()");

			foreach (var aoe in GetComponentsInChildren<AoePulseOnRecover>()) aoe.TriggerPulse();
		}
	}
}
