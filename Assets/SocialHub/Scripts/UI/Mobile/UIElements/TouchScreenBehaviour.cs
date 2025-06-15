using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// This class is managing the data binding between the TouchScreen
    /// </summary>
    class TouchScreenBehaviour : MonoBehaviour
    {
        static class UIElementNames
        {
            internal const string JoystickMove = "JoystickMove";
            internal const string JoystickLook = "JoystickLook";
            internal const string ButtonMenu = "ButtonMenu";
            internal const string ButtonJump = "ButtonJump";
            internal const string ButtonSprint = "ButtonSprint";
            internal const string ButtonInteract = "ButtonInteract";
            internal const string PlayerContainer = "PlayerContainer";
        }

        [SerializeField]
        UIDocument m_Document;

        [SerializeField]
        VisualTreeAsset m_TouchscreenUI;

        VirtualJoystick _mJoystickLeft;
        VirtualJoystick _mJoystickRight;
        MobileGamepadState _mRuntimeState;
        PressedButton _mButtonInteract;

        async void OnEnable()
        {
            var isMobile = await InputSystemManager.IsMobile;
            if (!isMobile)
            {
                return;
            }

            var root = m_Document.rootVisualElement.Q("touch-ui-container");
            m_TouchscreenUI.CloneTree(root);
            _mRuntimeState = MobileGamepadState.GetOrCreate;

            // Bindings
            root.dataSource = _mRuntimeState;
            var joystickMove = root.Q<VisualElement>(UIElementNames.JoystickMove);
            _mJoystickLeft = new VirtualJoystick(joystickMove, OnJoystickLeftMoved,
                _mRuntimeState.LeftJoystickTopName, _mRuntimeState.LeftJoystickLeftName);
            var joystickLook = root.Q<VisualElement>(UIElementNames.JoystickLook);
            _mJoystickRight = new VirtualJoystick(joystickLook, OnJoystickRightMoved,
                _mRuntimeState.RightJoystickTopName, _mRuntimeState.RightJoystickLeftName);

            var buttonMenu = root.Q<PressedButton>(UIElementNames.ButtonMenu);
            buttonMenu.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonMenu)),
                bindingMode = BindingMode.ToSource,
            });

            var buttonJump = root.Q<PressedButton>(UIElementNames.ButtonJump);
            buttonJump.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonJump)),
                bindingMode = BindingMode.ToSource,
            });

            _mButtonInteract = root.Q<PressedButton>(UIElementNames.ButtonInteract);
            _mButtonInteract.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonInteract)),
                bindingMode = BindingMode.ToSource,
            });

            var buttonSprint = root.Q<Toggle>(UIElementNames.ButtonSprint);
            buttonSprint.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonSprint)),
                bindingMode = BindingMode.ToSource,
            });

            GameplayEventHandler.OnPickupStateChanged += OnPickupStateChanged;
        }

        void OnPickupStateChanged(PickupState state, Transform _)
        {
            _mButtonInteract.enabledSelf = state != PickupState.Inactive;

            if(state == PickupState.Carry)
            {
                _mButtonInteract.AddToClassList("state-carry");
                return;
            }

            _mButtonInteract.RemoveFromClassList("state-carry");
        }

        void OnJoystickLeftMoved(Vector2 position) => _mRuntimeState.LeftJoystick = position;
        void OnJoystickRightMoved(Vector2 position) => _mRuntimeState.RightJoystick = position;

        void OnDisable()
        {
            _mJoystickLeft?.Dispose();
            _mJoystickLeft = null;
            _mJoystickRight?.Dispose();
            _mJoystickRight = null;

            GameplayEventHandler.OnPickupStateChanged -= OnPickupStateChanged;
        }

        /// <summary>
        /// This class handles a pointer capture and movement for a Joystick
        /// in the <see cref="TouchScreenBehaviour"/> UI panel.
        /// </summary>
        /// <remarks>
        /// The <see cref="IDisposable"/> interface is used to unregister the UI events callbacks
        /// and should be called in the UI <see cref="TouchScreenBehaviour"/> method.
        /// </remarks>
        /// <remarks>
        /// The Bindings on the visual elements are only reading from <see cref="MobileGamepadState"/>
        /// because in this particular case, the Pointer events handlers are writing the position to the data and the visual gets updated from it.
        /// </remarks>
        class VirtualJoystick : IDisposable
        {
            readonly VisualElement _mRoot;
            readonly Action<Vector2> _mOnJoystickMoved;

            public VirtualJoystick(VisualElement root, Action<Vector2> onJoystickMoved, string topProperty, string leftProperty)
            {
                _mRoot = root;
                _mOnJoystickMoved = onJoystickMoved;

                root.RegisterCallback<PointerDownEvent>(HandlePress);
                root.RegisterCallback<PointerMoveEvent>(HandleDrag);
                root.RegisterCallback<PointerUpEvent>(HandleRelease);

                var stick = root.Q<VisualElement>("Stick");
                stick.SetBinding("style.top", new DataBinding
                {
                    dataSourcePath = new PropertyPath(topProperty),
                    bindingMode = BindingMode.ToTarget,
                });
                stick.SetBinding("style.left", new DataBinding
                {
                    dataSourcePath = new PropertyPath(leftProperty),
                    bindingMode = BindingMode.ToTarget,
                });
            }

            public void Dispose()
            {
                _mRoot.UnregisterCallback<PointerDownEvent>(HandlePress);
                _mRoot.UnregisterCallback<PointerMoveEvent>(HandleDrag);
                _mRoot.UnregisterCallback<PointerUpEvent>(HandleRelease);
            }

            void HandlePress(PointerDownEvent evt) => _mRoot.CapturePointer(evt.pointerId);

            void HandleRelease(PointerUpEvent evt)
            {
                if (!_mRoot.HasPointerCapture(evt.pointerId))
                    return;

                _mRoot.ReleasePointer(evt.pointerId);
                _mOnJoystickMoved(Vector2.zero);
            }

            void HandleDrag(PointerMoveEvent evt)
            {
                if (!_mRoot.HasPointerCapture(evt.pointerId))
                    return;

                var center = _mRoot.contentRect.center;
                var width = _mRoot.contentRect.width;
                var centerToPosition = ((Vector2)evt.localPosition - center) / width;

                if (centerToPosition.sqrMagnitude > 1)
                {
                    centerToPosition = centerToPosition.normalized;
                }

                _mOnJoystickMoved(centerToPosition);
            }
        }
    }
}
