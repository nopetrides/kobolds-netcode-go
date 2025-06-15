using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
	public enum UISoundType
	{
		Click,
		Hover,
		Error,
		Success,
		Back
	}

	/// <summary>
	///     Animated button with hover, press states and sound effects
	/// </summary>
	public class KoboldButton : KoboldVisualElement
	{
		private readonly VisualElement _background;
		private readonly Button _button;
		private readonly VisualElement _hoverOverlay;

		private bool _isHovered;
		private bool _isPressed;
		private readonly Label _label;
		private readonly VisualElement _pressOverlay;

		public KoboldButton() : this(string.Empty)
		{
		}

		public KoboldButton(string text)
		{
			AddToClassList("kobold-button");

			// Create button structure
			_button = new Button();
			_button.AddToClassList("unity-button");
			Add(_button);

			// Background for animations
			_background = new VisualElement();
			_background.AddToClassList("button-background");
			_button.Add(_background);

			// Hover overlay
			_hoverOverlay = new VisualElement();
			_hoverOverlay.AddToClassList("button-hover-overlay");
			_hoverOverlay.style.opacity = 0f;
			_button.Add(_hoverOverlay);

			// Press overlay
			_pressOverlay = new VisualElement();
			_pressOverlay.AddToClassList("button-press-overlay");
			_pressOverlay.style.opacity = 0f;
			_button.Add(_pressOverlay);

			// Label
			_label = new Label(text);
			_label.AddToClassList("button-label");
			_button.Add(_label);

			RegisterCallbacks();
		}

		public string Text
		{
			get => _label?.text ?? string.Empty;
			set
			{
				if (_label != null)
					_label.text = value;
			}
		}

		public event Action Clicked;

		private void RegisterCallbacks()
		{
			_button.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
			_button.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
			_button.RegisterCallback<MouseDownEvent>(OnMouseDown);
			_button.RegisterCallback<MouseUpEvent>(OnMouseUp);
			_button.clicked += OnClicked;
		}

		private void OnMouseEnter(MouseEnterEvent evt)
		{
			if (_isPressed || IsAnimating) return;

			_isHovered = true;
			AnimateHoverIn();
			PlaySound(UISoundType.Hover);
		}

		private void OnMouseLeave(MouseLeaveEvent evt)
		{
			if (IsAnimating) return;

			_isHovered = false;
			_isPressed = false;
			AnimateHoverOut();
			AnimatePressOut();
		}

		private void OnMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0 || IsAnimating) return;

			_isPressed = true;
			AnimatePressIn();
		}

		private void OnMouseUp(MouseUpEvent evt)
		{
			if (evt.button != 0 || IsAnimating) return;

			_isPressed = false;
			AnimatePressOut();
		}

		private void OnClicked()
		{
			if (IsAnimating) return;

			PlaySound(UISoundType.Click);
			AnimatePulse();
			Clicked?.Invoke();
		}

		private void AnimateHoverIn()
		{
			// Animate opacity
			_hoverOverlay.style.opacity = 0f;
			_hoverOverlay.schedule.Execute(() => _hoverOverlay.style.opacity = 1f).StartingIn(1);

			// Animate scale
			_button.style.scale = new Scale(Vector2.one);
			_button.schedule.Execute(() => _button.style.scale = new Scale(Vector2.one * 1.05f)).StartingIn(1);
		}

		private void AnimateHoverOut()
		{
			// Animate opacity
			_hoverOverlay.style.opacity = 1f;
			_hoverOverlay.schedule.Execute(() => _hoverOverlay.style.opacity = 0f).StartingIn(1);

			// Animate scale
			_button.style.scale = new Scale(Vector2.one * 1.05f);
			_button.schedule.Execute(() => _button.style.scale = new Scale(Vector2.one)).StartingIn(1);
		}

		private void AnimatePressIn()
		{
			// Animate opacity
			_pressOverlay.style.opacity = 0f;
			_pressOverlay.schedule.Execute(() => _pressOverlay.style.opacity = 1f).StartingIn(1);

			// Animate scale
			_button.style.scale = new Scale(Vector2.one);
			_button.schedule.Execute(() => _button.style.scale = new Scale(Vector2.one * 0.95f)).StartingIn(1);
		}

		private void AnimatePressOut()
		{
			// Animate opacity
			_pressOverlay.style.opacity = 1f;
			_pressOverlay.schedule.Execute(() => _pressOverlay.style.opacity = 0f).StartingIn(1);

			if (!_isHovered)
			{
				// Animate scale
				_button.style.scale = new Scale(Vector2.one * 0.95f);
				_button.schedule.Execute(() => _button.style.scale = new Scale(Vector2.one)).StartingIn(1);
			}
		}

		private void AnimatePulse()
		{
			// Quick scale pulse on click
			_button.style.scale = new Scale(Vector2.one);
			_button.schedule.Execute(() =>
			{
				_button.style.scale = new Scale(Vector2.one * 1.1f);
				_button.schedule.Execute(() => { _button.style.scale = new Scale(Vector2.one); }).StartingIn(100);
			}).StartingIn(1);
		}

		private void PlaySound(UISoundType soundType)
		{
			// TODO: Hook into your sound system
			// KoboldThemeManager.Instance?.PlayUISound(soundType);
		}

		protected override void PrepareForAnimation()
		{
			base.PrepareForAnimation();

			// Buttons can have a slight Y offset animation
			style.translate = new StyleTranslate(new Translate(0, 20, 0));
		}

		protected override void OnAnimateInComplete()
		{
			base.OnAnimateInComplete();
			style.translate = new StyleTranslate(new Translate(0, 0, 0));
		}
	}

	// Extension for easy creation in UI Builder
	[UxmlElement]
	public partial class KoboldButtonElement : VisualElement
	{
		private KoboldButton _button;

		public KoboldButtonElement()
		{
			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
		}

		[UxmlAttribute]
		private string Text { get; set; } = "Button";

		[UxmlAttribute]
		public float AnimationDuration { get; set; } = 0.3f;

		[UxmlAttribute]
		public float AnimationDelay { get; set; } = 0f;

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			if (_button == null)
			{
				_button = new KoboldButton(Text);
				_button.AnimationDuration = AnimationDuration;
				_button.AnimationDelay = AnimationDelay;
				Add(_button);
				Debug.Log($"[KoboldButtonElement] Attached: {name}, added: {_button != null}");
			}
		}
	}
}
