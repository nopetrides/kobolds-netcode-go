using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Wrapper class for direct references to components relevant to physics.
    /// Each instance of a PhysicsWrapper is registered to a static dictionary, indexed by the NetworkObject's ID.
    /// </summary>
    /// <remarks>
    /// The root GameObject of PCs & NPCs is not the object which will move through the world, so other classes will
    /// need a quick reference to a PC's/NPC's in-game position.
    /// </remarks>
    public class PhysicsWrapper : NetworkBehaviour
    {
        static Dictionary<ulong, PhysicsWrapper> _mPhysicsWrappers = new Dictionary<ulong, PhysicsWrapper>();

        [SerializeField]
        Transform m_Transform;

        public Transform Transform => m_Transform;

        [SerializeField]
        Collider m_DamageCollider;

        public Collider DamageCollider => m_DamageCollider;

        ulong _mNetworkObjectID;

        public override void OnNetworkSpawn()
        {
            _mPhysicsWrappers.Add(NetworkObjectId, this);

            _mNetworkObjectID = NetworkObjectId;
        }

        public override void OnNetworkDespawn()
        {
            RemovePhysicsWrapper();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RemovePhysicsWrapper();
        }

        void RemovePhysicsWrapper()
        {
            _mPhysicsWrappers.Remove(_mNetworkObjectID);
        }

        public static bool TryGetPhysicsWrapper(ulong networkObjectID, out PhysicsWrapper physicsWrapper)
        {
            return _mPhysicsWrappers.TryGetValue(networkObjectID, out physicsWrapper);
        }
    }
}
