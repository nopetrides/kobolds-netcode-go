using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kobold.UI
{
	public class MainMenuPhysicsLoop : MonoBehaviour
	{
		[SerializeField] private RagdollAnimator2 PhysicsPrefab;
		[SerializeField] private Transform PhysicsSpawnPoint;
		[SerializeField] private float PhysicsFloor = -10f;
		[SerializeField] private float MinSpawnInterval = 1f;
		
		private List<RagdollAnimator2> _physicsObjects = new();

		private float _spawnInterval;
		private float _timer;
		
		// Start is called before the first frame update
		void Start()
		{
			_spawnInterval = float.MaxValue;
			_physicsObjects.Add(Instantiate(PhysicsPrefab, PhysicsSpawnPoint.position, Random.rotation));
		}

		private void Update()
		{
			foreach (var obj in _physicsObjects)
			{
				if (obj.transform.position.y < PhysicsFloor)
				{
					obj.User_SetAllVelocity(Vector3.zero);
					obj.User_SetAllBonesVelocity(Vector3.zero);
					var rb = obj.GetComponent<Rigidbody>();
					rb.linearVelocity = Vector3.zero;
					rb.angularVelocity = Vector3.zero;
					obj.transform.position = PhysicsSpawnPoint.position;
					obj.User_Teleport();
					_spawnInterval = Mathf.Max(_timer/1.1f, MinSpawnInterval);
					_timer = 0f;
				}
			}

			if (_timer > _spawnInterval)
			{
				_physicsObjects.Add(Instantiate(PhysicsPrefab, PhysicsSpawnPoint.position, Random.rotation));
				_timer = 0f;
			}

			_timer += Time.deltaTime;
		}
		
		/*
		private IEnumerator Teleport(RagdollAnimator2 ragdoll)
		{
			// Step 1: Set kinematic to stop physics simulation
			ragdoll.TeHandler.SetAllBodiesKinematic(true);

			// Step 2: Optionally disable collisions during teleport
			ragdoll.Handler.DisableAllColliders();

			// Step 3: Teleport the character and dummy root
			var root = ragdoll.transform;
			root.position = targetPosition;
			root.rotation = targetRotation;

			// Step 4: Immediately apply transforms to dummy bones
			ragdoll.User_UpdateAllBonesParametersAfterManualChanges();

			// Wait one physics frame to let Unity settle the kinematics
			yield return new WaitForFixedUpdate();

			// Step 5: Re-enable physics and collisions
			ragdoll.Handler.EnableAllColliders();
			ragdoll.Handler.SetAllBodiesKinematic(false);

			// Optional: reset velocity if needed
			ragdoll.Handler.GetAnchorBoneController.MainRigidbody.velocity = Vector3.zero;

			// If desired, force physical matching
			ragdoll.User_FadeMusclesPowerMultiplicator(1f, 0.05f);
		}
		*/
	}
}
