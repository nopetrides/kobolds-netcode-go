using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Kobold.UI
{
    /// <summary>
    /// Handles UI-specific input actions
    /// </summary>
    public class KoboldUIInputHandler : MonoBehaviour
    {
        [Header("UI Navigation")]
        [SerializeField] private InputActionReference _navigateAction;
        [SerializeField] private InputActionReference _submitAction;
        [SerializeField] private InputActionReference _cancelAction;
        [SerializeField] private InputActionReference _pointAction;
        [SerializeField] private InputActionReference _clickAction;
        
        private UIDocument _currentDocument;
        private bool _isInitialized = false;
        
        private void OnEnable()
        {
            // Enable UI actions if they exist
            _navigateAction?.action.Enable();
            _submitAction?.action.Enable();
            _cancelAction?.action.Enable();
            _pointAction?.action.Enable();
            _clickAction?.action.Enable();
            
            // Subscribe to events
            if (_navigateAction) _navigateAction.action.performed += OnNavigate;
			if (_submitAction) _submitAction.action.performed += OnSubmit;
			if (_cancelAction) _cancelAction.action.performed += OnCancel;
            
            Debug.Log("[KoboldUIInputHandler] UI input handler enabled");
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
			if (_navigateAction) _navigateAction.action.performed -= OnNavigate;
			if (_submitAction) _submitAction.action.performed -= OnSubmit;
			if (_cancelAction) _cancelAction.action.performed -= OnCancel;
            
            // Disable actions
            _navigateAction?.action.Disable();
            _submitAction?.action.Disable();
            _cancelAction?.action.Disable();
            _pointAction?.action.Disable();
            _clickAction?.action.Disable();
            
            Debug.Log("[KoboldUIInputHandler] UI input handler disabled");
        }
        
        public void SetCurrentDocument(UIDocument document)
        {
            _currentDocument = document;
            KoboldUINavigationManager.Instance?.SetCurrentUIDocument(document);
            
            // Set initial focus after a short delay to ensure UI is ready
            if (!_isInitialized)
            {
                StartCoroutine(SetInitialFocusDelayed());
                _isInitialized = true;
            }
        }
        
        private System.Collections.IEnumerator SetInitialFocusDelayed()
        {
            yield return new WaitForEndOfFrame();
            KoboldUINavigationManager.Instance?.SetInitialFocus(_currentDocument);
        }
        
        private void OnNavigate(InputAction.CallbackContext context)
        {
            var input = context.ReadValue<Vector2>();
            KoboldUINavigationManager.Instance?.HandleNavigationInput(input);
        }
        
        private void OnSubmit(InputAction.CallbackContext context)
        {
            // Handle submit (Enter/Space/Gamepad A)
            var root = _currentDocument?.rootVisualElement;
            if (root == null) return;
            
            var focusedElement = root.focusController.focusedElement;
            if (focusedElement is Button button)
            {
                Debug.Log($"[KoboldUIInputHandler] Submit pressed on button: {button.name}");
				using var e = ClickEvent.GetPooled();
				e.target = button;
				button.SendEvent(e);
			}
        }
        
        private void OnCancel(InputAction.CallbackContext context)
        {
            // Handle cancel (Escape/Gamepad B)
            Debug.Log("[KoboldUIInputHandler] Cancel pressed");
            
            // You can implement back navigation or close current window here
            // For now, just log it
        }
        
        // Fallback method for when input actions aren't configured
        private void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            // Handle keyboard navigation as fallback
            if (_currentDocument == null) return;
            var input = Vector2.zero;
            
            // Check for arrow key input
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                input.y = 1f;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                input.y = -1f;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                input.x = -1f;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                input.x = 1f;
            
            if (input != Vector2.zero)
            {
                KoboldUINavigationManager.Instance?.HandleNavigationInput(input);
            }
            
            // Handle submit with Enter or Space
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                OnSubmit(new InputAction.CallbackContext());
            }
            
            // Handle cancel with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancel(new InputAction.CallbackContext());
            }
#endif
        }
    }
}