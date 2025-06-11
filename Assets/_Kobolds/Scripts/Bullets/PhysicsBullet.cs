using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kobolds
{
    public class PhysicsBullet : MonoBehaviour
    {
        [SerializeField] private float ProjectileSpeed;

        [SerializeField] private float ProjectileDamage;

        [SerializeField] private Rigidbody Rb;
        
        private BulletManager _bulletManager;

        public void Initialize(BulletManager manager)
        {
            _bulletManager = manager;
        }

        // Start is called before the first frame update
        void Start()
        {
            // Add force once on projectile spawn
            Rb.AddForce(transform.forward * ProjectileSpeed, ForceMode.Impulse);
        }

        void OnCollisionEnter(Collision collision)
        {
            ContactPoint contact = collision.GetContact(0);
            _bulletManager.OnProjectileCollision(contact.point, contact.normal);
            Destroy(gameObject);
        }
    }
}