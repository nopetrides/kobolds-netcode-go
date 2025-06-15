using System.Threading.Tasks;
using Kobold.Input;
using Kobold.UI;
using Kobolds;
using UnityEngine;

namespace Kobold.GameManagement
{
    /// <summary>
    /// Handles main menu logic and scene transitions
    /// Works with the new KoboldUIController system
    /// </summary>
    public class KoboldMainMenuHandler : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private KoboldUIController _uiController;
        
        private bool _isConnecting;
        
        private void Awake()
        {
            // Find UI Controller if not assigned
            if (_uiController == null)
            {
                _uiController = FindFirstObjectByType<KoboldUIController>();
                if (_uiController == null)
                {
                    Debug.LogError($"[{name}] No KoboldUIController found in scene!");
                }
            }
        }
        
        private void Start()
        {
            // Ensure we're in UI mode for the menu
            if (KoboldInputSystemManager.Instance != null) 
            {
                KoboldInputSystemManager.Instance.EnableUIMode();
            }
            
            // Subscribe to UI events from the EventHandler
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            // UI Events
            KoboldEventHandler.OnStartSocialHubPressed += OnStartSocialHubRequested;
            KoboldEventHandler.OnQuickMissionPressed += OnQuickMissionRequested;
            
            // Connection completion events
            KoboldEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
            KoboldEventHandler.OnSocialHubConnectionCompleted += OnSocialHubConnectionCompleted;
            KoboldEventHandler.OnMissionConnectionCompleted += OnMissionConnectionCompleted;
        }
        
        private void UnsubscribeFromEvents()
        {
            // UI Events
            KoboldEventHandler.OnStartSocialHubPressed -= OnStartSocialHubRequested;
            KoboldEventHandler.OnQuickMissionPressed -= OnQuickMissionRequested;
            
            // Connection completion events
            KoboldEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
            KoboldEventHandler.OnSocialHubConnectionCompleted -= OnSocialHubConnectionCompleted;
            KoboldEventHandler.OnMissionConnectionCompleted -= OnMissionConnectionCompleted;
        }
        
        private void OnStartSocialHubRequested(string playerName, string sessionName)
        {
            if (_isConnecting)
            {
                Debug.LogWarning($"[{name}] Already connecting to a session");
                return;
            }
            
            _isConnecting = true;
            Debug.Log($"[{name}] Starting Social Hub connection - Player: {playerName}, Session: {sessionName}");
            
            // TODO: Start your actual connection logic here
            // For example:
            // NetworkManager.Instance.ConnectToSocialHub(playerName, sessionName);
            
            // Simulate connection for testing
            StartCoroutine(SimulateConnection(true, sessionName));
        }
        
        private void OnQuickMissionRequested(string playerName, string missionType)
        {
            if (_isConnecting)
            {
                Debug.LogWarning($"[{name}] Already connecting to a session");
                return;
            }
            
            _isConnecting = true;
            Debug.Log($"[{name}] Starting Quick Mission - Player: {playerName}, Type: {missionType}");
            
            // TODO: Start your actual mission connection logic here
            // For example:
            // NetworkManager.Instance.QuickJoinMission(playerName, missionType);
            
            // Simulate connection for testing
            StartCoroutine(SimulateConnection(false, missionType));
        }
        
        private System.Collections.IEnumerator SimulateConnection(bool isSocialHub, string sessionName)
        {
            // Simulate connection delay
            yield return new WaitForSeconds(2f);
            
            // Simulate successful connection
            var task = Task.CompletedTask;
            
            if (isSocialHub)
            {
                OnSocialHubConnectionCompleted(task, sessionName);
            }
            else
            {
                OnMissionConnectionCompleted(task, sessionName);
            }
        }
        
        private void OnConnectToSessionCompleted(Task task, string sessionName)
        {
            _isConnecting = false;
            
            try
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"[{name}] Connected to session: {sessionName}");
                    // Default behavior - assume social hub
                    KoboldEventHandler.LoadInGameScene(nameof(SceneNames.KoboldHub));
                }
                else
                {
                    Debug.LogError($"[{name}] Failed to connect to session: {sessionName}");
                    ShowConnectionError();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{name}] Error handling connection completion: {e}");
                ShowConnectionError();
            }
        }
        
        private void OnSocialHubConnectionCompleted(Task task, string sessionName)
        {
            _isConnecting = false;
            
            try
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"[{name}] Connected to Social Hub: {sessionName}");
                    KoboldEventHandler.LoadInGameScene(nameof(SceneNames.KoboldHub));
                }
                else
                {
                    Debug.LogError($"[{name}] Failed to connect to Social Hub: {sessionName}");
                    ShowConnectionError();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{name}] Error handling Social Hub connection: {e}");
                ShowConnectionError();
            }
        }
        
        private void OnMissionConnectionCompleted(Task task, string sessionName)
        {
            _isConnecting = false;
            
            try
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"[{name}] Connected to Mission: {sessionName}");
                    // Load mission scene or mission lobby
                    KoboldEventHandler.LoadMissionScene(nameof(SceneNames.KoboldMission));
                }
                else
                {
                    Debug.LogError($"[{name}] Failed to connect to Mission: {sessionName}");
                    ShowConnectionError();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{name}] Error handling Mission connection: {e}");
                ShowConnectionError();
            }
        }
        
        private void ShowConnectionError()
        {
            _isConnecting = false;
            
            // TODO: Show error UI
            // For now, just re-enable the UI
            Debug.LogError($"[{name}] Connection failed - implement error UI");
        }
    }
}