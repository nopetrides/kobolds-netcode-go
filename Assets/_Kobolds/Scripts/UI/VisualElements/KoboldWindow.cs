using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
	/// <summary>
	///     Base class for all Kobold windows/panels with navigation support
	/// </summary>
	public abstract class KoboldWindow : KoboldVisualElement
	{
		private static KoboldWindowManager _windowManager;
		private readonly List<KoboldVisualElement> _animatedChildren = new();
		private bool _hasAnimatedIn;

		public readonly VisualElement ContentContainer;

		protected KoboldWindow(string windowName)
		{
			WindowName = windowName;
			AddToClassList("kobold-window");

			// Create content container
			ContentContainer = new VisualElement();
			ContentContainer.AddToClassList("window-content");
			Add(ContentContainer);

			// Don't auto-animate windows
			AutoAnimateOnAttach = false;
		}

		public string WindowName { get; protected set; }
		public bool IsActive { get; private set; }

		// Stagger animation for children
		public float ChildAnimationStagger { get; set; } = 0.05f;

		protected override void OnAttachedToPanel(AttachToPanelEvent evt)
		{
			base.OnAttachedToPanel(evt);

			// Register with window manager
			if (_windowManager == null)
				_windowManager = KoboldWindowManager.Instance;

			_windowManager?.RegisterWindow(this);

			// Find all animated children
			CollectAnimatedChildren();
		}

		protected override void OnDetachedFromPanel(DetachFromPanelEvent evt)
		{
			base.OnDetachedFromPanel(evt);
			_windowManager?.UnregisterWindow(this);
		}

		public virtual void Show()
		{
			if (IsActive) return;

			IsActive = true;
			style.display = DisplayStyle.Flex;

			// Animate window in
			AnimateIn();

			// Then animate children with stagger
			if (!_hasAnimatedIn)
			{
				AnimateChildrenIn();
				_hasAnimatedIn = true;
			}
		}

		public virtual void Hide(Action onComplete = null)
		{
			if (!IsActive) return;

			IsActive = false;

			// Animate out
			AnimateOut(() =>
			{
				style.display = DisplayStyle.None;
				onComplete?.Invoke();
			});
		}

		protected virtual void AnimateChildrenIn()
		{
			var delay = AnimationDuration; // Start after window animates in

			foreach (var child in _animatedChildren)
			{
				child.AnimateIn(delay);
				delay += ChildAnimationStagger;
			}
		}

		private void CollectAnimatedChildren()
		{
			_animatedChildren.Clear();
			CollectAnimatedChildrenRecursive(ContentContainer);
		}

		private void CollectAnimatedChildrenRecursive(VisualElement p)
		{
			foreach (var child in p.Children())
				if (child is KoboldVisualElement animatedChild)
					_animatedChildren.Add(animatedChild);
				else
					CollectAnimatedChildrenRecursive(child);
		}

		protected override void PrepareForAnimation()
		{
			// Windows slide up and fade in
			style.opacity = 0f;
			style.translate = new StyleTranslate(new Translate(0, 50, 0));
		}

		protected override void OnAnimateInComplete()
		{
			base.OnAnimateInComplete();
			style.translate = new StyleTranslate(new Translate(0, 0, 0));
		}

		/// <summary>
		///     Generic window wrapper for UXML-based content
		/// </summary>
		public class GenericKoboldWindow : KoboldWindow
		{
			public GenericKoboldWindow(string windowName) : base(windowName)
			{
			}

			public void SetContent(VisualElement content)
			{
				ContentContainer.Clear();
				ContentContainer.Add(content);

				// Re-collect animated children after setting content
				CollectAnimatedChildren();
			}
		}
	}

	/// <summary>
	///     Manages window navigation and transitions
	/// </summary>
	public class KoboldWindowManager
	{
		private static KoboldWindowManager _instance;
		private KoboldWindow _currentWindow;
		private readonly Stack<KoboldWindow> _navigationStack = new();

		private readonly Dictionary<string, KoboldWindow> _windows = new();

		public static KoboldWindowManager Instance
		{
			get
			{
				if (_instance == null)
					_instance = new KoboldWindowManager();
				return _instance;
			}
		}

		public void RegisterWindow(KoboldWindow window)
		{
			if (!_windows.ContainsKey(window.WindowName))
				_windows[window.WindowName] = window;
		}

		public void UnregisterWindow(KoboldWindow window)
		{
			if (_windows.ContainsKey(window.WindowName))
				_windows.Remove(window.WindowName);
		}

		public void ShowWindow(string windowName, bool addToStack = true)
		{
			if (!_windows.TryGetValue(windowName, out var window))
			{
				Debug.LogError($"Window '{windowName}' not found!");
				return;
			}

			// Hide current window
			if (_currentWindow != null && _currentWindow != window)
			{
				if (addToStack)
					_navigationStack.Push(_currentWindow);

				_currentWindow.Hide();
			}

			// Show new window
			_currentWindow = window;
			window.Show();
		}

		public void NavigateBack()
		{
			if (_navigationStack.Count == 0) return;

			var previousWindow = _navigationStack.Pop();

			// Hide current
			_currentWindow?.Hide();

			// Show previous
			_currentWindow = previousWindow;
			previousWindow.Show();
		}

		public void HideAllWindows()
		{
			foreach (var window in _windows.Values) window.Hide();

			_currentWindow = null;
			_navigationStack.Clear();
		}
	}
}
