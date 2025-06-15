using System;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public static class RectTransformExtensions
    {
        internal static Vector3[] corners = new Vector3[4];
        internal static Vector3[] screenCorners = new Vector3[2];

        /// <summary>
        /// Converts the bounds of the RectTransform to screen coordinates.
        /// </summary>
        /// <param name="self">The RectTransform of interest.</param>
        /// <param name="canvas">If your canvas is set to "Screen Space - Overlay", you can pass <see langword="null"/>. Otherwise, you should pass a reference to the canvas.</param>
        /// <returns>A Rect containing the bounds of the RectTransform in screen coordinates.</returns>
        public static Rect ToScreenRect(this RectTransform self, Canvas canvas = null)
        {
            self.GetWorldCorners(corners);

            if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace))
            {
                screenCorners[0] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
                screenCorners[1] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);
            }
            else
            {
                screenCorners[0] = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                screenCorners[1] = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            }

            return new Rect(screenCorners[0], screenCorners[1] - screenCorners[0]);
        }

        /// <summary>
        /// Sets the pivot to the given value.
        /// </summary>
        /// <param name="self">the RectTransform to modify.</param>
        /// <param name="newPivot">the pivot position that should be assigned.</param>
        /// <param name="retainVisualLocation">If true, the position values of the rect transform are moved so that it remains at the same location.</param>
        public static void SetPivot(this RectTransform self, Vector2 newPivot, bool retainVisualLocation = true)
        {
            if (!retainVisualLocation)
            {
                self.pivot = newPivot;
                return;
            }

            Vector3 preCorner = GetReferenceCorner(self);
            
            self.pivot = newPivot;

            Vector3 postCorner = GetReferenceCorner(self);
            Vector3 diff = postCorner - preCorner;
            self.anchoredPosition -= (Vector2)diff;

            // set world space position in case of World-Canvas
            var pos = self.position;
            pos.z -= diff.z;
            self.position = pos;
        }

        static Vector3 GetReferenceCorner(this RectTransform self)
        {
            self.GetWorldCorners(corners);
            return (!self.parent ? corners[0] : self.parent.InverseTransformPoint(corners[0]));
        }

        /// <summary>
        /// Snaps the anchors of a RectTransform to its borders without changing its visual position or size.
        /// </summary>
        /// <param name="self">The rect-transform to modify</param>
        /// <param name="left">shall the left side (min.x) be affected?</param>
        /// <param name="right">shall the right side (max.x) be affected?</param>
        /// <param name="top">shall the upper side (max.y) be affected?</param>
        /// <param name="bottom">shall the lower side (min.y) be affected?</param>
        public static void SnapAnchorsToBorders(this RectTransform self,
            bool left = true, bool right = true, bool top = true, bool bottom = true)
        {
            if (self.parent == null)
                return;

            Quaternion parentRotation = self.parent.rotation;
            Quaternion objLocalRotation = self.localRotation;
            Vector3 objLocalScale = self.localScale;
            self.parent.rotation = Quaternion.identity;
            self.localRotation = Quaternion.identity;
            self.localScale = Vector3.one;

            RectTransform parentTransform = self.parent as RectTransform;
            Rect parent = (parentTransform != null)
                ? parentTransform.ToScreenRect()
                : new Rect(new Vector2(), ResolutionMonitor.CurrentResolution);

            Rect rect = self.ToScreenRect();

            float sx = CalculateSize(self.sizeDelta.x, left, right);
            float sy = CalculateSize(self.sizeDelta.y, top, bottom);
            float x = CalculateAncherPos(self.pivot.x, sx, left, right, self.anchoredPosition.x);
            float y = CalculateAncherPos(self.pivot.y, sy, top, bottom, self.anchoredPosition.y);

            if (left || bottom)
            {
                float xMin = CalculateMinAnchor(left, rect.xMin, parent.xMin, parent.size.x, self.anchorMin.x);
                float yMin = CalculateMinAnchor(bottom, rect.yMin, parent.yMin, parent.size.y, self.anchorMax.y);
                self.anchorMin = new Vector2(xMin, yMin);
            }

            if (right || top)
            {
                float xMax = CalculateMaxAnchor(right, rect.xMax, parent.xMax, parent.size.x, self.anchorMax.x);
                float yMax = CalculateMaxAnchor(top, rect.yMax, parent.yMax, parent.size.y, self.anchorMin.y);
                self.anchorMax = new Vector2(xMax, yMax);
            }

            self.anchoredPosition = new Vector2(x, y);
            self.sizeDelta = new Vector3(sx, sy);

            self.parent.rotation = parentRotation;
            self.localRotation = objLocalRotation;
            self.localScale = objLocalScale;
        }

        /// <summary>
        /// Snaps the anchors of a RectTransform to a point without changing its visual position or size.
        /// </summary>
        /// <param name="self">The rect-transform to modify</param>
        /// <param name="relativePosition">a coordinate in relative space (0-1).</param>
        /// <param name="isInParentSpace">if true, <paramref name="relativePosition"/> relates to the size of the parent of <paramref name="self"/>, otherwise it relates to the dimension of the <paramref name="self"/> itself.</param>
        /// <param name="horizontal">Shall the anchors be affected horizontally?</param>
        /// <param name="vertical">Shall the anchors be affected vertically?</param>
        public static void SnapAnchorsToPoint(this RectTransform self, Vector2 relativePosition, bool isInParentSpace = false, bool horizontal = true, bool vertical = true)
        {
            if (self == null)
                return;

            Vector2 pivotOffset = GetPivotOffset(self, relativePosition, isInParentSpace);
            Vector2 pivot = self.pivot + pivotOffset;

            Quaternion parentRotation = self.parent.rotation;
            Quaternion objLocalRotation = self.localRotation;
            Vector3 objLocalScale = self.localScale;
            self.parent.rotation = Quaternion.identity;
            self.localRotation = Quaternion.identity;
            self.localScale = Vector3.one;

            RectTransform parentTransform = self.parent as RectTransform;
            Rect parent = (parentTransform != null)
                ? parentTransform.ToScreenRect()
                : new Rect(new Vector2(), ResolutionMonitor.CurrentResolution);

            Rect rect = self.ToScreenRect();

            Vector2 pos = new Vector2(pivot.x * rect.width, pivot.y * rect.height);
            pos += rect.position;
            pos -= parent.position;
            pos.x /= parent.width;
            pos.y /= parent.height;

            Vector2 diff = self.anchoredPosition
                + new Vector2(pivotOffset.x * rect.width, pivotOffset.y * rect.height);

            if (horizontal && vertical)
            {
                self.anchorMin = pos;
                self.anchorMax = pos;
                self.sizeDelta = rect.size;
                self.anchoredPosition -= diff;
            }
            else if (horizontal)
            {
                self.anchorMin = new Vector2(pos.x, self.anchorMin.y);
                self.anchorMax = new Vector2(pos.x, self.anchorMax.y);
                self.sizeDelta = new Vector2(rect.size.x, self.sizeDelta.y);
                self.anchoredPosition -= new Vector2(diff.x, 0);
            }
            else if (vertical)
            {
                self.anchorMin = new Vector2(self.anchorMin.x, pos.y);
                self.anchorMax = new Vector2(self.anchorMax.x, pos.y);
                self.sizeDelta = new Vector2(self.sizeDelta.x, rect.size.y);
                self.anchoredPosition -= new Vector2(0, diff.y);
            }

            self.parent.rotation = parentRotation;
            self.localRotation = objLocalRotation;
            self.localScale = objLocalScale;
        }


        /// <summary>
        /// Changes the size and anchors of a RectTransform to enclose all its direct children.
        /// </summary>
        /// <remarks>If the RectTransform doesn't have children, nothing is changed.</remarks>
        /// <param name="self">The RectTransform to modify.</param>
        /// <param name="snapHorizontally">Shall the anchors and size be changed horizontally?</param>
        /// <param name="snapVertically">Shall the anchors and size be changed vertically?</param>
        /// <param name="retainChildLocations">If true, all the children of the object are inverse-transformed so that they retain their original visual position and size. Otherwise they might move / change size depending on their anchors.</param>
        /// <param name="recordUndo"><i>Ignored in builds</i> - If true, the user can undo the operation.</param>
        public static void SnapSizeAndAnchorsToChildren(this RectTransform self, bool snapHorizontally = true, bool snapVertically = true, bool retainChildLocations = true, bool recordUndo = false)
        {
            if (self == null || self.parent == null)
                return;

            if (self.childCount == 0)
                return;

            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            foreach (var child in self)
            {
                var rt = child as RectTransform;
                if (rt == null || !rt.gameObject.activeSelf)
                    continue;

                Rect rect = rt.ToScreenRect();

                xMin = Mathf.Min(xMin, rect.xMin);
                yMin = Mathf.Min(yMin, rect.yMin);
                xMax = Mathf.Max(xMax, rect.xMax);
                yMax = Mathf.Max(yMax, rect.yMax);
            }

            Rect childBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            var parent = self.parent as RectTransform;
            Rect parentRect = (parent != null)
                ? parent.ToScreenRect()
                : new Rect(new Vector2(), ResolutionMonitor.CurrentResolution);

            RectTransformData prev = new RectTransformData(self);
            RectTransformData cur = new RectTransformData().PullFromData(prev);


            if (snapHorizontally)
            {
                SnapSizeAndAnchorAlongAxis(cur, 0, childBounds.xMin, childBounds.xMax, parentRect.xMin, parentRect.width);
            }

            if (snapVertically)
            {
                SnapSizeAndAnchorAlongAxis(cur, 1, childBounds.yMin, childBounds.yMax, parentRect.yMin, parentRect.height);
            }

            #region do actual operation with undo

#if UNITY_EDITOR
            int group = -1;
            if (recordUndo)
            {
                Undo.RecordObject(self, "Snap To Children " + DateTime.Now.ToFileTime());
                group = Undo.GetCurrentGroup();
            }
#endif
            // push!
            cur.PushToTransform(self);

#if UNITY_EDITOR
            if (recordUndo)
            {
                foreach (Transform child in self)
                {
                    Undo.RecordObject(child, "transform child");
                }
            }
#endif

            if (retainChildLocations)
            {
                // update child positions
                MoveChildsToRetainPreviousLocations(self, cur, prev);
            }

#if UNITY_EDITOR
            if (recordUndo)
            {
                Undo.CollapseUndoOperations(group);
            }
#endif
            #endregion
        }

        /// <summary>
        /// Moves all children of a RectTransform to retain the position and size of the previous position of their parent.
        /// </summary>
        /// <remarks>You can call this before or after changing the position of the parent object - or just use it without changing the parent at all (maybe to some weird animations). The actual transform of the parent object is not evaluated in this method.</remarks>
        /// <param name="self">the parent object whose direct childs should be moved.</param>
        /// <param name="newTransform">The <see cref="RectTransformData"/> of the previous location of <paramref name="obj"/>.</param>
        /// <param name="oldTransform">The <see cref="RectTransformData"/> of the new location of <paramref name="obj"/>.</param>
        public static void MoveChildsToRetainPreviousLocations(this RectTransform self, RectTransformData newTransform, RectTransformData oldTransform)
        {
            if (self == null)
                return;

            if (newTransform == oldTransform)
                return;

            RectTransform parent = self.parent as RectTransform;
            Rect parentRect = parent.rect;

            Rect cur = newTransform.ToRect(parentRect, relativeSpace: true);
            bool isCurZero = Mathf.Approximately(cur.width, 0) || Mathf.Approximately(cur.height, 0);

            Rect prev = oldTransform.ToRect(parentRect, relativeSpace: true);
            bool isPrevZero = Mathf.Approximately(prev.width, 0) || Mathf.Approximately(prev.height, 0);

            if (isCurZero || isPrevZero)
            {
                return;
            }

            float scaleH = 1 / cur.width;
            float scaleV = 1 / cur.height;

            foreach (var child in self)
            {
                RectTransform rt = child as RectTransform;
                if (rt == null)
                    continue;

                // prev to parent-parent-relative-space
                float xMin = prev.x + prev.width * rt.anchorMin.x;
                float xMax = prev.x + prev.width * rt.anchorMax.x;

                float yMin = prev.y + prev.height * rt.anchorMin.y;
                float yMax = prev.y + prev.height * rt.anchorMax.y;

                // parent-parent-relative-space to cur
                xMin = xMin * scaleH - cur.x * scaleH;
                xMax = xMax * scaleH - cur.x * scaleH;

                yMin = yMin * scaleV - cur.y * scaleV;
                yMax = yMax * scaleV - cur.y * scaleV;

                // assign calculated values
                rt.anchorMin = new Vector2(xMin, yMin);
                rt.anchorMax = new Vector2(xMax, yMax);
            }
        }

        /// <summary>
        /// Sets the anchors to the border of the parent and removes any positioning or sizing, so that it covers the area of the parent.
        /// </summary>
        /// <param name="self">the RectTransform to modify.</param>
        /// <param name="horizontal">Shall it be expanded horizontally?</param>
        /// <param name="vertical">Shall it be expanded vertically?</param>
        public static void ExpandToParentSize(this RectTransform self, bool horizontal, bool vertical)
        {
            float xMin = horizontal ? 0 : self.anchorMin.x;
            float yMin = vertical ? 0 : self.anchorMin.y;

            float xMax = horizontal ? 1 : self.anchorMax.x;
            float yMax = vertical ? 1 : self.anchorMax.y;

            float xSize = horizontal ? 0 : self.sizeDelta.x;
            float ySize = vertical ? 0 : self.sizeDelta.y;

            float xPos = horizontal ? 0 : self.anchoredPosition.x;
            float yPos = vertical ? 0 : self.anchoredPosition.y;

            self.anchorMin = new Vector2(xMin, yMin);
            self.anchorMax = new Vector2(xMax, yMax);
            self.sizeDelta = new Vector2(xSize, ySize);
            self.anchoredPosition = new Vector2(xPos, yPos);
        }

        /// <summary>
        /// Gets the difference between the given relative position and the pivot (basically "relativePosition - pivot").
        /// In case <paramref name="parentPosition"/> is true, it transposes the relativePosition form parent to local space first.
        /// </summary>
        /// <param name="self">the RectTransform to operate on</param>
        /// <param name="relativePosition">The position of interest in relative coordinates (0-1)</param>
        /// <param name="parentPosition">if true, the <paramref name="relativePosition"/> is transposed into local space before calculation.</param>
        /// <returns>The distance between the pivot and the givel position.</returns>
        public static Vector2 GetPivotOffset(this RectTransform self, Vector2 relativePosition, bool parentPosition)
        {
            Vector2 result;

            if (parentPosition)
            {

                RectTransform parentTransform = self.parent as RectTransform;
                Rect parent = (parentTransform != null)
                    ? parentTransform.ToScreenRect()
                    : new Rect(new Vector2(), ResolutionMonitor.CurrentResolution);

                Rect rect = self.ToScreenRect();
                Vector2 p = relativePosition;

                result = new Vector2(p.x * parent.width, p.y * parent.height);
                result += parent.position;
                result -= rect.position;
                result = new Vector2(result.x / rect.width, result.y / rect.height) - self.pivot;
            }
            else
            {
                result = relativePosition - self.pivot;
            }

            return result;
        }

        #region internal helper methods
        static float CalculateMinAnchor(bool calculate, float innerPos, float outerPos, float outerSize, float fallback)
        {
            return (calculate) ? (innerPos - outerPos) / outerSize : fallback;
        }

        static float CalculateMaxAnchor(bool calculate, float innerPos, float outerPos, float outerSize, float fallback)
        {
            return (calculate) ? 1 - ((outerPos - innerPos) / outerSize) : fallback;
        }

        static float CalculateSize(float size, bool front, bool back)
        {
            return (front && back) ? 0
                : (front || back) ? 0.5f * size
                : size;
        }

        static float CalculateAncherPos(float pivot, float size, bool front, bool back, float fallback)
        {
            if (!(front) && !(back))
                return fallback;

            return 0.5f * size - pivot * size;
        }


        private static void SnapSizeAndAnchorAlongAxis(RectTransformData data, int axis, float min, float max, float parentMin, float parentSize)
        {
            data.AnchorMin[axis] = (min - parentMin) / parentSize;
            data.AnchorMax[axis] = (max - parentMin) / parentSize;

            data.AnchoredPosition[axis] = 0;
            data.SizeDelta[axis] = 0;
        }

        #endregion
    }
}
