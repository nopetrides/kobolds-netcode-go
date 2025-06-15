using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// This custom control replicate a Button Pressed/Released behaviour as it would work on a Gamepad Controller.
    /// </summary>
    [UxmlElement]
    public partial class PressedButton : Toggle
    {
        bool _mHasPointer;

        public PressedButton()
            : this(null)
        {
        }

        public PressedButton(string label)
            : base(label)
        {
            _mHasPointer = false;
            RegisterCallback<PointerCaptureEvent>(OnPointerCapture);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            _mHasPointer = false;
            ToggleValue();
        }

        void OnPointerCapture(PointerCaptureEvent evt)
        {
            _mHasPointer = true;
            ToggleValue();
        }

        protected override void ToggleValue()
        {
            value = _mHasPointer;
        }
    }
}
