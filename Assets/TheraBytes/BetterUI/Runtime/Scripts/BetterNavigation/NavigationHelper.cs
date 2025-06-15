using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public static class NavigationHelper
    {
        const float EPSILON = 1.0e-05f;

        static BaseEventData baseEventData;
        public static BaseEventData GetBaseEventData(EventSystem eventSystem = null)
        {
            if (eventSystem == null)
            {
                eventSystem = EventSystem.current;
            }

            if (baseEventData == null || baseEventData.selectedObject != eventSystem?.currentSelectedGameObject)
            {
                baseEventData = new BaseEventData(eventSystem);
            }

            baseEventData.Reset();
            return baseEventData;
        }


        public static Selectable FindSelectableInDirection(Selectable selectable, MoveDirection direction, SelectableCollection options)
        {
            if (selectable.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable result = null;
                switch (direction)
                {
                    case MoveDirection.Left: result = selectable.navigation.selectOnLeft; break;
                    case MoveDirection.Right: result = selectable.navigation.selectOnRight; break;
                    case MoveDirection.Up: result = selectable.navigation.selectOnUp; break;
                    case MoveDirection.Down: result = selectable.navigation.selectOnDown; break;
                }

                // at this point we assume the collectables are up to date.
                if (result != null && options.Contains(result, collectElements: false))
                    return result;

                return null;
            }

            Vector3 dir = new Vector3();
            switch (direction)
            {
                case MoveDirection.Left:
                    if (selectable.navigation.mode.HasFlag(Navigation.Mode.Horizontal))
                        dir = Vector3.left;
                    break;

                case MoveDirection.Right:
                    if (selectable.navigation.mode.HasFlag(Navigation.Mode.Horizontal))
                        dir = Vector3.right;
                    break;

                case MoveDirection.Up:
                    if (selectable.navigation.mode.HasFlag(Navigation.Mode.Vertical))
                        dir = Vector3.up;
                    break;

                case MoveDirection.Down:
                    if (selectable.navigation.mode.HasFlag(Navigation.Mode.Vertical))
                        dir = Vector3.down;
                    break;
            }

            if (dir.sqrMagnitude <= EPSILON)
                return null;

            // the following line is bullshit as it rotates the input
            // (if something is rotated 180°, up will navigate downwards).
            // But to be consistent with the Selectable navigation, we do it the same stupid way.
            dir = selectable.transform.rotation * dir;
            return FindSelectableInDirection(selectable.transform as RectTransform, dir, options.Elements);
        }

        /// <summary>
        /// Finds the selectable object next to the provided origin.
        /// </summary>
        /// <remarks>
        /// The direction is determined by a Vector3 variable.
        /// This method is a modified version of <see cref="Selectable.FindSelectable(Vector3)"/>.
        /// </remarks>
        /// <param name="origin">The RectTransform from which another selectable should be found. If the origin is part of the <paramref name="options"/>, it will not be the result (null, in case nothing is found). Usually, the origin is the current selection (<c>EventSystem.current.currentSelectedGameObject</c>.</param>
        /// <param name="dir">The direction in which to search for a neighbouring Selectable object. This vectors x and y values are considered (z should be 0). It is not required to normalize the vector, as it is normalized inside the mehtod.</param>
        /// <param name="options">the collection of selectables that are allowed to be used for finding the right selectable. This may contain <paramref name="origin"/>.</param>
        /// <returns>The neighbouring Selectable object. Null if none found.</returns>
        public static Selectable FindSelectableInDirection(RectTransform origin, Vector3 dir, IEnumerable<Selectable> options)
        {
            return FindElementInDirection(origin, dir, options, SelectableCollection.SelectablePredicate);
        }

        public static T FindElementInDirection<T>(RectTransform origin, Vector3 dir, IEnumerable<T> options, Predicate<T> predicate = null)
            where T : MonoBehaviour
        {
            dir = dir.normalized;

            Vector3 localDir = Quaternion.Inverse(origin.rotation) * dir;
            Vector3 pos = GetPivotScreenPositionOnEdge(origin, localDir) 
                - localDir; // move a bit inside to circumvent the common touching edges scenario
            float maxScore = Mathf.NegativeInfinity;
            T bestPick = null;

            foreach (var o in options)
            {
                if (o.transform == origin || o == null)
                    continue;

                if (predicate != null && !predicate(o))
                    continue;

                var selRect = o.transform as RectTransform;

                Vector3 pivot = selRect != null ? GetPivotScreenPosition(selRect) : Vector3.zero;
                Vector3 myVector = pivot - pos;


                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                float score = dot / myVector.sqrMagnitude;

                if (score > maxScore)
                {
                    maxScore = score;
                    bestPick = o;
                }
            }

            return bestPick;
        }

        public static Vector3 GetPivotScreenPositionOnEdge(RectTransform rectTransform, Vector2 edgeDir)
        {
            // FIXME: the pivot calculation may not work properly for rotated UI.
            Vector2 pivot = rectTransform.pivot;
            if(edgeDir.y != 0)
            {
                pivot.y = (edgeDir.y + 1) / 2; // 0 or 1
            }

            if(edgeDir.x != 0)
            {
                pivot.x = (edgeDir.x + 1) / 2; // 0 or 1
            }

            return GetPivotScreenPosition(rectTransform, pivot);
        }

        public static Vector3 GetPivotScreenPosition(RectTransform rectTransform)
        {
            return GetPivotScreenPosition(rectTransform, rectTransform.pivot);
        }

        public static Vector3 GetPivotScreenPosition(RectTransform rectTransform, Vector2 pivot)
        {
            var rectangle = rectTransform.rect;
            var local = new Vector3(
                Mathf.Lerp(rectangle.xMin, rectangle.xMax, pivot.x), 
                Mathf.Lerp(rectangle.yMin, rectangle.yMax, pivot.y),
                0);

            return rectTransform.TransformPoint(local);
        }

        public static Selectable FindClosestSelectable(Vector2 screenCoord, IEnumerable<Selectable> options)
        {
            return FindClosestElementTo(screenCoord, options, SelectableCollection.SelectablePredicate);
        }

        public static T FindClosestElementTo<T>(Vector2 screenCoord, IEnumerable<T> options, Predicate<T> predicate)
            where T : MonoBehaviour
        {
            float smallestSqDistance = float.MaxValue;
            T closest = null;
            foreach (var o in options)
            {
                if (predicate != null && !predicate(o))
                    continue;

                var rectOnScreen = (o.transform as RectTransform).ToScreenRect();
                var dist = (rectOnScreen.center - screenCoord).sqrMagnitude;
                if (dist < smallestSqDistance)
                {
                    smallestSqDistance = dist;
                    closest = o;
                }
            }

            return closest;
        }

        static float distanceTo45Degree = Mathf.Sin(Mathf.PI / 4f);

        public static Vector2 ToEightWayDirection(float x, float y)
        {
            const float oneThird = 1f / 3f;

            float absX = Mathf.Abs(x);
            float absY = Mathf.Abs(y);

            bool zeroX = absX <= EPSILON;
            bool zeroY = absY <= EPSILON;

            if (zeroX && zeroY)
            {
                return default;
            }

            if (zeroX)
            {
                return new Vector2(0, Mathf.Sign(y));
            }

            if (zeroY)
            {
                return new Vector2(Mathf.Sign(x), 0);
            }

            float signX = Mathf.Sign(x);
            float signY = Mathf.Sign(y);

            if (absX > absY)
            {
                if (absY / absX > oneThird)
                    return new Vector2(signX * distanceTo45Degree, signY * distanceTo45Degree);

                return new Vector2(signX, 0);
            }
            else
            {
                if (absX / absY > oneThird)
                    return new Vector2(signX * distanceTo45Degree, signY * distanceTo45Degree);

                return new Vector2(0, signY);
            }
        }

        public static Vector2 ToFourWayDirection(float x, float y)
        {
            float absX = Mathf.Abs(x);
            float absY = Mathf.Abs(y);

            bool zeroX = absX <= EPSILON;
            bool zeroY = absY <= EPSILON;

            if (zeroX && zeroY)
            {
                return default;
            }

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                return new Vector2(Mathf.Sign(x), 0);
            }
            else
            {
                return new Vector2(0, Mathf.Sign(y));
            }
        }

        public static MoveDirection ToMoveDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= EPSILON)
            {
                return MoveDirection.None;
            }

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return (direction.x < 0) ? MoveDirection.Left : MoveDirection.Right;
            }
            else
            {
                return (direction.y < 0) ? MoveDirection.Down : MoveDirection.Up;
            }
        }

        public static Vector2 ToDirectionVector(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Up: return Vector2.up;
                case MoveDirection.Down: return Vector2.down;
                case MoveDirection.Left: return Vector2.left;
                case MoveDirection.Right: return Vector2.right;
            }

            return default;
        }

        public static Selectable GetSelectableInDirection(MoveDirection moveDirection, Selectable source)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                    return source.FindSelectableOnUp();
                case MoveDirection.Down:
                    return source.FindSelectableOnDown();
                case MoveDirection.Left:
                    return source.FindSelectableOnLeft();
                case MoveDirection.Right:
                    return source.FindSelectableOnRight();
            }

            return null;
        }

        internal static void CollectSelectablesBeneath(Transform root, List<Selectable> resultList,
            bool includeNavigaionGroups, bool includeInactiveChildren)
        {
            if (root == null || !(root is RectTransform))
                return;

            if (!includeInactiveChildren && !root.gameObject.activeInHierarchy)
                return;

#if UNITY_EDITOR
            if (root.TryGetComponent<Selectable>(out _))
            {
                Debug.LogError($"Selectable found in root object {root.name}. Navigation groups should not be a Selectable as well.");
            }
#endif

            for (int i = 0; i < root.childCount; i++)
            {
                CollectSelectablesBeneathRecursive(root.GetChild(i), resultList,
                    includeNavigaionGroups, includeInactiveChildren);
            }
        }

        private static void CollectSelectablesBeneathRecursive(Transform current, List<Selectable> resultList,
            bool includeNavigaionGroups, bool includeInactiveChildren)
        {
            if (current == null)
                return;

            if (!includeInactiveChildren && !current.gameObject.activeSelf)
                return;

            if (!includeNavigaionGroups && current.TryGetComponent<IElementCollectionContainer<Selectable>>(out _))
                return;

            if (current.TryGetComponent<Selectable>(out var selectable))
            {
                resultList.Add(selectable);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                CollectSelectablesBeneathRecursive(current.GetChild(i), resultList,
                    includeNavigaionGroups, includeInactiveChildren);
            }
        }

        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
        {
            if (rect == null)
                return Vector3.zero;

            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));

            var result = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);

            return result;
        }


        internal static bool ShouldIgnoreEventsOnNoFocus()
        {
            OperatingSystemFamily operatingSystemFamily = SystemInfo.operatingSystemFamily;
            if ((uint)(operatingSystemFamily - 1) <= 2u)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isRemoteConnected)
                {
                    return false;
                }
#endif
                return true;
            }

            return false;
        }
    }
}
