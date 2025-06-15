using System;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    [Serializable]
    public class Padding : MarginOrPadding<float>
    {
        public float Horizontal { get { return left + right; } }
        public float Vertical { get { return top + bottom; } }

        public Padding()
            : base(0, 0, 0, 0)
        { }

        public Padding(float left, float right, float top, float bottom)
            : base(left, right, top, bottom)
        { }

        public Padding(RectOffset source)
            : base(source.left, source.right, source.top, source.bottom)
        {
        }

        public Padding Clone()
        {
            return new Padding(this.left, this.right, this.top, this.bottom);
        }

        public Padding(Vector4 source, Vector4Order order = Vector4Order.LeftBottomRightTop)
            : base(source, order)
        { }


        protected override void SetFloats(float left, float right, float top, float bottom)
        {
            base.Set(left, right, top, bottom);
        }

        protected override float AsFloat(float value)
        {
            return value;
        }

        public Vector4 ToVector4()
        {
            return base.ToVector4(Vector4Order.LeftBottomRightTop);
        }
    }
}
