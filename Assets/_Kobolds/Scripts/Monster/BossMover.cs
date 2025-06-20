using System.Collections;
using System.Collections.Generic;
using Kobold.Monster;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	/// <summary>
	/// Owner only networked kinematic body mover using animator.
	/// Only responsible for updating position information so that NetworkTransform and NetworkRigidbody
	/// sync the clients.
	/// </summary>
	public class BossMover : NetworkBehaviour
	{
		[SerializeField] private Animator Animator;
		[SerializeField] private List<Rigidbody> OnToppledRigidbodies;
		[SerializeField] private Rigidbody CoreRigidbody;
		
		private Dictionary<Rigidbody, Transform> _originalPoseTargets = new();

void Awake()
{
    foreach (var rb in OnToppledRigidbodies)
    {
        var t = new GameObject($"{rb.name}_PoseTarget").transform;
        t.position = rb.position;
        t.rotation = rb.rotation;
        t.SetParent(transform); // maintain hierarchy space
        _originalPoseTargets[rb] = t;
    }
}

		void Start()
		{
			Animator.enabled = IsOwner;
			if (!IsOwner)
				return;

			Animator.Play("Walk"); // Loops a movement animation
		}

		protected override void OnOwnershipChanged(ulong previousOwner,ulong currentOwner)
		{
			base.OnOwnershipChanged(previousOwner, currentOwner);
			Animator.enabled = IsOwner;
		}
		
		private void Update()
		{
			if (!IsOwner || !Animator) return;

			// Orbit around world origin at a constant speed
			float orbitSpeed = 10f;
			transform.RotateAround(Vector3.zero, Vector3.up, orbitSpeed * Time.deltaTime);
		}


		/// <summary>
		/// Makes the boss ToppledRigidbodies not kinematic.
		/// If at zero hp the core is handled through <see cref="PlayCoreRevealMotion"/>.
		/// Also see <see cref="MonsterBossRPCHandler.PlayToppleEffect()"/> for non-motion visuals/>
		/// </summary>
		public void PlayToppleMotion()
		{
			if (!IsOwner)
			{
				Debug.LogWarning("[BossMover] Motion called by non-owner. Ignoring.");
				return;
			}
			Debug.Log("[BossMover] PlayToppleMotion()");
			// TODO: Make the boss stop and idle, disable kinematic subset
			Animator.enabled = false;
			foreach (var rb in OnToppledRigidbodies)
			{
				rb.isKinematic = false; // make all rigidbodies not kinematic so they fall
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}

		/// <summary>
		/// Makes the boss ToppledRigidbodies kinematic.
		/// If the core is visible, it becomes hidden
		/// Also see <see cref="MonsterBossRPCHandler.PlayRecoveryEffect()"/> for non-motion visuals/>
		/// </summary>
		public void PlayRecoveryEffectMotion()
		{
			if (!IsOwner)
			{
				Debug.LogWarning("[BossMover] Motion called by non-owner. Ignoring.");
				return;
			}
			Debug.Log("[BossMover] PlayRecoveryEffectMotion()");
			
			foreach (var rb in OnToppledRigidbodies)
			{
				if (_originalPoseTargets.TryGetValue(rb, out var target))
				{
					StartCoroutine(InterpolateLimbToPose(rb, target));
				}
			}

			Animator.enabled = true; // maybe only after the reassembly is done?
		}
		
		public IEnumerator InterpolateLimbToPose(Rigidbody limb, Transform target)
		{
			float t = 0f;
			Vector3 startPos = limb.position;
			Quaternion startRot = limb.rotation;
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
		/// Shake and have the entire boss fall apart
		/// Also see <see cref="MonsterBossRPCHandler.PlayDeathEffect()"/> for non-motion visuals/>
		/// </summary>
		public void PlayDeathMotion()
		{
			if (!IsOwner)
			{
				Debug.LogWarning("[BossMover] Motion called by non-owner. Ignoring.");
				return;
			}
			Debug.Log("[BossMover] PlayDeathMotion()");
			// TODO: shake and have the entire boss fall apart
			// Animator should already be disabled, but just in case
			Animator.enabled = false;
			foreach (var rb in gameObject.GetComponentsInChildren<Rigidbody>())
			{
				rb.isKinematic = false; // make all rigidbodies not kinematic so they fall
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}

		/// <summary>
		/// Make the core object visible, turn kinematic off.
		/// Also see <see cref="MonsterBossRPCHandler.PlayCoreReveal()"/> for non-motion visuals/>
		/// </summary>
		public void PlayCoreRevealMotion()
		{
			if (!IsOwner)
			{
				Debug.LogWarning("[BossMover] Motion called by non-owner. Ignoring.");
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
		/// Vibrates the limbs to warn about the incoming shockwave.
		/// Also see <see cref="MonsterBossRPCHandler.PlayAoePulse()"/> for non rigidbody visuals/>
		/// </summary>
		public void PlayAoePulseMotion()
		{
			if (!IsOwner) return;
			Debug.Log("[BossMover] PlayAOEPulseMotion()");

			foreach (var aoe in GetComponentsInChildren<AoePulseOnRecover>())
			{
				aoe.TriggerPulse();
			}
		}

	}
}
