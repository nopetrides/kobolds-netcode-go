using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
    /// <summary>
    /// Animated slider with value display
    /// </summary>
    public class KoboldSlider : KoboldVisualElement
    {
        private Slider _slider;
        private Label _label;
        private Label _valueLabel;
        private VisualElement _fillBar;
        
        public string Label { get; set; }
        public float MinValue { get; set; } = 0f;
        public float MaxValue { get; set; } = 100f;
        public float Value
        {
            get => _slider?.value ?? 0f;
            set
            {
                if (_slider != null)
                {
                    _slider.value = value;
                    UpdateValueDisplay();
                }
            }
        }
        
        public bool ShowValue { get; set; } = true;
        public string ValueFormat { get; set; } = "{0:0}";
        
        public event Action<float> ValueChanged;
        
        public KoboldSlider() : this(string.Empty) { }
        
        public KoboldSlider(string label)
        {
            Label = label;
            AddToClassList("kobold-slider");
            
            BuildUI();
            RegisterCallbacks();
        }
        
        private void BuildUI()
        {
            // Container
            var container = new VisualElement();
            container.AddToClassList("slider-container");
            Add(container);
            
            // Header with label and value
            var header = new VisualElement();
            header.AddToClassList("slider-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            container.Add(header);
            
            // Label
            _label = new Label(Label);
            _label.AddToClassList("slider-label");
            header.Add(_label);
            
            // Value display
            _valueLabel = new Label();
            _valueLabel.AddToClassList("slider-value");
            if (ShowValue)
                header.Add(_valueLabel);
            
            // Custom slider wrapper
            var sliderWrapper = new VisualElement();
            sliderWrapper.AddToClassList("slider-wrapper");
            container.Add(sliderWrapper);
            
            // Fill bar (visual enhancement)
            _fillBar = new VisualElement();
            _fillBar.AddToClassList("slider-fill");
            sliderWrapper.Add(_fillBar);
            
            // The actual slider
            _slider = new Slider(MinValue, MaxValue);
            _slider.AddToClassList("unity-slider");
            _slider.showInputField = false;
            sliderWrapper.Add(_slider);
            
            UpdateValueDisplay();
        }
        
        private void RegisterCallbacks()
        {
            _slider.RegisterValueChangedCallback(OnSliderValueChanged);
            _slider.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            _slider.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _slider.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _slider.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }
        
        private void OnSliderValueChanged(ChangeEvent<float> evt)
        {
            UpdateValueDisplay();
            UpdateFillBar();
            ValueChanged?.Invoke(evt.newValue);
            
            // Subtle pulse on value change
            if (Mathf.Abs(evt.newValue - evt.previousValue) > 0.01f)
            {
                AnimatePulse();
            }
        }
        
        private void UpdateValueDisplay()
        {
            if (_valueLabel != null && ShowValue)
            {
                _valueLabel.text = string.Format(ValueFormat, _slider.value);
            }
        }
        
        private void UpdateFillBar()
        {
            if (_fillBar != null && _slider != null)
            {
                float percent = (_slider.value - MinValue) / (MaxValue - MinValue);
                _fillBar.style.width = Length.Percent(percent * 100);
            }
        }
        
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            AddToClassList("hover");
            PlaySound(UISoundType.Hover);
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            RemoveFromClassList("hover");
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            AddToClassList("active");
            PlaySound(UISoundType.Click);
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            RemoveFromClassList("active");
        }
        
        private void AnimatePulse()
        {
            if (_valueLabel != null)
            {
                _valueLabel.style.scale = new Scale(Vector2.one * 1.2f);
                _valueLabel.schedule.Execute(() =>
                {
                    _valueLabel.style.scale = new Scale(Vector2.one);
                }).StartingIn(100);
            }
        }
        
        private void PlaySound(UISoundType soundType)
        {
            // TODO: Hook into your sound system
        }
        
        protected override void PrepareForAnimation()
        {
            base.PrepareForAnimation();
            
            // Sliders slide in from left
            style.translate = new StyleTranslate(new Translate(-30, 0, 0));
        }
        
        public void SetLabel(string label)
        {
            Label = label;
            if (_label != null)
                _label.text = label;
        }
    }
    
    // UXML Support
    [UxmlElement]
    public partial class KoboldSliderElement : VisualElement
    {
        [UxmlAttribute]
        public string Label { get; set; } = "Slider";
        
        [UxmlAttribute]
        public float MinValue { get; set; } = 0f;
        
        [UxmlAttribute]
        public float MaxValue { get; set; } = 100f;
        
        [UxmlAttribute]
        public float Value { get; set; } = 50f;
        
        [UxmlAttribute]
        public bool ShowValue { get; set; } = true;
        
        [UxmlAttribute]
        public string ValueFormat { get; set; } = "{0:0}";
        
        [UxmlAttribute]
        public float AnimationDelay { get; set; } = 0f;
        
        private KoboldSlider _slider;
        
        public KoboldSliderElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_slider == null)
            {
                _slider = new KoboldSlider(Label)
                {
                    MinValue = MinValue,
                    MaxValue = MaxValue,
                    Value = Value,
                    ShowValue = ShowValue,
                    ValueFormat = ValueFormat,
                    AnimationDelay = AnimationDelay
                };
                Add(_slider);
            }
        }
    }
}