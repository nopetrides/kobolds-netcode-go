using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI
{
    /// <summary>
    /// Simple test script to verify UI navigation is working
    /// </summary>
    public class KoboldUINavigationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _enableTestLogging = true;
        
        private void Start()
        {
            if (_enableTestLogging)
            {
                Debug.Log("[KoboldUINavigationTest] UI Navigation Test started");
                
                // Test navigation manager
                if (KoboldUINavigationManager.Instance != null)
                {
                    Debug.Log("[KoboldUINavigationTest] Navigation Manager found and ready");
                }
                else
                {
                    Debug.LogError("[KoboldUINavigationTest] Navigation Manager not found!");
                }
                
                // Test input system
                if (KoboldInputSystemManager.Instance != null)
                {
                    Debug.Log("[KoboldUINavigationTest] Input System Manager found and ready");
                    Debug.Log($"[KoboldUINavigationTest] Current input mode: {(KoboldInputSystemManager.Instance.IsInUIMode ? "UI" : "Gameplay")}");
                }
                else
                {
                    Debug.LogError("[KoboldUINavigationTest] Input System Manager not found!");
                }
            }
        }
        
        private void Update()
        {
            if (!_enableTestLogging) return;
            
            // Test keyboard input
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("[KoboldUINavigationTest] Up arrow pressed");
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("[KoboldUINavigationTest] Down arrow pressed");
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("[KoboldUINavigationTest] Left arrow pressed");
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log("[KoboldUINavigationTest] Right arrow pressed");
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[KoboldUINavigationTest] Submit key pressed");
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[KoboldUINavigationTest] Cancel key pressed");
            }
        }
    }
} 