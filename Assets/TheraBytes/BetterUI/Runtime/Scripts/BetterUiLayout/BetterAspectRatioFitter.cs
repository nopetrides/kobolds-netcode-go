using System;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterAspectRatioFitter.html")]
    [AddComponentMenu("Better UI/Layout/Better Aspect Ratio Fitter", 30)]
    public class BetterAspectRatioFitter : AspectRatioFitter, IResolutionDependency, ILayoutElement, ILayoutIgnorer
    {
        public enum LayoutMode
        {
            CalculatePreferredSize,
            IgnoreLayout,
        }

        [Serializable]
        public class Settings : IScreenConfigConnection
        {
            public AspectMode AspectMode;
            public float AspectRatio = 1;

            [SerializeField]
            string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }
        }

        [Serializable]
        public class SettingsConfigCollection : SizeConfigCollection<Settings> { }

        [SerializeField]
        LayoutMode layoutMode;

        [SerializeField]
        Settings settingsFallback = new Settings();

        [SerializeField]
        SettingsConfigCollection customSettings = new SettingsConfigCollection();


        RectTransform rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = this.transform as RectTransform;
                }

                return rectTransform;
            }
        }

        public Settings CurrentSettings { get { return customSettings.GetCurrentItem(settingsFallback); } }

        public new AspectMode aspectMode
        {
            get { return base.aspectMode; }
            set
            {
                Config.Set(value, (o) => base.aspectMode = value, (o) => CurrentSettings.AspectMode = value);
            }
        }

        public new float aspectRatio
        {
            get { return base.aspectRatio; }
            set
            {
                Config.Set(value, (o) => base.aspectRatio = value, (o) => CurrentSettings.AspectRatio = value);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        public void OnResolutionChanged()
        {
            Apply();
        }

        void Apply()
        {
            base.aspectMode = CurrentSettings.AspectMode;
            base.aspectRatio = CurrentSettings.AspectRatio;
        }



        #region ILayoutElement & ILayoutIgnorer
        float GetLayoutSize(RectTransform.Axis axis)
        {
            if (layoutMode == LayoutMode.IgnoreLayout)
                return -1;

            var rect = RectTransform.rect;
            switch (aspectMode)
            {
                case AspectMode.None:
                    return -1;
                case AspectMode.WidthControlsHeight:
                    if (axis == RectTransform.Axis.Horizontal)
                        return -1;

                    return rect.width / aspectRatio;

                case AspectMode.HeightControlsWidth:
                    if (axis == RectTransform.Axis.Vertical)
                        return -1;

                    return rect.height * aspectRatio;

                case AspectMode.FitInParent:
                case AspectMode.EnvelopeParent:
                    var parent = transform.parent as RectTransform;
                    if (parent == null)
                        return -1;

                    var parentSize = parent.rect.size;
                    Vector2 sizeDelta = Vector2.zero;
                    if ((parentSize.y * aspectRatio < parentSize.x) ^ (aspectMode == AspectMode.FitInParent))
                    {
                        if (axis == RectTransform.Axis.Horizontal)
                            return -1;

                        sizeDelta.y = GetSizeDeltaToProduceSize(parentSize.x, parentSize.x / aspectRatio, 1);
                    }
                    else
                    {
                        if (axis == RectTransform.Axis.Vertical)
                            return -1;

                        sizeDelta.x = GetSizeDeltaToProduceSize(parentSize.y, parentSize.y * aspectRatio, 0);
                    }

                    return rect.size[(int)axis] + sizeDelta[(int)axis];

                default:
                    throw new NotImplementedException();
            }
        }

        private float GetSizeDeltaToProduceSize(float parentSize, float size, int axis)
        {
            return size - parentSize * (RectTransform.anchorMax[axis] - RectTransform.anchorMin[axis]);
        }

        float ILayoutElement.minWidth
        {
            get { return GetLayoutSize(RectTransform.Axis.Horizontal); }
        }
        float ILayoutElement.minHeight
        {
            get { return GetLayoutSize(RectTransform.Axis.Vertical); }
        }

        float ILayoutElement.preferredWidth
        {
            get { return GetLayoutSize(RectTransform.Axis.Horizontal); }
        }

        float ILayoutElement.preferredHeight
        {
            get { return GetLayoutSize(RectTransform.Axis.Vertical); }
        }

        float ILayoutElement.flexibleWidth { get { return -1; } }
        float ILayoutElement.flexibleHeight { get { return -1; } }

        int ILayoutElement.layoutPriority { get { return 1; } }

        bool ILayoutIgnorer.ignoreLayout { get { return layoutMode == LayoutMode.IgnoreLayout; } }

        void ILayoutElement.CalculateLayoutInputHorizontal()
        {
            SetLayoutHorizontal();
        }

        void ILayoutElement.CalculateLayoutInputVertical()
        {
            SetLayoutVertical();
        }
        #endregion

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Apply();
        }
#endif
    }

}

#pragma warning restore 0649 // never assigned warning
