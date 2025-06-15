using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterBorder.html")]
    public class BetterBorder : BaseMeshEffect, IImageAppearanceProvider, IResolutionDependency
    {
        #region nested types
        [Serializable]
        public class BorderSettings : IScreenConfigConnection
        {
            [SerializeField] ColorMode colorMode;
            [SerializeField] Color primaryColor;
            [SerializeField] Color secondaryColor;
            [SerializeField] List<Vector2SizeModifier> offsets;

            [SerializeField]
            string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }

            public ColorMode ColorMode { get { return colorMode; } set { colorMode = value; } }
            public Color PrimaryColor { get { return primaryColor; } set { primaryColor = value; } }
            public Color SecondaryColor { get { return secondaryColor; } set { secondaryColor = value; } }
            public IList<Vector2SizeModifier> Offsets { get { return offsets; } }

            public BorderSettings(ColorMode colorMode, Color primary, Color secondary, params Vector2SizeModifier[] offsets)
            {
                this.colorMode = colorMode;
                this.primaryColor = primary;
                this.secondaryColor = secondary;
                this.offsets = offsets.ToList();
            }

            public BorderSettings()
                : this(ColorMode.Color, Color.white, Color.white,
                      new Vector2SizeModifier(Vector2.one, Vector2.zero, 600 * Vector2.one))
            {
            }
        }

        [Serializable]
        public class BorderSettingsConfigCollection : SizeConfigCollection<BorderSettings> { }
        #endregion

        static List<UIVertex> nonAllocVertices = new List<UIVertex>();

        public ColorMode ColoringMode
        {
            get { return CurrentBorderSettings.ColorMode; }
            set
            {
                CurrentBorderSettings.ColorMode = value;
                graphic?.SetVerticesDirty();
            }
        }

        public Color PrimaryColor
        {
            get { return CurrentBorderSettings.PrimaryColor; }
            set
            {
                CurrentBorderSettings.PrimaryColor = value;
                graphic?.SetVerticesDirty();
            }
        }

        public Color SecondColor
        {
            get { return CurrentBorderSettings.SecondaryColor; }
            set
            {
                CurrentBorderSettings.SecondaryColor = value;
                graphic?.SetVerticesDirty();
            }
        }

        public bool UseGraphicAlpha
        {
            get { return useGraphicAlpha; }
            set
            {
                useGraphicAlpha = value;
                graphic?.SetVerticesDirty();
            }
        }


        public BorderSettings CurrentBorderSettings { get { return settingsCollection.GetCurrentItem(settingsFallback); } }

        public VertexMaterialData MaterialProperties { get { return materialProperties; } }

        Color IImageAppearanceProvider.color { get { return PrimaryColor; } set { PrimaryColor = value; } }

        string IImageAppearanceProvider.MaterialType
        {
            get { return (graphic as IImageAppearanceProvider)?.MaterialType ?? "Default"; }
            set { throw new NotSupportedException("Please set the Material Type in the corresponding Better (Raw) Image instead."); }
        }

        MaterialEffect IImageAppearanceProvider.MaterialEffect
        {
            get { return (graphic as IImageAppearanceProvider)?.MaterialEffect ?? MaterialEffect.Normal; }
            set { throw new NotSupportedException("Please set the Material Effect in the corresponding Better (Raw) Image instead."); }
        }

        Material IImageAppearanceProvider.material { get { return graphic?.material; } }



        [SerializeField] VertexMaterialData materialProperties = new VertexMaterialData();

        [SerializeField]
        BorderSettings settingsFallback = new BorderSettings();

        [SerializeField]
        BorderSettingsConfigCollection settingsCollection = new BorderSettingsConfigCollection();

        [SerializeField]
        float materialProperty1, materialProperty2, materialProperty3;

        [SerializeField]
        bool useGraphicAlpha;

        protected override void OnEnable()
        {
            if (MaterialProperties.FloatProperties != null)
            {
                if (MaterialProperties.FloatProperties.Length > 0)
                    materialProperty1 = MaterialProperties.FloatProperties[0].Value;

                if (MaterialProperties.FloatProperties.Length > 1)
                    materialProperty2 = MaterialProperties.FloatProperties[1].Value;

                if (MaterialProperties.FloatProperties.Length > 2)
                    materialProperty3 = MaterialProperties.FloatProperties[2].Value;
            }

            CalculateSizes();
            base.OnEnable();
        }

        public void OnResolutionChanged()
        {
            CalculateSizes();
        }

        void CalculateSizes()
        {
            var settings = CurrentBorderSettings;
            foreach (var offset in settings.Offsets)
            {
                offset.CalculateSize(this, nameof(offset));
            }

            graphic?.SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            CalculateSizes();
            base.OnValidate();
        }
