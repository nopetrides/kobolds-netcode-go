using System.Collections.Generic;
using Kobold.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kobold.UI
{
	public class UISelectionIndicator : MonoBehaviour
	{
		private const float MouseMovementThreshold = 0.5f;

		// as long we we check this for null, it should be okay?
		public static GameObject LastValidSelectable;
		[SerializeField] private Transform _safeRoot;
		[SerializeField] private RectTransform _indicator;

		private bool _isUsingMouse;
		private GameObject _lastHovered;
		private Vector2 _lastMousePosition;
		private GameObject _lastSelected;

		private bool _previousInputWasMouse;

		private void Awake()
		{
			if (_safeRoot == null)
				_safeRoot = transform.parent ?? transform;
		}

		private void Update()
		{
			if (!ShouldUpdate()) return;

			DetectInputMode();
			MaybeClearSelectionIfMouseMoved();
			CheckMouseHover();
			CheckSubmitInput();
			UpdateSelectionIndicator();
		}

		private bool ShouldUpdate()
		{
			if (KoboldInputSystemManager.Instance.IsInGameplayMode)
			{
				if (_indicator.gameObject.activeSelf) _indicator.gameObject.SetActive(false);
				return false;
			}

			return true;
		}

		private void DetectInputMode()
		{
			var navInput = Gamepad.current?.leftStick.ReadValue().sqrMagnitude > 0.1f
							|| Gamepad.current?.dpad.ReadValue().sqrMagnitude > 0.1f
							|| Keyboard.current?.anyKey.wasPressedThisFrame == true;

			var mouseMoved = false;
			if (Mouse.current != null)
			{
				var mouseDelta = Mouse.current.delta.ReadValue();
				mouseMoved = mouseDelta.sqrMagnitude > 0.5f;
			}

			// Update input mode
			if (navInput)
				_isUsingMouse = false;
			else if (mouseMoved)
				_isUsingMouse = true;

			// Detect mode change
			if (_isUsingMouse != _previousInputWasMouse)
			{
				if (!_isUsingMouse) OnSwitchedToGamepad();

				_previousInputWasMouse = _isUsingMouse;
			}
		}

		private void OnSwitchedToGamepad()
		{
			if (EventSystem.current.currentSelectedGameObject == null && LastValidSelectable != null)
			{
				EventSystem.current.SetSelectedGameObject(LastValidSelectable);
				_lastSelected = null; // force visual refresh
				UpdateSelectionIndicator();
			}
		}

		private void MaybeClearSelectionIfMouseMoved()
		{
			if (!_isUsingMouse || Mouse.current == null)
				return;

			var selected = EventSystem.current.currentSelectedGameObject;
			if (selected == null)
				return;

			var pointerData = new PointerEventData(EventSystem.current)
			{
				position = Mouse.current.position.ReadValue()
			};

			var raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointerData, raycastResults);

			var hoveringSelected = false;
			foreach (var result in raycastResults)
				if (result.gameObject == selected)
				{
					hoveringSelected = true;
					break;
				}

			if (!hoveringSelected && _isUsingMouse)
			{
				// Only clear selection if we're hovering over something else
				var hoveringNew = false;
				foreach (var result in raycastResults)
					if (result.gameObject.GetComponent<Selectable>() != null)
					{
						hoveringNew = true;
						break;
					}

				if (hoveringNew) EventSystem.current.SetSelectedGameObject(null);
			}
		}

		private void CheckMouseHover()
		{
			if (!_isUsingMouse || Mouse.current == null)
				return;

			var pointerData = new PointerEventData(EventSystem.current)
			{
				position = Mouse.current.position.ReadValue()
			};

			var raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointerData, raycastResults);

			if (raycastResults.Count > 0)
			{
				var hovered = raycastResults[0].gameObject;

				if (hovered &&
					hovered.GetComponent<Selectable>() &&
					hovered != _lastHovered)
				{
					_lastHovered = hovered;
					KoboldAudio.PlayUINavigateSound();
				}
			}
		}

		private void CheckSubmitInput()
		{
			var submitPressed =
				(Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false) ||
				(Keyboard.current?.enterKey.wasPressedThisFrame ?? false) ||
				(Keyboard.current?.spaceKey.wasPressedThisFrame ?? false) ||
				(Mouse.current?.leftButton.wasPressedThisFrame ?? false);

			if (submitPressed && EventSystem.current.currentSelectedGameObject != null) KoboldAudio.PlayUIClickSound();
		}

		private void UpdateSelectionIndicator()
		{
			if (_isUsingMouse)
			{
				if (_indicator && _indicator.gameObject.activeSelf)
					_indicator.gameObject.SetActive(false);
			}
			else
			{
				var current = EventSystem.current.currentSelectedGameObject;
				if (current != null)
					LastValidSelectable = current;
				else return;

				if (current != _lastSelected)
				{
					_lastSelected = current;
					KoboldAudio.PlayUINavigateSound();

					var selectedRect = current.GetComponent<RectTransform>();
					if (selectedRect != null && _indicator != null)
					{
						_indicator.gameObject.SetActive(true);

						// Get world corners of the selected rect
						var worldCorners = new Vector3[4];
						selectedRect.GetWorldCorners(worldCorners);

						// Calculate size and position in world space
						var worldMin = worldCorners[0]; // bottom-left
						var worldMax = worldCorners[2]; // top-right
						var worldCenter = (worldMin + worldMax) * 0.5f;
						Vector2 worldSize = worldMax - worldMin;

						// Reparent to the safe root (persistent canvas)
						_indicator.SetParent(_safeRoot, false);

						// Convert world position to local space of _safeRoot
						Vector2 localPos;
						RectTransformUtility.ScreenPointToLocalPointInRectangle(
							_safeRoot as RectTransform,
							RectTransformUtility.WorldToScreenPoint(null, worldCenter),
							null,
							out localPos
						);

						// Apply final transform
						var indicatorRect = _indicator;
						indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
						indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
						indicatorRect.pivot = new Vector2(0.5f, 0.5f);
						indicatorRect.anchoredPosition = localPos;
						indicatorRect.sizeDelta = worldSize;
						indicatorRect.localRotation = selectedRect.rotation; // Optional: match rotation
					}
				}
			}
		}
	}
}
