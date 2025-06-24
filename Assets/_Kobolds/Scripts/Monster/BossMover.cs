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
		[SerializeField] private float _recoverySpeed = 2f;
		[SerializeField] private float _maxDistanceFromCenter = 8f;
		[SerializeField] private float _maxRotationSpeed = 15f;

		private readonly Dictionary<Rigidbody, Vector3> _originalLocalPositions = new();
		private readonly Dictionary<Rigidbody, Quaternion> _originalLocalRotations = new();

		private void Awake()
		{
			// Cache local positions and rotations for all rigidbodies
			foreach (var rb in OnToppledRigidbodies)
			{
				_originalLocalPositions[rb] = rb.transform.localPosition;
				_originalLocalRotations[rb] = rb.transform.localRotation;
			}

			if (CoreRigidbody != null)
			{
				_originalLocalPositions[CoreRigidbody] = CoreRigidbody.transform.localPosition;
				_originalLocalRotations[CoreRigidbody] = CoreRigidbody.transform.localRotation;
			}

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
			if (Mathf.Abs(distance - _maxDistanceFromCenter) > 0.01f)
			{
				// Compute direction to rotate toward (tangent to the circle)
				Vector3 outward = toCenter.normalized;
				Vector3 tangent = Vector3.Cross(Vector3.up, outward); // Clockwise around Y axis

				// Smoothly rotate forward to match the tangent direction
				Quaternion targetRotation = Quaternion.LookRotation(tangent, Vector3.up);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _maxRotationSpeed * Time.deltaTime);
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
			Animator.enabled = false;
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
			{
				if (_originalLocalPositions.TryGetValue(rb, out var localPos) &&
					_originalLocalRotations.TryGetValue(rb, out var localRot))
				{
					StartCoroutine(InterpolatePartToPose(rb, localPos, localRot));
				}
			}

			if (CoreRigidbody != null && CoreRigidbody.gameObject.activeSelf &&
				_originalLocalPositions.TryGetValue(CoreRigidbody, out var coreLocalPos) &&
				_originalLocalRotations.TryGetValue(CoreRigidbody, out var coreLocalRot))
			{
				CoreRigidbody.gameObject.SetActive(true);
				StartCoroutine(InterpolatePartToPose(CoreRigidbody, coreLocalPos, coreLocalRot));
			}
		}

		public IEnumerator InterpolatePartToPose(Rigidbody limb, Vector3 localTargetPos, Quaternion localTargetRot)
		{
			var t = 0f;
			var startPos = limb.transform.localPosition;
			var startRot = limb.transform.localRotation;
			limb.isKinematic = true;

			while (t < 1f)
			{
				t += Time.deltaTime / _recoverySpeed; // where _recoverySpeed is duration
				limb.transform.localPosition = Vector3.Lerp(startPos, localTargetPos, t);
				limb.transform.localRotation = Quaternion.Slerp(startRot, localTargetRot, t);
				yield return null;
			}
			
			Animator.enabled = true;
			Animator.Rebind();
			Animator.Update(0f);
			Animator.SetBool(Walk, true); // Loops a movement animation
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

				var c = CoreRigidbody.GetComponentInChildren<Collider>();
				if (c) c.enabled = true;
			}

			if (CoreRigidbody) CoreRigidbody.gameObject.SetActive(true);
		}
	}
}
