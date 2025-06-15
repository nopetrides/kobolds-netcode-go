using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
    /// <summary>
    /// Animated text field with focus states
    /// </summary>
    public class KoboldTextField : KoboldVisualElement
    {
        private TextField _textField;
        private VisualElement _focusIndicator;
        private Label _floatingLabel;
        
        private bool _isFocused;
        private bool _hasValue;
        
        public string Value
        {
            get => _textField?.value ?? string.Empty;
            set
            {
                if (_textField != null)
                {
                    _textField.value = value;
                    UpdateFloatingLabel();
                }
            }
        }
        
        public string Label { get; set; }
        public string Placeholder { get; set; }
        
        public event Action<string> ValueChanged;
        
        public KoboldTextField() : this(string.Empty, string.Empty) { }
        
        public KoboldTextField(string label, string placeholder = "")
        {
            Label = label;
            Placeholder = placeholder;
            
            AddToClassList("kobold-text-field");
            
            BuildUI();
            RegisterCallbacks();
        }
        
        private void BuildUI()
        {
            // Container for the field
            var container = new VisualElement();
            container.AddToClassList("text-field-container");
            Add(container);
            
            // Floating label
            _floatingLabel = new Label(Label);
            _floatingLabel.AddToClassList("floating-label");
            container.Add(_floatingLabel);
            
            // The actual text field
            _textField = new TextField();
            _textField.AddToClassList("kobold-input");
            container.Add(_textField);
            
            // Focus indicator (animated underline)
            _focusIndicator = new VisualElement();
            _focusIndicator.AddToClassList("focus-indicator");
            _focusIndicator.style.scale = new Scale(new Vector2(0, 1));
            container.Add(_focusIndicator);
        }
        
        private void RegisterCallbacks()
        {
            _textField.RegisterCallback<FocusInEvent>(OnFocusIn);
            _textField.RegisterCallback<FocusOutEvent>(OnFocusOut);
            _textField.RegisterValueChangedCallback(OnValueChanged);
        }
        
        private void OnFocusIn(FocusInEvent evt)
        {
            _isFocused = true;
            AddToClassList("focused");
            AnimateFocusIn();
            PlaySound(UISoundType.Click);
        }
        
        private void OnFocusOut(FocusOutEvent evt)
        {
            _isFocused = false;
            RemoveFromClassList("focused");
            AnimateFocusOut();
            UpdateFloatingLabel();
        }
        
        private void OnValueChanged(ChangeEvent<string> evt)
        {
            _hasValue = !string.IsNullOrEmpty(evt.newValue);
            UpdateFloatingLabel();
            ValueChanged?.Invoke(evt.newValue);
        }
        
        private void UpdateFloatingLabel()
        {
            if (_hasValue || _isFocused)
            {
                _floatingLabel.AddToClassList("floating");
            }
            else
            {
                _floatingLabel.RemoveFromClassList("floating");
            }
        }
        
        private void AnimateFocusIn()
        {
            // Animate focus indicator
            _focusIndicator.schedule.Execute(() =>
            {
                _focusIndicator.style.scale = new Scale(new Vector2(1, 1));
            }).StartingIn(1);
            
            // Animate label
            if (!_hasValue)
            {
                _floatingLabel.schedule.Execute(() =>
                {
                    _floatingLabel.AddToClassList("floating");
                }).StartingIn(1);
            }
        }
        
        private void AnimateFocusOut()
        {
            // Animate focus indicator
            _focusIndicator.schedule.Execute(() =>
            {
                _focusIndicator.style.scale = new Scale(new Vector2(0, 1));
            }).StartingIn(1);
        }
        
        private void PlaySound(UISoundType soundType)
        {
            // TODO: Hook into your sound system
        }
        
        protected override void PrepareForAnimation()
        {
            base.PrepareForAnimation();
            
            // Text fields slide in from bottom
            style.translate = new StyleTranslate(new Translate(0, 30, 0));
        }
    }
    
    // UXML Support with modern attributes
    [UxmlElement]
    public partial class KoboldTextFieldElement : VisualElement
    {
        [UxmlAttribute]
        public string Label { get; set; } = "Label";
        
        [UxmlAttribute]
        public string Placeholder { get; set; } = "";
        
        [UxmlAttribute]
        public string Value { get; set; } = "";
        
        [UxmlAttribute]
        public float AnimationDuration { get; set; } = 0.3f;
        
        [UxmlAttribute]
        public float AnimationDelay { get; set; } = 0f;
        
        private KoboldTextField _textField;
        
        public KoboldTextFieldElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_textField == null)
            {
                _textField = new KoboldTextField(Label, Placeholder);
                _textField.Value = Value;
                _textField.AnimationDuration = AnimationDuration;
                _textField.AnimationDelay = AnimationDelay;
                Add(_textField);
            }
        }
    }
}