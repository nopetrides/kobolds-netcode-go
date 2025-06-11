using UnityEngine;
using UnityEngine.SceneManagement;

// Responsible for loading levels and managing level-related logic.
public class LevelManager : MonoBehaviour
{
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
    public void LoadLevelAdditively(string levelName)
    {
        SceneManager.LoadScene(levelName, LoadSceneMode.Additive);
    }
}