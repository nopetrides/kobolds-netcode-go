using Unity.Cinemachine;
using UnityEngine;

// Manages character-related logic like spawning and initialization.
public class CharacterManager : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;

    public void SpawnCharacter()
    {
        Vector3 spawnPosition = Vector3.zero;
        Instantiate(characterPrefab, spawnPosition, Quaternion.identity, transform);
    }
}
