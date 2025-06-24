using UnityEngine;
using Unity.Netcode;

public class LatchableColliderIndexer : NetworkBehaviour {
    public Collider[] Colliders { get; private set; }

    void Awake() {
        Colliders = GetComponentsInChildren<Collider>();
    }

    public int GetColliderIndex(Collider col) {
        for (int i = 0; i < Colliders.Length; i++) {
            if (Colliders[i] == col) return i;
        }
        return -1;
    }

    public Collider GetColliderByIndex(int idx) {
        if (idx >= 0 && idx < Colliders.Length) return Colliders[idx];
        return null;
    }
} 