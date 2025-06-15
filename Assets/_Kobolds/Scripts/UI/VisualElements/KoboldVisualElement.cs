using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
	/// <summary>
	///     Base class for all Kobold UI elements with built-in animation support
	/// </summary>
	[UxmlElement]
	public partial class KoboldVisualElement : VisualElement
	{
		private float _animationDelay;

		// Animation settings
		private float _animationDuration = 0.3f;

		// Initial state for reset
		private StyleFloat _initialOpacity;
		private StyleScale _initialScale;
		private StyleTranslate _initialTranslate;
		private bool _isAnimating;
		public bool IsAnimating => _isAnimating;

		public KoboldVisualElement()
		{
			RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
			RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

			// Add base class
			AddToClassList("kobold-element");
		}

		public float AnimationDuration
		{
			get => _animationDuration;
			set => _animationDuration = Mathf.Max(0f, value);
		}

		public float AnimationDelay
		{
			get => _animationDelay;
			set => _animationDelay = Mathf.Max(0f, value);
		}

		public Ease AnimationEase { get; set; } = Ease.OutBack;

		public bool AutoAnimateOnAttach { get; set; } = true;

		protected virtual void OnAttachedToPanel(AttachToPanelEvent evt)
		{
			// Store initial values
			_initialOpacity = style.opacity;
			_initialScale = style.scale;
			_initialTranslate = style.translate;

			if (AutoAnimateOnAttach) AnimateIn();
		}

		protected virtual void OnDetachedFromPanel(DetachFromPanelEvent evt)
		{
			// Cleanup if needed
		}

		public virtual void AnimateIn(float delay = -1f)
		{
			if (_isAnimating) return;

			var actualDelay = delay >= 0 ? delay : _animationDelay;

			// Set initial state
			PrepareForAnimation();

			// Add animating class
			AddToClassList("animating");

			// Schedule animation
			schedule.Execute(() =>
			{
				_isAnimating = true;
				StartCoroutine(AnimateInCoroutine());
			}).StartingIn((long) (actualDelay * 1000));
		}

		public virtual void AnimateOut(Action onComplete = null)
		{
			if (_isAnimating) return;

			_isAnimating = true;
			AddToClassList("animating");
			StartCoroutine(AnimateOutCoroutine(onComplete));
		}

		protected virtual void PrepareForAnimation()
		{
			// Default: fade and scale from 0
			style.opacity = 0f;
			style.scale = new Scale(Vector2.zero);
		}

		private IEnumerator AnimateInCoroutine()
		{
			var elapsed = 0f;

			while (elapsed < _animationDuration)
			{
				elapsed += Time.deltaTime;
				var t = elapsed / _animationDuration;
				var easedT = ApplyEasing(t, AnimationEase);

				// Animate opacity
				style.opacity = Mathf.Lerp(0f, 1f, easedT);

				// Animate scale
				var scale = Mathf.Lerp(0f, 1f, easedT);
				style.scale = new Scale(new Vector2(scale, scale));

				yield return null;
			}

			// Ensure final values
			style.opacity = _initialOpacity;
			style.scale = _initialScale;

			_isAnimating = false;
			RemoveFromClassList("animating");
			OnAnimateInComplete();
		}

		private IEnumerator AnimateOutCoroutine(Action onComplete)
		{
			var elapsed = 0f;

			while (elapsed < _animationDuration)
			{
				elapsed += Time.deltaTime;
				var t = elapsed / _animationDuration;
				var easedT = ApplyEasing(t, Ease.InBack);

				// Animate opacity
				style.opacity = Mathf.Lerp(1f, 0f, easedT);

				// Animate scale
				var scale = Mathf.Lerp(1f, 0f, easedT);
				style.scale = new Scale(new Vector2(scale, scale));

				yield return null;
			}

			// Final state
			style.opacity = 0f;
			style.scale = new Scale(Vector2.zero);

			_isAnimating = false;
			RemoveFromClassList("animating");
			OnAnimateOutComplete();
			onComplete?.Invoke();
		}

		protected virtual void OnAnimateInComplete()
		{
		}

		protected virtual void OnAnimateOutComplete()
		{
		}

		private IEnumerator StartCoroutine(IEnumerator routine)
		{
			// UI Toolkit doesn't have built-in coroutines, so we use the schedule system
			var running = true;
			var enumerator = routine;

			Action tick = null;
			tick = () =>
			{
				if (!running) return;

				try
				{
					if (!enumerator.MoveNext())
					{
						running = false;
						return;
					}

					schedule.Execute(tick).ExecuteLater(16); // ~60fps
				}
				catch (Exception e)
				{
					Debug.LogError($"Animation error: {e}");
					running = false;
				}
			};

			schedule.Execute(tick);
			return routine;
		}

		private float ApplyEasing(float t, Ease ease)
		{
			switch (ease)
			{
				case Ease.Linear:
					return t;

				case Ease.InQuad:
					return t * t;

				case Ease.OutQuad:
					return t * (2f - t);

				case Ease.InOutQuad:
					return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

				case Ease.InCubic:
					return t * t * t;

				case Ease.OutCubic:
					return 1f + --t * t * t;

				case Ease.InOutCubic:
					return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

				case Ease.InBack:
					return t * t * (2.70158f * t - 1.70158f);

				case Ease.OutBack:
					return 1f + --t * t * (2.70158f * t + 1.70158f);

				case Ease.InOutBack:
					const float c = 1.70158f * 1.525f;
					return t < 0.5f ?
						Mathf.Pow(2f * t, 2f) * ((c + 1f) * 2f * t - c) / 2f :
						(Mathf.Pow(2f * t - 2f, 2f) * ((c + 1f) * (t * 2f - 2f) + c) + 2f) / 2f;

				case Ease.OutElastic:
					if (t == 0f || Mathf.Approximately(t, 1f)) return t;
					return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.075f) * (2f * Mathf.PI) / 0.3f) + 1f;

				case Ease.OutBounce:
					if (t < 1f / 2.75f) return 7.5625f * t * t;

					if (t < 2f / 2.75f)
					{
						t -= 1.5f / 2.75f;
						return 7.5625f * t * t + 0.75f;
					}

					if (t < 2.5f / 2.75f)
					{
						t -= 2.25f / 2.75f;
						return 7.5625f * t * t + 0.9375f;
					}

					t -= 2.625f / 2.75f;
					return 7.5625f * t * t + 0.984375f;

				default:
					return t;
			}
		}
	}
	public enum Ease
	{
		Linear,
		InQuad,
		OutQuad,
		InOutQuad,
		InCubic,
		OutCubic,
		InOutCubic,
		InBack,
		OutBack,
		InOutBack,
		OutElastic,
		OutBounce
	}
}
