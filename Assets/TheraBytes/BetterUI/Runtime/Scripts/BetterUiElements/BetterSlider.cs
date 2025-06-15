using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterSlider.html")]
    [AddComponentMenu("Better UI/Controls/Better Slider", 30)]
    public class BetterSlider : Slider, IBetterTransitionUiElement
    {
        public List<Transitions> BetterTransitions { get { return betterTransitions; } }

        [SerializeField, DefaultTransitionStates]
        List<Transitions> betterTransitions = new List<Transitions>();

        [SerializeField] bool moveHandlePivotWithValue;
        [SerializeField]
        AnimationCurve stepSizeOverTime
            = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1), new Keyframe(5, 10));

        [SerializeField] float stepSizeScaleFactor = 1;

        float lastMoveTime, firstMoveTime;

        public AnimationCurve StepSizeOverTime { get { return stepSizeOverTime; } set { stepSizeOverTime = value; } }
        public float StepSizeScaleFactor { get { return stepSizeScaleFactor; } set { stepSizeScaleFactor = value; } }

        bool IsHorizontalNavigation { get { return direction == Direction.LeftToRight || direction == Direction.RightToLeft; } }
        bool IsVerticalNavigation { get { return direction == Direction.TopToBottom || direction == Direction.BottomToTop; } }
        bool IsReversedNavigation { get { return direction == Direction.RightToLeft || direction == Direction.TopToBottom; } }

        float MoveResetTime
        {
            get
            {
                return (BetterNavigation.Current != null && BetterNavigation.Current.InputDetector != null)
                    ? 2.25f * BetterNavigation.Current.InputDetector.ConsecutiveRepeatDelay // x2 is not enough for some reason
                    : 0.3f;
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (!(base.gameObject.activeInHierarchy))
                return;

            foreach (var info in betterTransitions)
            {
                info.SetState(state.ToString(), instant);
            }
        }

        protected override void Set(float input, bool sendCallback = true)
        {
            base.Set(input, sendCallback);

            if (!moveHandlePivotWithValue)
                return;

            Vector2 pivot = handleRect.pivot;

            switch (direction)
            {
                case Direction.LeftToRight:
                    pivot.x = normalizedValue;
                    break;
                case Direction.RightToLeft:
                    pivot.x = 1 - normalizedValue;
                    break;
                case Direction.TopToBottom:
                    pivot.y = 1 - normalizedValue;
                    break;
                case Direction.BottomToTop:
                    pivot.y = normalizedValue;
                    break;
            }

            handleRect.pivot = pivot;
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }
            
            float curveScale = GetCurveScaleFactor();

            // same calculation as inside Slider:
            float stepSize = wholeNumbers ? 1 : (maxValue - minValue) * 0.1f;

            // scale it!
            stepSize = stepSize * stepSizeScaleFactor * curveScale;

            if (wholeNumbers)
            {
                stepSize = Mathf.RoundToInt(stepSize);
            }

            // same logic as inside Slider:
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (IsHorizontalNavigation && FindSelectableOnLeft() == null)
                        Set(IsReversedNavigation ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (IsHorizontalNavigation && FindSelectableOnRight() == null)
                        Set(IsReversedNavigation ? value - stepSize : value + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (IsVerticalNavigation && FindSelectableOnUp() == null)
                        Set(IsReversedNavigation ? value - stepSize : value + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (IsVerticalNavigation && FindSelectableOnDown() == null)
                        Set(IsReversedNavigation ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        private float GetCurveScaleFactor()
        {
            float time = Time.unscaledTime;
            if (time - lastMoveTime <= MoveResetTime)
            {
                float t = time - firstMoveTime;
                lastMoveTime = time;
                return stepSizeOverTime.Evaluate(t);
            }
            else
            {
                firstMoveTime = time;
                lastMoveTime = time;
            }

            return stepSizeOverTime.Evaluate(0);
        }
    }
}

#pragma warning restore 0649 // never assigned warning
