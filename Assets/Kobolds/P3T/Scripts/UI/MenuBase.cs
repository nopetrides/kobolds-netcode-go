using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace P3T.Scripts.UI
{
	/// <summary>
	///     Base class for menus
	///     Allows for transitioning as menu is revealed or hidden
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(GraphicRaycaster))]
	public class MenuBase : MonoBehaviour
	{
		private TweenerCore<float, float, FloatOptions> _activeTween;

		private Canvas _canvas;

		private CanvasGroup _canvasGroup;

		public int SortOrder
		{
			get => _canvas.sortingOrder;
			set => _canvas.sortingOrder = value;
		}

		private bool IsTweening => _activeTween is {active: true};


		public void OnInstantiate()
		{
			_canvas = GetComponent<Canvas>();
			_canvas.overrideSorting = true;
			_canvasGroup = GetComponent<CanvasGroup>();
			HideFader();
		}

		private void RevealFader()
		{
			_canvasGroup.alpha = 0;
			_canvasGroup.gameObject.SetActive(true);
		}

		private void HideFader()
		{
			_canvasGroup.alpha = 0;
			_canvasGroup.gameObject.SetActive(false);
		}

		public void PerformFullFadeIn(float duration, Action onFadeInComplete = null)
		{
			if (IsTweening)
				_activeTween.Kill();
			else
				RevealFader();

			if (_canvasGroup.isActiveAndEnabled && Mathf.Approximately(_canvasGroup.alpha, 1f))
				onFadeInComplete?.Invoke();
			else
				_activeTween = _canvasGroup.DOFade(1f, duration).OnComplete(() => onFadeInComplete?.Invoke());
		}

		public void PerformHalfFadeIn(float duration, Action onFadeInComplete = null)
		{
			if (IsTweening)
				_activeTween.Kill();
			else
				RevealFader();

			if (_canvasGroup.isActiveAndEnabled && Mathf.Approximately(_canvasGroup.alpha, 0.5f))
				onFadeInComplete?.Invoke();
			else
				_activeTween = _canvasGroup.DOFade(0.5f, duration).SetUpdate(true)
					.OnComplete(() => onFadeInComplete?.Invoke());
		}

		public void PerformFullFadeOut(float duration, Action onFadeOutComplete = null)
		{
			if (IsTweening)
				_activeTween.Kill();

			if (!_canvasGroup.isActiveAndEnabled && Mathf.Approximately(_canvasGroup.alpha, 0f))
				onFadeOutComplete?.Invoke();
			else
				_activeTween = _canvasGroup.DOFade(0f, duration).SetUpdate(true).OnComplete(
					() =>
					{
						HideFader();
						onFadeOutComplete?.Invoke();
					});
		}
	}
}