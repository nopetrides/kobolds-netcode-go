using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.ShortcutManagement;

namespace TheraBytes.BetterUi.Editor
{
    public static class Shortcuts
    {
        // Snap Borders
        [Shortcut("Better UI/Snap Anchors to Borders - All Directions", KeyCode.KeypadPlus)]
        public static void SnapAnchorsToAllBorders()
        {
            SnapAnchorsToBorders(true, true, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Borders - Horizontal", KeyCode.KeypadMinus)]
        public static void SnapAnchorsToBordersHorizontally()
        {
            SnapAnchorsToBorders(true, true, false, false);
        }

        [Shortcut("Better UI/Snap Anchors to Borders - Vertical", KeyCode.KeypadDivide)]
        public static void SnapAnchorsToBordersVertically()
        {
            SnapAnchorsToBorders(false, false, true, true);
        }

        // Expand to parent
        [Shortcut("Better UI/Expand To Parent Size - All Directions", KeyCode.KeypadPlus, ShortcutModifiers.Alt)]
        public static void ExpandToParentSizeAll()
        {
            ExpandToParentSize(true, true);
        }


        [Shortcut("Better UI/Expand To Parent Size - Horizontal", KeyCode.KeypadMinus, ShortcutModifiers.Alt)]
        public static void ExpandToParentSizeHorizontally()
        {
            ExpandToParentSize(true, false);
        }

        [Shortcut("Better UI/Expand To Parent Size - Vertical", KeyCode.KeypadDivide, ShortcutModifiers.Alt)]
        public static void ExpandToParentSizeVertically()
        {
            ExpandToParentSize(false, true);
        }

        // Snap Point
        [Shortcut("Better UI/Snap Anchors to Point (Horizontal) - Left", KeyCode.Keypad4)]
        public static void SnapAnchorsToLeft()
        {
            SnapAnchorsToPoint(new Vector2(0, 0), true, false, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point (Horizontal) - Right", KeyCode.Keypad6)]
        public static void SnapAnchorsToRight()
        {
            SnapAnchorsToPoint(new Vector2(1, 0), true, false, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point (Vertical) - Top", KeyCode.Keypad8)]
        public static void SnapAnchorsToTop()
        {
            SnapAnchorsToPoint(new Vector2(0, 1), false, true, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point (Vertical) - Bottom", KeyCode.Keypad2)]
        public static void SnapAnchorsToBottom()
        {
            SnapAnchorsToPoint(new Vector2(0, 0), false, true, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point - Center", KeyCode.Keypad5)]
        public static void SnapAnchorsToCenter()
        {
            SnapAnchorsToPoint(new Vector2(0.5f, 0.5f), true, true, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point - Top Left", KeyCode.Keypad7)]
        public static void SnapAnchorsToTopLeft()
        {
            SnapAnchorsToPoint(new Vector2(0, 1), true, true, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point - Top Right", KeyCode.Keypad9)]
        public static void SnapAnchorsToTopRight()
        {
            SnapAnchorsToPoint(new Vector2(1, 1), true, true, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point - Bottom Left", KeyCode.Keypad1)]
        public static void SnapAnchorsToBottomLeft()
        {
            SnapAnchorsToPoint(new Vector2(0, 0), true, true, false);
        }

        [Shortcut("Better UI/Snap Anchors to Point - Bottom Right", KeyCode.Keypad3)]
        public static void SnapAnchorsToBottomRight()
        {
            SnapAnchorsToPoint(new Vector2(1, 0), true, true, false);
        }


        // Snap to Point - Parent

        [Shortcut("Better UI/Snap Anchors to Parent Point (Horizontal) - Left", KeyCode.Keypad4, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentLeft()
        {
            SnapAnchorsToPoint(new Vector2(0, 0), true, false, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point (Horizontal) - Right", KeyCode.Keypad6, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentRight()
        {
            SnapAnchorsToPoint(new Vector2(1, 0), true, false, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point (Vertical) - Top", KeyCode.Keypad8, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentTop()
        {
            SnapAnchorsToPoint(new Vector2(0, 1), false, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point (Vertical) - Bottom", KeyCode.Keypad2, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentBottom()
        {
            SnapAnchorsToPoint(new Vector2(0, 0), false, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point - Center", KeyCode.Keypad5, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentCenter()
        {
            SnapAnchorsToPoint(new Vector2(0.5f, 0.5f), true, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point - Top Left", KeyCode.Keypad7, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentTopLeft()
        {
            SnapAnchorsToPoint(new Vector2(0, 1), true, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point - Top Right", KeyCode.Keypad9, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentTopRight()
        {
            SnapAnchorsToPoint(new Vector2(1, 1), true, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point - Bottom Left", KeyCode.Keypad1, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentBottomLeft()
        {
            SnapAnchorsToPoint(new Vector2(0, 0), true, true, true);
        }

        [Shortcut("Better UI/Snap Anchors to Parent Point - Bottom Right", KeyCode.Keypad3, ShortcutModifiers.Alt)]
        public static void SnapAnchorsToParentBottomRight()
        {
            SnapAnchorsToPoint(new Vector2(1, 0), true, true, true);
        }

        // Set Pivot
        [Shortcut("Better UI/Set Pivot - Middle of Anchors", KeyCode.Keypad0)]
        public static void SetPivotToMiddleOfAnchors()
        {
            Undo.SetCurrentGroupName("SetPivot" + DateTime.Now.ToFileTime());
            int group = Undo.GetCurrentGroup();

            foreach (var tr in Selection.transforms)
            {
                if (tr is RectTransform rt)
                {
                    Undo.RecordObject(rt, "Set Pivot");
                    Vector2 parentPos = 0.5f * (rt.anchorMin + rt.anchorMax);
                    Vector2 pivotOffset = rt.GetPivotOffset(parentPos, true);

                    rt.SetPivot(rt.pivot + pivotOffset);
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        // Enclose Children
        [Shortcut("Better UI/Enclose Children - All Directions", KeyCode.KeypadPlus, ShortcutModifiers.Action)]
        public static void EncloseChildren()
        {
            RectTransform rt = Selection.activeTransform as RectTransform;
            if (rt == null)
                return;

            rt.SnapSizeAndAnchorsToChildren(
                retainChildLocations: !SmartParentWindow.IsFreeMovementEnabled, recordUndo: true);
        }

        [Shortcut("Better UI/Enclose Children - Horizontal", KeyCode.KeypadMinus, ShortcutModifiers.Action)]
        public static void EncloseChildrenHorizontally()
        {
            RectTransform rt = Selection.activeTransform as RectTransform;
            if (rt == null)
                return;

            rt.SnapSizeAndAnchorsToChildren(true, false,
                retainChildLocations: !SmartParentWindow.IsFreeMovementEnabled, recordUndo: true);
        }

        [Shortcut("Better UI/Enclose Children - Vertical", KeyCode.KeypadDivide, ShortcutModifiers.Action)]
        public static void EncloseChildrenVertically()
        {
            RectTransform rt = Selection.activeTransform as RectTransform;
            if (rt == null)
                return;

            rt.SnapSizeAndAnchorsToChildren(false, true, 
                retainChildLocations: !SmartParentWindow.IsFreeMovementEnabled, recordUndo: true);
        }

        [Shortcut("Better UI/Retain Child Positions - Toggle On / Off", KeyCode.KeypadMultiply, ShortcutModifiers.Action)]
        public static void DetachChildrenToggle()
        {
            SmartParentWindow.IsFreeMovementEnabled = !SmartParentWindow.IsFreeMovementEnabled;
        }

        // Select
        [Shortcut("Better UI/Select - Next In Hierarchy", KeyCode.KeypadEnter)]
        public static void SelectNextInHierarchy()
        {
            if(Selection.activeGameObject == null)
            {
                Selection.activeGameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault();
                return;
            }

            if (!TrySelectFirstChild(Selection.activeTransform))
            {
                Transform transform = Selection.activeTransform;
                while (!TrySelectNextSibling(transform))
                {
                    transform = transform.parent;
                }
            }
        }

        [Shortcut("Better UI/Select - Next Sibling", KeyCode.KeypadEnter, ShortcutModifiers.Shift)]
        public static void SelectNextSibling()
        {
            if (Selection.activeTransform == null)
            {
                Selection.activeGameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault();
                return;
            }

            if(!TrySelectNextSibling(Selection.activeTransform))
            {
                Selection.activeTransform = Selection.activeTransform.parent?.GetChild(0);
            }
        }

        [Shortcut("Better UI/Select - Parent", KeyCode.KeypadEnter, ShortcutModifiers.Action)]
        public static void SelectParent()
        {
            if (Selection.activeTransform == null)
                return;

            if (Selection.activeTransform.parent == null)
                return;

            Selection.activeTransform = Selection.activeTransform.parent;
        }

        // helper methods
        public static bool TrySelectFirstChild(Transform selection)
        {
            if (selection.childCount == 0)
                return false;

            Selection.activeTransform = selection.GetChild(0);
            return true;
        }

        private static bool TrySelectNextSibling(Transform selection)
        {
            var parent = selection?.parent;
            if (parent == null)
            {
                var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
                int index = Mathf.Clamp(rootObjects.IndexOf(selection.gameObject), 0, rootObjects.Count);

                // not what the method name would suggest, but as this is always the behavior we want, it is in here:
                // at the root level, skip to the first object when the last on was reached.
                Selection.activeObject = rootObjects[(index + 1) % rootObjects.Count];
                return true;
            }
            else
            {
                for (int i = 0; i < parent.childCount - 1; i++)
                {
                    if (parent.GetChild(i) == selection)
                    {
                        Selection.activeTransform = parent.GetChild(i + 1);
                        return true;
                    }
                }
            }
            
            return false;
        }


        static void SnapAnchorsToBorders(bool left, bool right, bool top, bool bottom)
        {
            Undo.SetCurrentGroupName("SnapBorder" + DateTime.Now.ToFileTime());
            int group = Undo.GetCurrentGroup();

            foreach (var tr in Selection.transforms)
            {
                if (tr is RectTransform rt)
                {
                    Undo.RecordObject(rt, "Snap Anchors Border");
                    rt.SnapAnchorsToBorders(left, right, top, bottom);
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        static void SnapAnchorsToPoint(Vector2 relativePosition, bool horizontal, bool vertical, bool parentPosition)
        {
            Undo.SetCurrentGroupName("SnapPoint" + DateTime.Now.ToFileTime());
            int group = Undo.GetCurrentGroup();

            foreach (var tr in Selection.transforms)
            {
                if (tr is RectTransform rt)
                {
                    Undo.RecordObject(rt, "Snap Anchors Point");
                    rt.SnapAnchorsToPoint(relativePosition, parentPosition, horizontal, vertical);
                }
            }

            Undo.CollapseUndoOperations(group);
        }


        private static void ExpandToParentSize(bool horizontal, bool vertical)
        {
            Undo.SetCurrentGroupName("ExpandSize" + DateTime.Now.ToFileTime());
            int group = Undo.GetCurrentGroup();

            foreach (var tr in Selection.transforms)
            {
                if (tr is RectTransform rt)
                {
                    Undo.RecordObject(rt, "Expand Size");
                    rt.ExpandToParentSize(horizontal, vertical);
                }
            }

            Undo.CollapseUndoOperations(group);
        }

    }
}
