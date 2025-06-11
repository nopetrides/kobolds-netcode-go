using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kobolds
{
	public class BulletManager : MonoBehaviour
	{
        [Header("External Scripts")]
		[SerializeField] private Camera Cam;
        [SerializeField] private KoboldInputs Inputs;

        [Header("Bullets")]
		[FormerlySerializedAs("BulletPrefab")] 
        [SerializeField] private PhysicsBullet PhysicsBulletPrefab;

        [Header("Raycast")]
        
        [SerializeField] private RaycastBullet BulletParticle;
        [SerializeField] private LayerMask RaycastMask;
        [SerializeField] private ShootType ShootingCalculation;

        public enum ShootType
        {
            Raycast = 0,
            Physics = 1,
        }

		private void Update()
		{
			if (Inputs)
			{
				if (Inputs.Aim && Inputs.Fire) OnFirePressed();
				Inputs.Fire = false;
			}
			else
			{
				if (Input.GetKey(KeyCode.Mouse1) && Input.GetKeyDown(KeyCode.Mouse0)) OnFirePressed();
			}
		}

		private void OnFirePressed()
		{
            Debug.Log("Firing projectile");
            switch (ShootingCalculation)
            {
                case ShootType.Raycast:
                    DoRaycastShot();
                    break;
                case ShootType.Physics:
                    SpawnPhysicsBullet();
                    break;
                default:
                    Debug.LogError("Unexpected value");
                    break;
            }
		}

        private void SpawnPhysicsBullet()
        {
            // does not call collision until physics system collides

            PhysicsBullet spawnedBullet = Instantiate(PhysicsBulletPrefab, Cam.transform.position, Cam.transform.rotation);
            spawnedBullet.Initialize(this);
        }

        private void DoRaycastShot()
        {
            if (Physics.Raycast(
                    Cam.transform.position,
                    Cam.transform.forward, 
                    out RaycastHit hit,
                    Mathf.Infinity, 
                    RaycastMask))
            {
                Debug.Log("Raycast Hit!");
                OnProjectileCollision(hit.point, hit.normal);
            }
            else
            {
                Debug.Log("Raycast Miss");
            }
        }

        public void OnProjectileCollision(Vector3 position, Vector3 rotation)
        {
            // do stuff
            
            
            SpawnParticle(position, rotation);
        }

        private void SpawnParticle(Vector3 position, Vector3 rotation)
        {
                
            Instantiate(BulletParticle, position, Quaternion.Euler(rotation));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (Inputs && Inputs.Aim)
                Gizmos.DrawLine(Cam.transform.position, Cam.transform.position + Cam.transform.forward * 100);
        }

        private void CleanupParticle()
        {
            
        }
    }
}
