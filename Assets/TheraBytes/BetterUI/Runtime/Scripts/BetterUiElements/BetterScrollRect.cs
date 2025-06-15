using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterScrollRect.html")]
    [AddComponentMenu("Better UI/Controls/Better Scroll Rect", 30)]
    public class BetterScrollRect : ScrollRect, IResolutionDependency, IElementCollectionContainer<Selectable>
    {
#if UNITY_5_5_OR_NEWER
        /// <summary>
        /// Exposes the "m_ContentStartPosition" variable which is used as reference position during drag.
        /// It is a variable of the base ScrollRect class which is not accessible by default. 
        /// Use the setter at your own risk.
        /// </summary>
        public Vector2 DragStartPosition
        {
            get { return base.m_ContentStartPosition; }
            set { base.m_ContentStartPosition = value; }
        }

        /// <summary>
        /// Exposes the "m_ContentBounds" variable which is used to evaluate the size of the content.
        /// It is a variable of the base ScrollRect class which is not accessible by default. 
        /// Use ther setter at your own risk.
        /// </summary>
        public Bounds ContentBounds
        {
            get { return base.m_ContentBounds; }
            set { base.m_ContentBounds = value; }
        }
#endif

        public float HorizontalStartPosition
        {
            get { return this.horizontalStartPosition; }
            set { this.horizontalStartPosition = value; }
        }

        public float VerticalStartPosition
        {
            get { return this.verticalStartPosition; }
            set { this.verticalStartPosition = value; }
        }

        public new float horizontalScrollbarSpacing
        {
            get { return base.horizontalScrollbarSpacing; }
            set { Config.Set(value, (o) => base.horizontalScrollbarSpacing = o, (o) => HorizontalSpacingSizer.SetSize(this, o)); }
        }
        public new float verticalScrollbarSpacing
        {
            get { return base.verticalScrollbarSpacing; }
            set { Config.Set(value, (o) => base.verticalScrollbarSpacing = o, (o) => VerticalSpacingSizer.SetSize(this, o)); }
        }
        public new float scrollSensitivity
        {
            get { return base.scrollSensitivity; }
            set { Config.Set(value, (o) => base.scrollSensitivity = o, (o) => ScrollSensitivitySizer.SetSize(this, o)); }
        }

        public FloatSizeModifier HorizontalSpacingSizer { get { return customHorizontalSpacingSizers.GetCurrentItem(horizontalSpacingFallback); } }

        public FloatSizeModifier VerticalSpacingSizer { get { return customVerticalSpacingSizers.GetCurrentItem(verticalSpacingFallback); } }

        public MarginSizeModifier KeepInViewPadding { get { return customKeepInViewPaddingSizers.GetCurrentItem(keepInViewPaddingFallback); } }

        public FloatSizeModifier ScrollSensitivitySizer { get { return scrollSensitivitySizers.GetCurrentItem(scrollSensitivityFallback); } }

        [SerializeField]
        [Range(0, 1)]
        float horizontalStartPosition = 0;

        [SerializeField]
        [Range(0, 1)]
        float verticalStartPosition = 1;

        [SerializeField]
        FloatSizeModifier horizontalSpacingFallback = new FloatSizeModifier(-3, -500, 500);

        [SerializeField]
        FloatSizeConfigCollection customHorizontalSpacingSizers = new FloatSizeConfigCollection();

        [SerializeField]
        FloatSizeModifier verticalSpacingFallback = new FloatSizeModifier(-3, -500, 500);

        [SerializeField]
        FloatSizeConfigCollection customVerticalSpacingSizers = new FloatSizeConfigCollection();

        [SerializeField]
        bool alwaysKeepSelectionInView = true;

        [SerializeField]
        float scrollToSelectionDuration = 0.2f;

        [SerializeField]
        MarginSizeModifier keepInViewPaddingFallback = new MarginSizeModifier(new Margin(), new Margin(), new Margin(1000 * Vector4.one));

        [SerializeField]
        MarginSizeConfigCollection customKeepInViewPaddingSizers = new MarginSizeConfigCollection();

        [SerializeField]
        FloatSizeModifier scrollSensitivityFallback = new FloatSizeModifier(1, 0, 100);

        [SerializeField]
        FloatSizeConfigCollection scrollSensitivitySizers = new FloatSizeConfigCollection();

        SelectableCollection selectableGroup;
        Canvas canvas;
        Canvas Canvas
        {
            get
            {
                if (canvas == null)
                {
                    canvas = GetComponentInParent<Canvas>();
                }

                return canvas;
            }
        }

        ElementCollection<Selectable> IElementCollectionContainer<Selectable>.ElementCollection { get { return selectableGroup; } }

        Coroutine scrollIntoFocusCoroutine;


        protected override void OnEnable()
        {
            base.OnEnable();
            if (selectableGroup == null)
            {
                selectableGroup = new SelectableCollection(CollectingElementsStrategy.CollectWhenDirty);
            }

            if (!selectableGroup.IsInitialized)
            {
                selectableGroup.Initialize(this);
            }

            BetterNavigation.SelectableChanged += SelectableChanged;

            RemainBackwardsCompatible();
            CalculateSize();
        }

        protected override void OnDisable()
        {
            BetterNavigation.SelectableChanged -= SelectableChanged;
            base.OnDisable();
        }


        public void OnResolutionChanged()
        {
            CalculateSize();
        }


        protected override void Start()
        {
            base.Start();

            if (Application.isPlaying)
            {
                ResetToStartPosition();
            }
        }

        private void RemainBackwardsCompatible()
        {
            // earlier Better UI versions didn't have scrollSensitivity. In case of update, keep the legacy value.
            if (base.scrollSensitivity != 1 && scrollSensitivityFallback.OptimizedSize == 1)
            {
                ScrollSensitivitySizer.SetSize(this, base.scrollSensitivity);
            }
        }

        private void SelectableChanged(Selectable previousSelectable, Selectable currentSelectable)
        {
            KeepSelectionInView();
        }

        private void KeepSelectionInView()
        {
            if (!alwaysKeepSelectionInView)
                return;

            var sel = BetterNavigation.LastSelection;
            if (sel == null)
                return;

            if (!selectableGroup.Contains(sel))
                return;

            var rt = sel.transform as RectTransform;
            Rect sr = rt.ToScreenRect(Canvas);        // sr = SelectableRect
            Rect cr = content.ToScreenRect(Canvas);   // cr = ContentRect
            Rect vr = viewRect.ToScreenRect(Canvas);  // vr = ViewportRect
            var pad = KeepInViewPadding.LastCalculatedSize;

            Vector2 target = normalizedPosition;
            if (horizontal)
            {
                if (sr.xMin < vr.xMin)
                {
                    target.x = CalculateTargetPosition(cr.width, vr.width, cr.x, sr.xMin, 0, -pad.Left);
                }
                else if (sr.xMax > vr.xMax)
                {
                    target.x = CalculateTargetPosition(cr.width, vr.width, cr.x, sr.xMax, 1, pad.Right);
                }
            }

            if (vertical)
            {
                if (sr.yMin < vr.yMin)
                {
                    target.y = CalculateTargetPosition(cr.height, vr.height, cr.y, sr.yMin, 0, -pad.Bottom);
                }
                else if (sr.yMax > vr.yMax)
                {
                    target.y = CalculateTargetPosition(cr.height, vr.height, cr.y, sr.yMax, 1, pad.Top);
                }
            }

            if (target != normalizedPosition)
            {
                if (scrollIntoFocusCoroutine != null)
                {
                    StopCoroutine(scrollIntoFocusCoroutine);
                }

                scrollIntoFocusCoroutine = StartCoroutine(ScrollIntoFocus(target));
            }
        }

        private IEnumerator ScrollIntoFocus(Vector2 target)
        {
            float timer = scrollToSelectionDuration;
            Vector2 startPos = normalizedPosition;
            Vector2 previousPosition = normalizedPosition;
            while (timer > 0)
            {
                // if the previous position does not match, the user has interacted -> do not scroll anymore.
                if (previousPosition != normalizedPosition)
                {
                    scrollIntoFocusCoroutine = null;
                    yield break;
                }

                float t = 1 - timer / scrollToSelectionDuration;
                float amount = Mathf.SmoothStep(0, 1, t);
                normalizedPosition = Vector2.Lerp(startPos, target, amount);
                previousPosition = normalizedPosition;

                yield return null;

                timer -= Time.unscaledDeltaTime;
            }

            normalizedPosition = target;
            scrollIntoFocusCoroutine = null;
        }

        float CalculateTargetPosition(float contentSize, float viewportSize, float contentPos, float targetContentPos, float targetViewportPos, float viewportPadding)
        {
            float diffSize = contentSize - viewportSize;
            float p = (targetContentPos + viewportPadding) - contentPos;
            float result = (p - targetViewportPos * viewportSize) / diffSize;
            return Mathf.Clamp01(result);
        }

        public void ResetToStartPosition()
        {
            if (horizontalScrollbar != null)
            {
                horizontalScrollbar.value = horizontalStartPosition;
            }
            else if (horizontal)
            {
                horizontalNormalizedPosition = horizontalStartPosition;
            }

            if (verticalScrollbar != null)
            {
                verticalScrollbar.value = verticalStartPosition;
            }
            else if (vertical)
            {
                verticalNormalizedPosition = verticalStartPosition;
            }
        }

        private void CalculateSize()
        {
            base.horizontalScrollbarSpacing = HorizontalSpacingSizer.CalculateSize(this, nameof(HorizontalSpacingSizer));
            base.verticalScrollbarSpacing = VerticalSpacingSizer.CalculateSize(this, nameof(VerticalSpacingSizer));
            base.scrollSensitivity = ScrollSensitivitySizer.CalculateSize(this, nameof(ScrollSensitivitySizer));

            KeepInViewPadding.CalculateSize(this, nameof(KeepInViewPadding));
        }

        void IElementCollectionContainer<Selectable>.CollectElements(List<Selectable> resultList)
        {
            GetComponentsInChildren(includeInactive: false, result: resultList);
            if (horizontalScrollbar != null)
            {
                resultList.Remove(horizontalScrollbar);
            }

            if (verticalScrollbar != null)
            {
                resultList.Remove(verticalScrollbar);
            }
        }

        Rect IElementCollectionContainer<Selectable>.GetRectOnScreen()
        {
            return content.ToScreenRect();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            CalculateSize();
            base.OnValidate();
        }

#endif
    }
}
