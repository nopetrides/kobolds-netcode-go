#if UNITY_2020_1_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterRectMask2D.html")]
    [AddComponentMenu("Better UI/Controls/Better Rect Mask 2D", 30)]
    public class BetterRectMask2D : RectMask2D, IResolutionDependency
    {
        public new Vector4 padding
        {
            get { return base.padding; }
            set
            {
                Config.Set(value, (o) => base.padding = value,
                    o => PaddingSizer.SetSize(this, new Padding(value)));
            }
        }

        public new Vector2Int softness
        {
            get { return base.softness; }
            set
            {
                Config.Set(value, (o) => base.softness = value, (o) => SoftnessSizer.SetSize(this, value));
            }
        }

        public PaddingSizeModifier PaddingSizer { get { return paddingSizers.GetCurrentItem(paddingFallback); } }

        public Vector2IntSizeModifier SoftnessSizer { get { return softnessSizers.GetCurrentItem(softnessFallback); } }


        [SerializeField]
        PaddingSizeModifier paddingFallback = new PaddingSizeModifier(new Padding(),
                    new Padding(-1000 * Vector4.one), new Padding(1000 * Vector4.one));

        [SerializeField]
        PaddingSizeConfigCollection paddingSizers = new PaddingSizeConfigCollection();


        [SerializeField]
        Vector2IntSizeModifier softnessFallback = new Vector2IntSizeModifier(Vector2Int.zero, Vector2Int.zero, 100 * Vector2Int.one);

        [SerializeField]
        Vector2IntSizeConfigCollection softnessSizers = new Vector2IntSizeConfigCollection();

        protected override void OnEnable()
        {
            base.OnEnable();
            CalculateSizes();
        }

        public void OnResolutionChanged()
        {
            CalculateSizes();
        }

        private void CalculateSizes()
        {
            // as the base doesn't check if the value actually changed (at least in Unity 2019),
            // let's do it here to prevent unnecessary re-calculations.

            var pad = PaddingSizer.CalculateSize(this, nameof(PaddingSizer)).ToVector4();
            if(pad != base.padding)
            {
                base.padding = pad;
            }

            var soft = SoftnessSizer.CalculateSize(this, nameof(SoftnessSizer));
            if (soft != base.softness)
            {
                base.softness = soft;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            CalculateSizes();
        }
#endif
    }
}
#endif
