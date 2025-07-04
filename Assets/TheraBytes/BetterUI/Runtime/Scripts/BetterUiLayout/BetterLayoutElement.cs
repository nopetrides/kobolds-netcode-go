﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterLayoutElement.html")]
    [AddComponentMenu("Better UI/Layout/Better Layout Element", 30)]
    public class BetterLayoutElement : LayoutElement, IResolutionDependency
    {
        [Serializable]
        public class Settings : IScreenConfigConnection
        {
            public bool IgnoreLayout;
            public bool MinWidthEnabled, MinHeightEnabled;
            public bool PreferredWidthEnabled, PreferredHeightEnabled;
            public bool FlexibleWidthEnabled, FlexibleHeightEnabled;
            public float FlexibleWidth = 1;
            public float FlexibleHeight = 1;

            [SerializeField]
            string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }
        }

        [Serializable]
        public class SettingsConfigCollection : SizeConfigCollection<Settings> { }

        public Settings CurrentSettings { get { return customSettings.GetCurrentItem(settingsFallback); } }

        [SerializeField]
        Settings settingsFallback = new Settings();

        [SerializeField]
        SettingsConfigCollection customSettings = new SettingsConfigCollection();

        public FloatSizeModifier MinWidthSizer { get { return customMinWidthSizers.GetCurrentItem(minWidthSizerFallback); } }
        public FloatSizeModifier MinHeightSizer { get { return customMinHeightSizers.GetCurrentItem(minHeightSizerFallback); } }
        public FloatSizeModifier PreferredWidthSizer { get { return customPreferredWidthSizers.GetCurrentItem(preferredWidthSizerFallback); } }
        public FloatSizeModifier PreferredHeightSizer { get { return customPreferredHeightSizers.GetCurrentItem(preferredHeightSizerFallback); } }

        public new bool ignoreLayout
        {
            get { return base.ignoreLayout; }
            set { Config.Set(value, (o) => base.ignoreLayout = o, (o) => CurrentSettings.IgnoreLayout = o); }
        }
        public new float flexibleWidth
        {
            get { return base.flexibleWidth; }
            set { Config.Set(value, (o) => base.flexibleWidth = o, (o) => CurrentSettings.FlexibleWidth = o); }
        }
        public new float flexibleHeight
        {
            get { return base.flexibleHeight; }
            set { Config.Set(value, (o) => base.flexibleHeight = o, (o) => CurrentSettings.FlexibleHeight = o); }
        }
        public new float minWidth
        {
            get { return base.minWidth; }
            set { Config.Set(value, (o) => base.minWidth = o, (o) => MinWidthSizer.SetSize(this, o)); }
        }
        public new float minHeight
        {
            get { return base.minHeight; }
            set { Config.Set(value, (o) => base.minHeight = o, (o) => MinHeightSizer.SetSize(this, o)); }
        }
        public new float preferredWidth
        {
            get { return base.preferredWidth; }
            set { Config.Set(value, (o) => base.preferredWidth = o, (o) => PreferredWidthSizer.SetSize(this, o)); }
        }
        public new float preferredHeight
        {
            get { return base.preferredHeight; }
            set { Config.Set(value, (o) => base.preferredHeight = o, (o) => PreferredHeightSizer.SetSize(this, o)); }
        }

        [SerializeField]
        FloatSizeModifier minWidthSizerFallback = new FloatSizeModifier(0, 0, 5000);
        [SerializeField]
        FloatSizeConfigCollection customMinWidthSizers = new FloatSizeConfigCollection();

        [SerializeField]
        FloatSizeModifier minHeightSizerFallback = new FloatSizeModifier(0, 0, 5000);
        [SerializeField]
        FloatSizeConfigCollection customMinHeightSizers = new FloatSizeConfigCollection();

        [SerializeField]
        FloatSizeModifier preferredWidthSizerFallback = new FloatSizeModifier(100, 0, 5000);
        [SerializeField]
        FloatSizeConfigCollection customPreferredWidthSizers = new FloatSizeConfigCollection();

        [SerializeField]
        FloatSizeModifier preferredHeightSizerFallback = new FloatSizeModifier(100, 0, 5000);
        [SerializeField]
        FloatSizeConfigCollection customPreferredHeightSizers = new FloatSizeConfigCollection();


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
            Settings s = CurrentSettings;

            base.ignoreLayout = s.IgnoreLayout;

            base.minWidth = (s.MinWidthEnabled) ? MinWidthSizer.CalculateSize(this, nameof(MinWidthSizer)) : -1;
            base.minHeight = (s.MinHeightEnabled) ? MinHeightSizer.CalculateSize(this, nameof(MinHeightSizer)) : -1;
            base.preferredWidth = (s.PreferredWidthEnabled) ? PreferredWidthSizer.CalculateSize(this, nameof(PreferredWidthSizer)) : -1;
            base.preferredHeight = (s.PreferredHeightEnabled) ? PreferredHeightSizer.CalculateSize(this, nameof(PreferredHeightSizer)) : -1;
            base.flexibleWidth = (s.FlexibleWidthEnabled) ? s.FlexibleWidth : -1;
            base.flexibleHeight = (s.FlexibleHeightEnabled) ? s.FlexibleHeight : -1;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Apply();
        }
#endif
    }
}
