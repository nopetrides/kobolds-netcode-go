using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterRawImage.html")]
    [AddComponentMenu("Better UI/Controls/Better Raw Image", 30)]
    public class BetterRawImage : RawImage, IImageAppearanceProvider, IResolutionDependency
    {
        #region Nested Types
        [Serializable]
        public class TextureSettings : IScreenConfigConnection
        {
            public Texture Texture;
            public ColorMode ColorMode;
            public Color PrimaryColor;
            public Color SecondaryColor;
            public Rect UvRect;
            public FlippingMode FlippingMode;

            [SerializeField]
            string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }


            public TextureSettings(Texture texture, ColorMode colorMode, Color primary, Color secondary, Rect uvRect, FlippingMode flippingMode = FlippingMode.None)
            {
                this.Texture = texture;
                this.ColorMode = colorMode;
                this.PrimaryColor = primary;
                this.SecondaryColor = secondary;
                this.UvRect = uvRect;
                this.FlippingMode = flippingMode;
            }
        }

        [Serializable]
        public class TextureSettingsConfigCollection : SizeConfigCollection<TextureSettings> { }
        #endregion

        public string MaterialType
        {
            get { return materialType; }
            set { ImageAppearanceProviderHelper.SetMaterialType(value, this, materialProperties, ref materialEffect, ref materialType); }
        }

        public MaterialEffect MaterialEffect
        {
            get { return materialEffect; }
            set { ImageAppearanceProviderHelper.SetMaterialEffect(value, this, materialProperties, ref materialEffect, ref materialType); }
        }

        public VertexMaterialData MaterialProperties { get { return materialProperties; } }

        public ColorMode ColoringMode
        {
            get { return colorMode; } 
            set
            {
                Config.Set(value, (o) => colorMode = value, (o) => CurrentTextureSettings.ColorMode = value);
                SetVerticesDirty();
            }
        }
        public Color SecondColor
        { 
            get { return secondColor; } 
            set
            {
                Config.Set(value, (o) => secondColor = value, (o) => CurrentTextureSettings.SecondaryColor = value);
                SetVerticesDirty();
            }
        }

        public override Color color
        {
            get { return base.color; }
            set
            {
                Config.Set(value, (o) => base.color = value, (o) => CurrentTextureSettings.PrimaryColor = value);
            }
        }

        public new Texture texture
        {
            get { return base.texture; }
            set
            {
                Config.Set(value, (o) => base.texture = value, (o) => CurrentTextureSettings.Texture = value);
            }
        }

        public new Rect uvRect
        {
            get { return base.uvRect; }
            set
            {
                Config.Set(value, (o) => base.uvRect = value, (o) => CurrentTextureSettings.UvRect = value);
            }
        }

        public FlippingMode FlippingMode
        {
            get { return flippingMode; }
            set
            {
                Config.Set(value, (o) => flippingMode = value, (o) => CurrentTextureSettings.FlippingMode = value);
            }
        }

#if UNITY_2020_1_OR_NEWER
        public new Vector4 raycastPadding
        {
            get { return base.raycastPadding; }
            set
            {
                Config.Set(value, (o) => base.raycastPadding = value,
                    o => RaycastPaddingSizer.SetSize(this, new Padding(value)));
            }
        }

        public PaddingSizeModifier RaycastPaddingSizer { get { return raycastPaddingSizers.GetCurrentItem(raycastPaddingFallback); } }


        [SerializeField]
        PaddingSizeModifier raycastPaddingFallback = new PaddingSizeModifier(new Padding(),
                    new Padding(-1000 * Vector4.one), new Padding(1000 * Vector4.one));

        [SerializeField]
        PaddingSizeConfigCollection raycastPaddingSizers = new PaddingSizeConfigCollection();
