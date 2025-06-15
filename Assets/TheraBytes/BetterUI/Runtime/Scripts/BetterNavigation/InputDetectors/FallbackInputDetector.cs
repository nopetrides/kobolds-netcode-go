using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace TheraBytes.BetterUi
{
    public class FallbackInputDetector : IInputDetector
    {
        NavigationInfo currentNavigationInfo;
        MoveDirection navigationDirection;

        public NavigationInfo CurrentNavigationInfo { get { return currentNavigationInfo; } }
        public MoveDirection NavigationDirection { get { return navigationDirection; } }

        public float InitialRepeatDelay { get { return 0.5f; } }
        public float ConsecutiveRepeatDelay { get { return 0.1f; } }

        public BaseInputModule InputModule => EventSystem.current?.currentInputModule;

        public bool LastInputWasGamepad => false;

        public FallbackInputDetector() 
        {
#if !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError("The Fallback Input Detector only works with the legacy input module (unless you are using Player Mice)");
#endif
        }

        public void UpdateCurrentNavigationData()
        {
            currentNavigationInfo = GetNavigationInfo();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        static readonly Dictionary<KeyCode, Vector2> navigationKeyCodes = new Dictionary<KeyCode, Vector2>
            {
                { KeyCode.W, Vector2.up }, 
                { KeyCode.A, Vector2.left }, 
                { KeyCode.S, Vector2.down },
                { KeyCode.D, Vector2.right },
                { KeyCode.UpArrow, Vector2.up },
                { KeyCode.LeftArrow, Vector2.left },
                { KeyCode.DownArrow, Vector2.down },
                { KeyCode.RightArrow, Vector2.right },
            };

        static readonly KeyCode[] submitKeyCodes = new KeyCode[]
            {
                KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space,
            };

        static readonly KeyCode[] cancelKeyCodes = new KeyCode[]
            {
                KeyCode.Escape,
            };

        static readonly string[] submitButtonNames = new string[]
        {
            "Submit",
        };

        static readonly string[] cancelButtonNames = new string[]
        {
            "Cancel",
        };

        static readonly Dictionary<string, int> axisNames = new Dictionary<string, int>()
        {
            { "Horizontal", 0 }, 
            { "Vertical", 1 },
        };

        Vector3 lastMousePosition;


        NavigationInfo GetNavigationInfo()
        {
            // Controller
            var action = GetButtonNavigationMode()
                .GetCombinationWithPreviousFrame(InputDeviceType.DirectionDevice, currentNavigationInfo, false);

            if (action != InputActionType.None)
                return new NavigationInfo(InputDeviceType.DirectionDevice, action, navigationDirection);

            // Keyboard
            action = GetKeyNavigationMode()
                .GetCombinationWithPreviousFrame(InputDeviceType.DirectionDevice, currentNavigationInfo, false);

            if (action != InputActionType.None)
                return new NavigationInfo(InputDeviceType.DirectionDevice, action, navigationDirection);

            // Mouse
            action = GetMouseNavigationMode()
                .GetCombinationWithPreviousFrame(InputDeviceType.PointerDevice, currentNavigationInfo, false);

            if (action != InputActionType.None)
                return new NavigationInfo(InputDeviceType.PointerDevice, action);

            // Touch
            action = GetTouchNavigationMode()
                .GetCombinationWithPreviousFrame(InputDeviceType.PointerDevice, currentNavigationInfo, false);

            if (action != InputActionType.None)
                return new NavigationInfo(InputDeviceType.PointerDevice, action);

            return new NavigationInfo();
        }

        InputActionType GetButtonNavigationMode()
        {
            navigationDirection = MoveDirection.None;
            foreach (var btn in submitButtonNames)
            {
                if (Input.GetButton(btn))
                    return InputActionType.Submit;
            }

            foreach (var btn in cancelButtonNames)
            {
                if (Input.GetButton(btn))
                    return InputActionType.Cancel;
            }

            const float epsilon = 0.1f;
            Vector2 dir = new Vector2();
            foreach (var axis in axisNames)
            {
                float amount = Input.GetAxisRaw(axis.Key);
                if (Mathf.Abs(amount) > epsilon)
                {
                    dir[axis.Value] += amount;
                }
            }

            if(dir.sqrMagnitude > epsilon)
            {
                navigationDirection = NavigationHelper.ToMoveDirection(dir);
                return InputActionType.NavigateInAnyDirection;
            }

            return InputActionType.None;
        }

        InputActionType GetKeyNavigationMode()
        {
            navigationDirection = MoveDirection.None;
            foreach (var key in submitKeyCodes)
            {
                if (Input.GetKey(key))
                    return InputActionType.Submit;
            }

            foreach (var key in cancelKeyCodes)
            {
                if (Input.GetKey(key))
                    return InputActionType.Cancel;
            }

            Vector2 dir = new Vector2();
            foreach (var key in navigationKeyCodes)
            {
                if (Input.GetKey(key.Key))
                {
                    dir += key.Value;
                }
            }

            if(dir != Vector2.zero)
            {
                navigationDirection = NavigationHelper.ToMoveDirection(dir);
                return InputActionType.NavigateInAnyDirection;
            }

            return InputActionType.None;
        }

        InputActionType GetMouseNavigationMode()
        {
            if (!Input.mousePresent)
                return InputActionType.None;

            // movement
            Vector3 prev = lastMousePosition;
            lastMousePosition = Input.mousePosition;

            if (prev != Vector3.zero)
            {
                var mouseDelta = Input.mousePosition - prev;
                if (mouseDelta.sqrMagnitude > 0.0f)
                    return InputActionType.PointerPositionChanged;
            }

            // mouse clicks
            if (Input.GetMouseButton(0))
                return InputActionType.Submit;

            return InputActionType.None;
        }

        InputActionType GetTouchNavigationMode()
        {
            if (Input.touchCount > 0)
            {
                InputActionType touchResult = InputActionType.None;

                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

#if UNITY_5_3_OR_NEWER
                    if (touch.type == TouchType.Indirect)
                        continue;
#endif
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            touchResult |= InputActionType.Began;
                            break;
                        case TouchPhase.Moved:
                            touchResult |= InputActionType.PointerPositionChanged;
                            break;
                        case TouchPhase.Stationary:
                            touchResult |= InputActionType.Submit;
                            break;
                        case TouchPhase.Ended:
                            touchResult |= InputActionType.Ended;
                            break;
                        case TouchPhase.Canceled:
                            touchResult |= InputActionType.Cancel;
                            break;
                    }
                }

                return touchResult;
            }

            return InputActionType.None;
        }
#else
        NavigationInfo GetNavigationInfo()
        {
            return new NavigationInfo();
        }
#endif
    }
}
