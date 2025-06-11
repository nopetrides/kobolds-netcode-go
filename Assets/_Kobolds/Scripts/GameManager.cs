using UnityEngine;

// The main game controller that orchestrates all subsystems.
public class GameManager : MonoBehaviour
{
    // Singleton pattern to ensure only one GameManager exists.
    public static GameManager Instance { get; private set; }

    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private LevelManager levelManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        levelManager.LoadLevelAdditively("SimpleLevel");
        characterManager.SpawnCharacter();
    }
}