#endif

        [SerializeField]
        ColorMode colorMode = ColorMode.Color;

        [SerializeField]
        Color secondColor = Color.white;

        [SerializeField]
        FlippingMode flippingMode;

        [SerializeField]
        VertexMaterialData materialProperties = new VertexMaterialData();

        [SerializeField]
        string materialType;

        [SerializeField]
        MaterialEffect materialEffect;

        [SerializeField]
        float materialProperty1, materialProperty2, materialProperty3;


        [SerializeField]
        TextureSettings fallbackTextureSettings;

        [SerializeField]
        TextureSettingsConfigCollection customTextureSettings = new TextureSettingsConfigCollection();


        public TextureSettings CurrentTextureSettings 
        { 
            get 
            {
                DoValidation();
                return customTextureSettings.GetCurrentItem(fallbackTextureSettings); 
            } 
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            AssignTextureSettings();
            CalculateSize();

            if (MaterialProperties.FloatProperties != null)
            {
                if (MaterialProperties.FloatProperties.Length > 0)
                    materialProperty1 = MaterialProperties.FloatProperties[0].Value;

                if (MaterialProperties.FloatProperties.Length > 1)
                    materialProperty2 = MaterialProperties.FloatProperties[1].Value;

                if (MaterialProperties.FloatProperties.Length > 2)
                    materialProperty3 = MaterialProperties.FloatProperties[2].Value;
            }
        }

        public float GetMaterialPropertyValue(int propertyIndex)
        {
            return ImageAppearanceProviderHelper.GetMaterialPropertyValue(propertyIndex,
                ref materialProperty1, ref materialProperty2, ref materialProperty3);
        }

        public void SetMaterialProperty(int propertyIndex, float value)
        {
            ImageAppearanceProviderHelper.SetMaterialProperty(propertyIndex, value, this, materialProperties,
                ref materialProperty1, ref materialProperty2, ref materialProperty3);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Rect rect = GetPixelAdjustedRect();

            Vector2 pMin = new Vector2(rect.x, rect.y);
            Vector2 pMax = new Vector2(rect.x + rect.width, rect.y + rect.height);

            float w = (texture != null) ? (float)texture.width * texture.texelSize.x : 1;
            float h = (texture != null) ? (float)texture.height * texture.texelSize.y : 1;
            Vector2 uvMin = new Vector2(this.uvRect.xMin * w, this.uvRect.yMin * h);
            Vector2 uvMax = new Vector2(this.uvRect.xMax * w, this.uvRect.yMax * h);

            ApplyFlipping(flippingMode, ref uvMin, ref uvMax);

            vh.Clear();
            ImageAppearanceProviderHelper.AddQuad(vh, rect,
                pMin, pMax,
                colorMode, color, secondColor,
                uvMin, uvMax,
                materialProperties);
        }

        private void ApplyFlipping(FlippingMode mode, ref Vector2 uvMin, ref Vector2 uvMax)
        {
            switch (mode)
            {
                case FlippingMode.Horizontal:
                    float minX = uvMin.x;
                    uvMin.x = uvMax.x;
                    uvMax.x = minX;
                    break;
                case FlippingMode.Vertical:
                    float minY = uvMin.y;
                    uvMin.y = uvMax.y;
                    uvMax.y = minY;
                    break;
                case FlippingMode.Turn:
                    ApplyFlipping(FlippingMode.Horizontal, ref uvMin, ref uvMax);
                    ApplyFlipping(FlippingMode.Vertical, ref uvMin, ref uvMax);
                    break;
            }
        }

        public void OnResolutionChanged()
        {
            AssignTextureSettings();
            CalculateSize();
        }

        private void AssignTextureSettings()
        {
            var settings = CurrentTextureSettings;

            this.texture = settings.Texture;
            this.colorMode = settings.ColorMode;
            this.color = settings.PrimaryColor;
            this.secondColor = settings.SecondaryColor;
            this.uvRect = settings.UvRect;
            this.flippingMode = settings.FlippingMode;
        }

        private void CalculateSize()
        {
#if UNITY_2020_1_OR_NEWER
            base.raycastPadding = RaycastPaddingSizer.CalculateSize(this, nameof(RaycastPaddingSizer)).ToVector4();
#endif
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            DoValidation();
            AssignTextureSettings();
            CalculateSize();
        }

#endif
        void DoValidation()
        {
            bool isUnInitialized = fallbackTextureSettings == null
                || (fallbackTextureSettings.Texture == null
                && fallbackTextureSettings.ColorMode == ColorMode.Color
                && fallbackTextureSettings.PrimaryColor == new Color()
                && uvRect == new Rect());

            if (isUnInitialized)
            {
                fallbackTextureSettings = new TextureSettings(this.texture, this.colorMode, this.color, this.secondColor, this.uvRect);
            }

#if UNITY_2020_1_OR_NEWER
            // prevent data loss when updating Better UI
            if (raycastPadding != Vector4.zero && RaycastPaddingSizer.OptimizedSize.ToVector4() == Vector4.zero)
            {
                RaycastPaddingSizer.SetSize(this, new Padding(base.raycastPadding));
            }
#endif
        }


    }
}
