using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/ValueDragger.html")]
    [AddComponentMenu("Better UI/Controls/Value Dragger", 30)]
    public class ValueDragger : BetterSelectable, IDragHandler, IBeginDragHandler, IPointerClickHandler
    {
        #region Nested Types
        [Serializable]
        public class ValueDragEvent : UnityEvent<float> { }

        public enum DragDirection
        {
            Horizontal = 0, // maps to Vector2's X index
            Vertical = 1,   // maps to Vector2's Y index
        }

        [Serializable]
        public class DragSettings : IScreenConfigConnection
        {
            public DragDirection Direction = DragDirection.Horizontal;
            public bool Invert;

            [SerializeField]
            string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }
        }

        [Serializable]
        public class DragSettingsConfigCollection : SizeConfigCollection<DragSettings> { }

        [Serializable]
        public class ValueSettings : IScreenConfigConnection
        {
            public bool HasMinValue;
            public float MinValue = 0f;

            public bool HasMaxValue;
            public float MaxValue = 1f;

            public bool WholeNumbers;

            [SerializeField]
            string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }
        }

        [Serializable]
        public class ValueSettingsConfigCollection : SizeConfigCollection<ValueSettings> { }
        #endregion

        [SerializeField]
        DragSettings fallbackDragSettings = new DragSettings();
        [SerializeField]
        DragSettingsConfigCollection customDragSettings = new DragSettingsConfigCollection();

        [SerializeField]
        ValueSettings fallbackValueSettings = new ValueSettings();
        [SerializeField]
        ValueSettingsConfigCollection customValueSettings = new ValueSettingsConfigCollection();

        [SerializeField]
        FloatSizeModifier fallbackDragDistance = new FloatSizeModifier(1, float.Epsilon, 10000);
        [SerializeField]
        FloatSizeConfigCollection customDragDistance = new FloatSizeConfigCollection();

        [SerializeField]
        float value;

        [SerializeField]
        ValueDragEvent onValueChanged = new ValueDragEvent();

        float internalValue;

        public DragSettings CurrentDragSettings
        {
            get { return customDragSettings.GetCurrentItem(fallbackDragSettings); }
        }

        public ValueSettings CurrentValueSettings
        {
            get { return customValueSettings.GetCurrentItem(fallbackValueSettings); }
        }

        public FloatSizeModifier CurrentDragDistanceSizer
        {
            get { return customDragDistance.GetCurrentItem(fallbackDragDistance); }
        }

        public float Value { get { return this.value; } set { ApplyValue(value); } }

        public ValueDragEvent OnValueChanged { get { return onValueChanged; } set { onValueChanged = value; } }

        private float StepSize
        {
            get
            {
                var s = CurrentValueSettings;
                float multiplier = 10; // Consider: clamp it to optimized drag distance sizer to have a max step size of 0.1
                return s.WholeNumbers
                    ? 1f
                    : s.HasMinValue && s.HasMaxValue
                    ? multiplier * (s.MaxValue - s.MinValue) / CurrentDragDistanceSizer.OptimizedSize
                    : multiplier / CurrentDragDistanceSizer.OptimizedSize;
            }
        }

        protected override void Start()
        {
            base.Start();
            ApplyValue(this.value, true);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            internalValue = value;
            CurrentDragDistanceSizer.CalculateSize(this, nameof(CurrentDragDistanceSizer));
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            var dragSettings = CurrentDragSettings;

            int axis = (int)dragSettings.Direction;
            float delta = eventData.delta[axis];
            float divisor = CurrentDragDistanceSizer.LastCalculatedSize;

            internalValue += (dragSettings.Invert)
                ? -delta / divisor
                : delta / divisor;

            ApplyValue(internalValue);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            // consume the click by implementing this interface.
            // we don't want to let the click pass through to a control higher up in the hierarchy.
        }

        private bool ApplyValue(float val, bool force = false)
        {
            var valueSettings = CurrentValueSettings;
            if (valueSettings.HasMinValue && val < valueSettings.MinValue)
            {
                val = valueSettings.MinValue;
            }
            else if (valueSettings.HasMaxValue && val > valueSettings.MaxValue)
            {
                val = valueSettings.MaxValue;
            }

            if (valueSettings.WholeNumbers)
            {
                val = (int)val;
            }

            if (val != value || force)
            {
                value = val;
                onValueChanged.Invoke(value);
                return true;
            }

            return false;
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }
            var ds = CurrentDragSettings;
            bool wasSet = false;

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (ds.Direction == DragDirection.Horizontal && FindSelectableOnLeft() == null)
                    {
                        wasSet = ApplyValue(ds.Invert ? (value + StepSize) : (value - StepSize));
                    }
                    break;
                case MoveDirection.Right:
                    if (ds.Direction == DragDirection.Horizontal && FindSelectableOnRight() == null)
                    {
                        wasSet = ApplyValue(ds.Invert ? (value - StepSize) : (value + StepSize));
                    }
                    break;
                case MoveDirection.Up:
                    if (ds.Direction == DragDirection.Vertical && FindSelectableOnUp() == null)
                    {
                        wasSet = ApplyValue(ds.Invert ? (value - StepSize) : (value + StepSize));
                    }
                    break;
                case MoveDirection.Down:
                    if (ds.Direction == DragDirection.Vertical && FindSelectableOnDown() == null)
                    {
                        wasSet = ApplyValue(ds.Invert ? (value + StepSize) : (value - StepSize));
                    }
                    break;
            }

            if(wasSet)
            {
                eventData.Use();
            }
            else
            {
                base.OnMove(eventData);
            }

        }


        public override Selectable FindSelectableOnLeft()
        {
            var ds = CurrentDragSettings;
            if (base.navigation.mode == Navigation.Mode.Automatic && ds.Direction == DragDirection.Horizontal)
            {
                if(!IsAtThreshold(-1))
                    return null;
            }

            return base.FindSelectableOnLeft();
        }

        public override Selectable FindSelectableOnRight()
        {
            var ds = CurrentDragSettings;
            if (base.navigation.mode == Navigation.Mode.Automatic && ds.Direction == DragDirection.Horizontal)
            {
                if (!IsAtThreshold(+1))
                    return null;
            }

            return base.FindSelectableOnRight();
        }

        public override Selectable FindSelectableOnUp()
        {
            var ds = CurrentDragSettings;
            if (base.navigation.mode == Navigation.Mode.Automatic && ds.Direction == DragDirection.Vertical)
            {
                if (!IsAtThreshold(+1))
                    return null;
            }

            return base.FindSelectableOnUp();
        }

        public override Selectable FindSelectableOnDown()
        {
            var ds = CurrentDragSettings;
            if (base.navigation.mode == Navigation.Mode.Automatic && ds.Direction == DragDirection.Vertical)
            {
                if (!IsAtThreshold(-1))
                    return null;
            }

            return base.FindSelectableOnDown();
        }

        private bool IsAtThreshold(int direction)
        {
            var ds = CurrentDragSettings;
            var vs = CurrentValueSettings;
            if (ds.Invert)
            {
                direction = -direction;
            }

            if (direction < 0)
            {
                if (vs.HasMinValue && value <= vs.MinValue)
                    return true;

                return false;
            }
            else
            {
                if (vs.HasMaxValue && value >= vs.MaxValue)
                    return true;

                return false;
            }
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            CurrentDragDistanceSizer.CalculateSize(this, nameof(CurrentDragDistanceSizer));
        }

#endif
    }
}
