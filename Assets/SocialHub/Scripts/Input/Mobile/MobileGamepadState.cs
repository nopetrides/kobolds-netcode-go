using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Input
{
    /// <summary>
    /// Represents the state of a gamepad controller driven by the UI.
    /// </summary>
    /// <remarks>
    /// The singleton instance is written into by the visual inputs from the <see cref="TouchScreenBehaviour"/> UI
    /// and read by the <see cref="MobileGamepadBehaviour"/> which forwards changed values to the InputSystem.
    /// </remarks>
    class MobileGamepadState : INotifyBindablePropertyChanged
    {
        // UI Y axis is inversed compared to a gamepad joystick, invert it by default
        static readonly Vector2 KInvertY = new(1, -1);

        static MobileGamepadState _sInstance;
        /// <summary>
        /// The instance is only created when used at runtime.
        /// </summary>
        internal static MobileGamepadState GetOrCreate
        {
            get
            {
                _sInstance ??= new MobileGamepadState();
                return _sInstance;
            }
        }

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeOnLoad() => _sInstance = null;

        /// <summary>
        /// Private constructor.
        /// Use the singleton instance from <see cref="GetOrCreate"/> instead.
        /// </summary>
        MobileGamepadState()
        {
        }

        /// <summary>
        /// Event fired when a property bound to the UI is changed.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        /// <summary>
        /// Event fired when a button bound to the InputSystem is changed.
        /// </summary>
        internal event Action<string, float> ButtonStateChanged;
        /// <summary>
        /// Event fired when a joystick position bound to the InputSystem is changed.
        /// </summary>
        internal event Action<string, Vector2> JoystickStateChanged;

        /// <summary>
        /// This method lets the UI update when a property bound with <see cref="BindingMode.ToTarget"/> is calling it.
        /// </summary>
        /// <param name="property">The property bound in the UI using <see cref="BindingMode.ToTarget"/></param>
        void NotifyUI([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        /// <summary>
        /// This method lets the Input system update when a property value is changed.
        /// </summary>
        /// <seealso cref="NotifyInput(Vector2,string)"/>
        /// <param name="value">The new value of the property</param>
        /// <param name="property">The property name</param>
        void NotifyInput(float value, [CallerMemberName] string property = "")
        {
            ButtonStateChanged?.Invoke(property, value);
        }

        /// <summary>
        /// This method lets the Input system update when a property value is changed.
        /// </summary>
        /// <seealso cref="NotifyInput(float,string)"/>
        /// <param name="value">The new value of the property</param>
        /// <param name="property">The property name</param>
        void NotifyInput(Vector2 value, [CallerMemberName] string property = "")
        {
            JoystickStateChanged?.Invoke(property, value);
        }

        Vector2 _mLeftJoystick;
        /// <summary>
        /// The current position of the left joystick.
        /// </summary>
        /// <remarks>
        /// <para>UIToolkit usage:</para>
        /// The UI is bound to <see cref="LeftJoystickTop"/> and <see cref="LeftJoystickLeft"/>
        /// which converts the Vector2 position into a percent <see cref="StyleLength"/>.
        /// <para>The <see cref="TouchScreenBehaviour"/> is reading the UI pointer
        /// to directly write the delta in this property, which in returns updates the VisualElement position.</para>
        /// <para>InputSystem usage:</para>
        /// The InputSystem is being sent the Vector2 value with the Y axis inverted
        /// because UIToolkit has its origin in the top-left corner.
        /// </remarks>
        internal Vector2 LeftJoystick
        {
            set
            {
                var oldValue = _mLeftJoystick;
                _mLeftJoystick = value;
                NotifyInput(value * KInvertY);

                if (_mLeftJoystick.x != oldValue.x)
                {
                    NotifyUI(nameof(LeftJoystickLeft));
                }
                if (_mLeftJoystick.y != oldValue.y)
                {
                    NotifyUI(nameof(LeftJoystickTop));
                }
            }
        }

        internal string LeftJoystickTopName => nameof(LeftJoystickTop);
        [CreateProperty]
        StyleLength LeftJoystickTop => ConvertJoystickRangeToUIPosition(_mLeftJoystick.y);
        internal string LeftJoystickLeftName => nameof(LeftJoystickLeft);
        [CreateProperty]
        StyleLength LeftJoystickLeft => ConvertJoystickRangeToUIPosition(_mLeftJoystick.x);

        Vector2 _mRightJoystick;
        /// <summary>
        /// The current position of the right joystick.
        /// </summary>
        /// <remarks>
        /// <para>UIToolkit usage:</para>
        /// The UI is bound to <see cref="RightJoystickTop"/> and <see cref="RightJoystickLeft"/>
        /// which converts the Vector2 position into a percent <see cref="StyleLength"/>.
        /// <para>The <see cref="TouchScreenBehaviour"/> is reading the UI pointer
        /// to directly write the delta in this property, which in return updates the VisualElement position.</para>
        /// </remarks>
        internal Vector2 RightJoystick
        {
            set
            {
                var oldValue = _mRightJoystick;
                _mRightJoystick = value;
                NotifyInput(value * KInvertY);

                if (_mRightJoystick.x != oldValue.x)
                {
                    NotifyUI(nameof(RightJoystickLeft));
                }
                if (_mRightJoystick.y != oldValue.y)
                {
                    NotifyUI(nameof(RightJoystickTop));
                }
            }
        }
        internal string RightJoystickTopName => nameof(RightJoystickTop);
        [CreateProperty]
        StyleLength RightJoystickTop => ConvertJoystickRangeToUIPosition(_mRightJoystick.y);
        internal string RightJoystickLeftName => nameof(RightJoystickLeft);
        [CreateProperty]
        StyleLength RightJoystickLeft => ConvertJoystickRangeToUIPosition(_mRightJoystick.x);

        bool _mButtonMenu;
        /// <summary>
        /// The current state of the menu button.
        /// </summary>
        /// <remarks>
        /// <para>InputSystem usage:</para>
        /// The InputSystem is using a float value to describe button states.
        /// </remarks>
        [CreateProperty]
        internal bool ButtonMenu
        {
            get => _mButtonMenu;
            set
            {
                if (_mButtonMenu == value)
                    return;

                _mButtonMenu = value;
                NotifyUI();
                NotifyInput(value ? 1f : 0f);
            }
        }

        bool _mButtonInteract;
        /// <summary>
        /// The current state of the shoot button.
        /// </summary>
        /// <remarks>
        /// <para>InputSystem usage:</para>
        /// The InputSystem is using a float value to describe button states.
        /// </remarks>
        [CreateProperty]
        internal bool ButtonInteract
        {
            get => _mButtonInteract;
            set
            {
                if (_mButtonInteract == value)
                    return;

                _mButtonInteract = value;
                NotifyUI();
                NotifyInput(value ? 1f : 0f);
            }
        }

        bool _mButtonSprint;
        /// <summary>
        /// The current state of the aim button.
        /// </summary>
        /// <remarks>
        /// <para>InputSystem usage:</para>
        /// The InputSystem is using a float value to describe button states.
        /// </remarks>
        [CreateProperty]
        internal bool ButtonSprint
        {
            get => _mButtonSprint;
            set
            {
                if (_mButtonSprint == value)
                    return;

                _mButtonSprint = value;
                NotifyUI();
                NotifyInput(value ? 1f : 0f);
            }
        }

        bool _mButtonJump;
        /// <summary>
        /// The current state of the jump button.
        /// </summary>
        /// <remarks>
        /// <para>InputSystem usage:</para>
        /// The InputSystem is using a float value to describe button states.
        /// </remarks>
        [CreateProperty]
        internal bool ButtonJump
        {
            get => _mButtonJump;
            set
            {
                if (_mButtonJump == value)
                    return;

                _mButtonJump = value;
                NotifyUI();
                NotifyInput(value ? 1f : 0f);
            }
        }

        // TODO: revisit if necessary
        /*bool m_ButtonToggleNetworkStats;
        /// <summary>
        /// The current state of the toggle network stats button.
        /// </summary>
        /// <remarks>
        /// <para>InputSystem usage:</para>
        /// The InputSystem is using a float value to describe button states.
        /// </remarks>
        [CreateProperty]
        internal bool ButtonToggleNetworkStats
        {
            get => m_ButtonToggleNetworkStats;
            set
            {
                if (m_ButtonToggleNetworkStats == value)
                    return;

                m_ButtonToggleNetworkStats = value;
                NotifyUI();
                NotifyInput(value ? 1f : 0f);
            }
        }*/

        /// <summary>
        /// This method converts an Input Joystick position with a float[-1:1] range
        /// to a UIToolkit Percent Length that ranges from int[0:100].
        /// </summary>
        /// <param name="position">A float position expected to be between [-1:1]</param>
        /// <returns>A UIToolkit Length in percent.</returns>
        static StyleLength ConvertJoystickRangeToUIPosition(float position)
        {
            return Length.Percent((position + 1f) * 50);
        }
    }
}
