using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;


namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class DamageReceiver : NetworkBehaviour, IDamageable
    {
        public event Action<ServerCharacter, int> DamageReceived;

        public event Action<Collision> CollisionEntered;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public void ReceiveHp(ServerCharacter inflicter, int hp)
        {
            if (IsDamageable())
            {
                DamageReceived?.Invoke(inflicter, hp);
            }
        }

		public Transform Transform { get; }

		public IDamageable.SpecialDamageFlags GetSpecialDamageFlags()
        {
            return IDamageable.SpecialDamageFlags.None;
        }

        public bool IsDamageable()
        {
            return m_NetworkLifeState.LifeState.Value == LifeState.Alive;
        }

        void OnCollisionEnter(Collision other)
        {
            CollisionEntered?.Invoke(other);
        }
    }
}
