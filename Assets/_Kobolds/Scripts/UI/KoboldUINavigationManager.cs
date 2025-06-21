using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Kobold.UI
{
    /// <summary>
    /// Manages UI navigation for keyboard/gamepad input
    /// </summary>
    public class KoboldUINavigationManager : MonoBehaviour
    {
        [Header("Navigation Settings")]
        [SerializeField] private float _navigationRepeatDelay = 0.5f;
        [SerializeField] private float _navigationRepeatRate = 0.1f;
        
        private UIDocument _currentUIDocument;
        private VisualElement _currentFocusElement;
        private float _lastNavigationTime;
        private Vector2 _lastNavigationInput;
        
        public static KoboldUINavigationManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern with DontDestroyOnLoad
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Initialize navigation manager
            Debug.Log("[KoboldUINavigationManager] Navigation manager initialized");
        }
        
        public void SetCurrentUIDocument(UIDocument document)
        {
            _currentUIDocument = document;
            Debug.Log($"[KoboldUINavigationManager] Set current UI document: {document?.name}");
        }
        
        public void HandleNavigationInput(Vector2 input)
        {
            if (_currentUIDocument == null) return;
            
            var currentTime = Time.unscaledTime;
            
            // Check if we should process navigation (repeat delay)
            if (currentTime - _lastNavigationTime < _navigationRepeatDelay)
            {
                return;
            }
            
            // Process navigation input
            if (Mathf.Abs(input.x) > 0.5f || Mathf.Abs(input.y) > 0.5f)
            {
                NavigateUI(input);
                _lastNavigationTime = currentTime;
                _lastNavigationInput = input;
            }
        }
        
        private void NavigateUI(Vector2 direction)
        {
            var root = _currentUIDocument.rootVisualElement;
            var currentFocus = root.focusController.focusedElement;
            
            if (currentFocus == null)
            {
                // Find first focusable element
                var firstFocusable = FindFirstFocusableElement(root);
                if (firstFocusable != null)
                {
                    firstFocusable.Focus();
                }
                return;
            }
            
            // Find next focusable element in direction
            var nextElement = FindNextFocusableElement(currentFocus, direction);
            if (nextElement != null)
            {
                nextElement.Focus();
            }
        }
        
        private Focusable FindFirstFocusableElement(VisualElement root)
        {
            // Find first button or other focusable element
            var buttons = root.Query<Button>().ToList();
            return buttons.Count > 0 ? buttons[0] : null;
        }
        
        private Focusable FindNextFocusableElement(Focusable current, Vector2 direction)
        {
            // Simple implementation - you can enhance this with more sophisticated navigation logic
            var currentElement = current as VisualElement;
            if (currentElement == null) return null;
            
            var parent = currentElement.parent;
            if (parent == null) return null;
            
            var siblings = parent.Query<Button>().ToList();
            var currentIndex = siblings.IndexOf(currentElement as Button);
            
            if (currentIndex == -1) return null;
            
            // Simple grid navigation (assumes buttons are in a vertical list)
            if (direction.y > 0.5f) // Down
            {
                var nextIndex = (currentIndex + 1) % siblings.Count;
                return siblings[nextIndex];
            }
            else if (direction.y < -0.5f) // Up
            {
                var prevIndex = (currentIndex - 1 + siblings.Count) % siblings.Count;
                return siblings[prevIndex];
            }
            
            return null;
        }
        
        public void SetInitialFocus(UIDocument document)
        {
            if (document?.rootVisualElement == null) return;
            
            var firstButton = FindFirstFocusableElement(document.rootVisualElement);
            if (firstButton != null)
            {
                firstButton.Focus();
                Debug.Log($"[KoboldUINavigationManager] Set initial focus to: {firstButton}");
            }
        }
    }
} 