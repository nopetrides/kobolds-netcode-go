using UnityEngine;
using Kobold.GameManagement;
using Kobolds;

namespace Kobold.Input
{
    public class KoboldInputSystemManager : MonoBehaviour
    {
        public static KoboldInputSystemManager Instance { get; private set; }
		
		public KoboldInputs Inputs { get; private set; }
        
        [Header("Cursor Settings")]
        [SerializeField] bool _startInGameplayMode = false;
        
        private bool _isInUIMode = true;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
				Inputs = gameObject.GetComponent<KoboldInputs>();
			}
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            // Subscribe to scene/state events
            KoboldEventHandler.OnConnectToSessionCompleted += OnSessionConnected;
            KoboldEventHandler.OnExitedSession += OnSessionExited;
            
            // Start in appropriate mode
            if (_startInGameplayMode)
                EnableGameplayMode();
            else
                EnableUIMode();
        }
        
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            KoboldEventHandler.OnConnectToSessionCompleted -= OnSessionConnected;
            KoboldEventHandler.OnExitedSession -= OnSessionExited;
        }
        
        public void EnableUIMode()
        {
            _isInUIMode = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // You could add an event here to notify KoboldInputs components
            // KoboldEventHandler.InputModeChanged?.Invoke(false);
        }
        
        public void EnableGameplayMode()
        {
            _isInUIMode = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // You could add an event here to notify KoboldInputs components
            // KoboldEventHandler.InputModeChanged?.Invoke(true);
        }
        
        public void ToggleMode()
        {
            if (_isInUIMode)
                EnableGameplayMode();
            else
                EnableUIMode();
        }
        
        // Automatically switch modes based on game state
        void OnSessionConnected(System.Threading.Tasks.Task task, string sessionName)
        {
            if (task.IsCompletedSuccessfully)
            {
                // We're in game now, enable gameplay mode
                EnableGameplayMode();
            }
        }
        
        void OnSessionExited()
        {
            // Back to menu, enable UI mode
            EnableUIMode();
        }
        
        // Optional: Handle escape key for pause menu
        void Update()
        {
            if (Inputs.Escape)
            {
                ToggleMode();
            }
        }
        
        public bool IsInUIMode => _isInUIMode;
        public bool IsInGameplayMode => !_isInUIMode;
    }
}