#endif
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            List<UIVertex> list = nonAllocVertices;
            vh.GetUIVertexStream(list);

            var settings = CurrentBorderSettings;
            int capacity = list.Count * (settings.Offsets.Count + 1);
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }

            int originalCount = list.Count;

            int start = 0;
            int count = list.Count;

            foreach (var o in settings.Offsets)
            {
                Vector2 offset = o.LastCalculatedSize;

                AddOffsettedVertices(list,
                    settings.ColorMode, settings.PrimaryColor, settings.SecondaryColor,
                    start, list.Count, offset.x, offset.y);

                start = count;
                count = list.Count;
            }

            UIVertex vertex = default;
            float uvX = 0;
            float uvY = 0;
            float tangentW = 0;
            materialProperties.Apply(ref uvX, ref uvY, ref tangentW);
            Vector4 uv1 = new Vector4(uvX, uvY);

            for (int i = 0; i < list.Count - originalCount; i++)
            {
                vertex = list[i];

                Vector4 tangent = vertex.tangent;
                tangent.w = tangentW;

                vertex.tangent = tangent;
                vertex.uv1 = uv1;

                list[i] = vertex;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(list);
            list.Clear();
        }


        protected void AddOffsettedVertices(List<UIVertex> verts,
            ColorMode mode, Color32 color, Color secondaryColor,
            int start, int end, float x, float y)
        {
            Rect bounds = graphic.GetPixelAdjustedRect();
            bounds.x += x;
            bounds.y += y;

            for (int i = start; i < end; i++)
            {
                UIVertex uiVertex = verts[i]; // gets a copy
                // add a copy to the end (for the next border or the original image)
                verts.Add(uiVertex); // pass as value

                // then modify the vertex
                Vector3 position = uiVertex.position;
                position.x += x;
                position.y += y;
                uiVertex.position = position;

                var col = ImageAppearanceProviderHelper.GetColor(mode, color, secondaryColor, bounds, position.x, position.y);
                if (useGraphicAlpha)
                {
                    col.a = (byte)(col.a * verts[i].color.a / 255);
                }

                uiVertex.color = col;

                // overwrite the list-entry with the modified vertex
                verts[i] = uiVertex;
            }
        }

        public float GetMaterialPropertyValue(int propertyIndex)
        {
            return ImageAppearanceProviderHelper.GetMaterialPropertyValue(propertyIndex,
                ref materialProperty1, ref materialProperty2, ref materialProperty3);
        }

        public void SetMaterialProperty(int propertyIndex, float value)
        {
            ImageAppearanceProviderHelper.SetMaterialProperty(propertyIndex, value, graphic, materialProperties,
                ref materialProperty1, ref materialProperty2, ref materialProperty3);
        }

        public void SetMaterialDirty()
        {
            graphic.SetMaterialDirty();
        }

        public void SetDirty()
        {
            CalculateSizes();
            graphic.SetAllDirty();
        }

        public BorderSettings GetSettings(string screenConfig)
        {
            return settingsCollection.GetItemForConfig(screenConfig, settingsFallback);
        }

    }
}
