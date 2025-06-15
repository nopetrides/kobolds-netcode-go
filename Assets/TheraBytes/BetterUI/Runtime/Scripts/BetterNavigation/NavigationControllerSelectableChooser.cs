using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public class NavigationControllerSelectableChooser : ISelectableChooser
    {
        static readonly Rect empty = new Rect();

        Rect latestSelectableScreenRect;
        MoveDirection switchMoveDirection;
        Vector2 lastFocalScreenPosition;

        public NavigationController NavigationController { get; private set; }

        public Vector2 PreviousSelectableScreenPosition { get { return lastFocalScreenPosition; } }

        public NavigationControllerSelectableChooser(NavigationController navigationController)
        {
            this.NavigationController = navigationController;
        }

        internal void PrepareSwitchingContext(MoveDirection moveDirection, Selectable current)
        {
            // In case we did not had any navigation, the rect is 0 and we should use the current selectable as reference
            if (latestSelectableScreenRect == empty && current != null)
            {
                latestSelectableScreenRect = (current.transform as RectTransform).ToScreenRect();
            }

            lastFocalScreenPosition = latestSelectableScreenRect.center;
            switchMoveDirection = moveDirection;
        }

        internal void NotifyNavigation(MoveDirection direction, Selectable current)
        {
            Rect screenRect = (current.transform as RectTransform).ToScreenRect();
            if (IsNoneOrPerpendicular(switchMoveDirection, direction) || latestSelectableScreenRect == empty)
            {
                latestSelectableScreenRect = screenRect;
            }
            else
            {
                // If the user navigates to the same direction,
                // lets have a middle of all movements in that direction
                // as starting point for the next navigation group
                latestSelectableScreenRect = Rect.MinMaxRect(
                    Mathf.Min(screenRect.xMin, latestSelectableScreenRect.xMin),
                    Mathf.Min(screenRect.yMin, latestSelectableScreenRect.yMin),
                    Mathf.Max(screenRect.xMax, latestSelectableScreenRect.xMax),
                    Mathf.Max(screenRect.yMax, latestSelectableScreenRect.yMax));
            }
        }

        public Selectable ChooseFrom(IEnumerable<Selectable> options, Selectable fallback)
        {
            if (latestSelectableScreenRect == empty)
                return fallback;

            Vector2 pos = lastFocalScreenPosition;
            if (switchMoveDirection != MoveDirection.None)
            {
                bool isHorizontal = switchMoveDirection == MoveDirection.Left || switchMoveDirection == MoveDirection.Right;
                if (isHorizontal)
                {
                    pos.x = (switchMoveDirection == MoveDirection.Left)
                        ? ResolutionMonitor.CurrentResolution.x
                        : 0;
                }
                else
                {
                    pos.y = (switchMoveDirection == MoveDirection.Down)
                        ? ResolutionMonitor.CurrentResolution.y
                        : 0;
                }
            }

            return NavigationHelper.FindClosestSelectable(pos, options);
        }

        bool IsNoneOrPerpendicular(MoveDirection a, MoveDirection b)
        {
            if (a == MoveDirection.None || b == MoveDirection.None)
                return true;

            bool aIsHorizontal = (a == MoveDirection.Left || a == MoveDirection.Right);
            bool bIsHorizontal = (b == MoveDirection.Left || b == MoveDirection.Right);

            return aIsHorizontal != bIsHorizontal;
        }

    }
}
