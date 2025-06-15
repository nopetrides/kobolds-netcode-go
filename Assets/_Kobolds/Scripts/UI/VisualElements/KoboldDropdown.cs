using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
    /// <summary>
    /// Animated dropdown with custom styling
    /// </summary>
    public class KoboldDropdown : KoboldVisualElement
    {
        private DropdownField _dropdown;
        private Label _label;
        private VisualElement _dropdownIcon;
        
        public string Label { get; set; }
        public List<string> Choices { get; set; }
        public int Index
        {
            get => _dropdown?.index ?? 0;
            set
            {
                if (_dropdown != null && value >= 0 && value < Choices.Count)
                {
                    _dropdown.index = value;
                }
            }
        }
        
        public string Value
        {
            get => _dropdown?.value ?? string.Empty;
            set
            {
                if (_dropdown != null && Choices.Contains(value))
                {
                    _dropdown.value = value;
                }
            }
        }
        
        public event Action<string> ValueChanged;
        
        public KoboldDropdown() : this(string.Empty, new List<string>()) { }
        
        public KoboldDropdown(string label, List<string> choices)
        {
            Label = label;
            Choices = choices ?? new List<string>();
            
            AddToClassList("kobold-dropdown");
            
            BuildUI();
            RegisterCallbacks();
        }
        
        private void BuildUI()
        {
            // Container
            var container = new VisualElement();
            container.AddToClassList("dropdown-container");
            Add(container);
            
            // Label
            if (!string.IsNullOrEmpty(Label))
            {
                _label = new Label(Label);
                _label.AddToClassList("dropdown-label");
                container.Add(_label);
            }
            
            // Dropdown wrapper for custom styling
            var dropdownWrapper = new VisualElement();
            dropdownWrapper.AddToClassList("dropdown-wrapper");
            container.Add(dropdownWrapper);
            
            // The actual dropdown
            _dropdown = new DropdownField(Choices, 0);
            _dropdown.AddToClassList("unity-dropdown");
            dropdownWrapper.Add(_dropdown);
            
            // Custom dropdown icon (animated)
            _dropdownIcon = new VisualElement();
            _dropdownIcon.AddToClassList("dropdown-icon");
            dropdownWrapper.Add(_dropdownIcon);
        }
        
        private void RegisterCallbacks()
        {
            _dropdown.RegisterValueChangedCallback(OnValueChanged);
            _dropdown.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            _dropdown.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _dropdown.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _dropdown.RegisterCallback<MouseUpEvent>(OnMouseUp);
            
            // Listen for dropdown open/close
            _dropdown.RegisterCallback<FocusInEvent>(OnDropdownOpen);
            _dropdown.RegisterCallback<FocusOutEvent>(OnDropdownClose);
        }
        
        private void OnValueChanged(ChangeEvent<string> evt)
        {
            ValueChanged?.Invoke(evt.newValue);
            AnimateSelection();
            PlaySound(UISoundType.Click);
        }
        
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            AddToClassList("hover");
            AnimateHover();
            PlaySound(UISoundType.Hover);
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            RemoveFromClassList("hover");
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            AddToClassList("active");
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            RemoveFromClassList("active");
        }
        
        private void OnDropdownOpen(FocusInEvent evt)
        {
            AddToClassList("open");
            AnimateIconRotate(true);
        }
        
        private void OnDropdownClose(FocusOutEvent evt)
        {
            RemoveFromClassList("open");
            AnimateIconRotate(false);
        }
        
        private void AnimateHover()
        {
            var wrapper = this.Q<VisualElement>("dropdown-wrapper");
            if (wrapper != null)
            {
                wrapper.style.scale = new Scale(Vector2.one * 1.02f);
                wrapper.schedule.Execute(() =>
                {
                    wrapper.style.scale = new Scale(Vector2.one);
                }).StartingIn(200);
            }
        }
        
        private void AnimateSelection()
        {
            // Pulse animation on selection
            style.scale = new Scale(Vector2.one * 0.98f);
            schedule.Execute(() =>
            {
                style.scale = new Scale(Vector2.one);
            }).StartingIn(100);
        }
        
        private void AnimateIconRotate(bool open)
        {
            if (_dropdownIcon != null)
            {
                float targetRotation = open ? 180f : 0f;
                _dropdownIcon.style.rotate = new Rotate(Angle.Degrees(targetRotation));
            }
        }
        
        private void PlaySound(UISoundType soundType)
        {
            // TODO: Hook into your sound system
        }
        
        protected override void PrepareForAnimation()
        {
            base.PrepareForAnimation();
            
            // Dropdowns fade and scale in
            style.opacity = 0f;
            style.scale = new Scale(Vector2.one * 0.9f);
        }
        
        public void SetChoices(List<string> choices)
        {
            Choices = choices ?? new List<string>();
            if (_dropdown != null)
            {
                _dropdown.choices = Choices;
                if (_dropdown.index >= Choices.Count)
                {
                    _dropdown.index = 0;
                }
            }
        }
    }
    
    // UXML Support
    [UxmlElement]
    public partial class KoboldDropdownElement : VisualElement
    {
        [UxmlAttribute]
        public string Label { get; set; } = "Dropdown";
        
        [UxmlAttribute]
        public string Choices { get; set; } = "Option 1,Option 2,Option 3";
        
        [UxmlAttribute]
        public int Index { get; set; } = 0;
        
        [UxmlAttribute]
        public float AnimationDelay { get; set; } = 0f;
        
        private KoboldDropdown _dropdown;
        
        public KoboldDropdownElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_dropdown == null)
            {
                var choicesList = new List<string>(Choices.Split(','));
                _dropdown = new KoboldDropdown(Label, choicesList)
                {
                    Index = Index,
                    AnimationDelay = AnimationDelay
                };
                Add(_dropdown);
            }
        }
    }
}