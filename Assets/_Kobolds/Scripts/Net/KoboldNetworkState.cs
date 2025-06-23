using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Kobold.Net
{
    /// <summary>
    /// Container for Kobold's networked state data.
    /// Efficiently packs state information for network transmission.
    /// </summary>
    public struct KoboldNetworkState : INetworkSerializable
    {
        /// <summary>
        /// Current gameplay state of the Kobold.
        /// </summary>
        public KoboldState State;
        
        /// <summary>
        /// Current health points.
        /// </summary>
        public float Health;
        
        /// <summary>
        /// Maximum health points.
        /// </summary>
        public float MaxHealth;
		
        /// <summary>
        /// Player's display name.
        /// </summary>
        public FixedString64Bytes PlayerName;
        
        /// <summary>
        /// NetworkObjectReference to the currently grabbed object (if any).
        /// Invalid reference means no object is grabbed.
        /// </summary>
        public NetworkObjectReference GrabbedObject;
        
        /// <summary>
        /// NetworkObjectReference to the object being latched to (if any).
        /// Invalid reference means not latched.
        /// </summary>
        public NetworkObjectReference LatchTarget;
        
        /// <summary>
        /// Local position relative to the latch target.
        /// Only valid when LatchTarget is valid.
        /// </summary>
        public Vector3 LatchLocalPosition;
        
        /// <summary>
        /// Local rotation relative to the latch target.
        /// Only valid when LatchTarget is valid.
        /// </summary>
        public Quaternion LatchLocalRotation;

        /// <summary>
        /// Current latch state for immediate network synchronization.
        /// </summary>
        public LatchState LatchState;

        /// <summary>
        /// Index of the collider on the latch target (if networked object).
        /// </summary>
        public int LatchColliderIndex;
        /// <summary>
        /// World position of the latch point (if static geometry).
        /// </summary>
        public Vector3 LatchWorldPosition;
        /// <summary>
        /// World rotation of the latch point (if static geometry).
        /// </summary>
        public Quaternion LatchWorldRotation;
        /// <summary>
        /// True if latching to a networked object, false if static geometry.
        /// </summary>
        public bool LatchIsNetworked;

        /// <summary>
        /// Creates a default initialized state for a new player.
        /// </summary>
        public static KoboldNetworkState CreateDefault()
        {
            return new KoboldNetworkState
            {
                State = KoboldState.Unburying,
                Health = 100f,
                MaxHealth = 100f,
                PlayerName = "Kobold",
                GrabbedObject = new NetworkObjectReference(),
                LatchTarget = new NetworkObjectReference(),
                LatchLocalPosition = Vector3.zero,
                LatchLocalRotation = Quaternion.identity,
                LatchColliderIndex = -1,
                LatchWorldPosition = Vector3.zero,
                LatchWorldRotation = Quaternion.identity,
                LatchIsNetworked = false,
                LatchState = Kobold.LatchState.None
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref State);
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref MaxHealth);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref GrabbedObject);
            serializer.SerializeValue(ref LatchTarget);
            serializer.SerializeValue(ref LatchLocalPosition);
            serializer.SerializeValue(ref LatchLocalRotation);
            serializer.SerializeValue(ref LatchColliderIndex);
            serializer.SerializeValue(ref LatchWorldPosition);
            serializer.SerializeValue(ref LatchWorldRotation);
            serializer.SerializeValue(ref LatchIsNetworked);
            serializer.SerializeValue(ref LatchState);
        }
    }
}