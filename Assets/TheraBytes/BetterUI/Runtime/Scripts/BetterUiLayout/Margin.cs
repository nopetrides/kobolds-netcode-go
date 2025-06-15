using System;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public enum Vector4Order
    {
        LeftTopRightBottom,
        LeftBottomRightTop,
    }

    /// <summary>
    /// This class is used for margins and paddings that uses integers rather than floats. 
    /// The only exception is TextMeshPro Margins (here they should be floats). 
    /// This is a design flaw but kept for backwards compatibility reasons (it doesn't hurt too much for text margins)
    /// For other paddings / margins with floats, the Padding class is used.
    /// </summary>
    [Serializable]
    public class Margin : MarginOrPadding<int>
    {
        public float Horizontal { get { return left + right; } }
        public float Vertical { get { return top + bottom; } }

        public Margin()
            : base(0, 0, 0, 0)
        { }

        public Margin(int left, int right, int top, int bottom)
            : base(left, right, top, bottom)
        { }

        public Margin(RectOffset source)
            : base(source.left, source.right, source.top, source.bottom)
        {
        }

        public Margin Clone()
        {
            return new Margin(this.left, this.right, this.top, this.bottom);
        }

        public Margin(Vector4 source, Vector4Order order = Vector4Order.LeftTopRightBottom)
            : base(source, order)
        { }

        public void CopyValuesTo(RectOffset target)
        {
            target.left = this.left;
            target.right = this.right;
            target.top = this.top;
            target.bottom = this.bottom;
        }


        protected override void SetFloats(float left, float right, float top, float bottom)
        {
            base.Set((int)left, (int)right, (int)top, (int)bottom);
        }

        protected override float AsFloat(int value)
        {
            return value;
        }

        public Vector4 ToVector4()
        {
            return base.ToVector4(Vector4Order.LeftTopRightBottom);
        }
    }

    [Serializable]
    public abstract class MarginOrPadding<T>
    {
        public T Left { get { return left; } set { left = value; } }
        public T Right { get { return right; } set { right = value; } }
        public T Top { get { return top; } set { top = value; } }
        public T Bottom { get { return bottom; } set { bottom = value; } }

        [SerializeField]
        protected T left, right, top, bottom;

        public T this[int idx]
        {
            get
            {
                switch (idx)
                {
                    case 0:
                        return left;
                    case 1:
                        return right;
                    case 2:
                        return top;
                    default:
                        return bottom;
                }
            }
            set
            {
                switch (idx)
                {
                    case 0:
                        left = value;
                        break;
                    case 1:
                        right = value;
                        break;
                    case 2:
                        top = value;
                        break;
                    default:
                        bottom = value;
                        break;
                }
            }
        }

        protected MarginOrPadding(T left, T right, T top, T bottom)
        {
            Set(left, right, top, bottom);
        }

        protected MarginOrPadding(Vector4 source, Vector4Order order)
        {
            switch (order)
            {
                case Vector4Order.LeftTopRightBottom:
                    SetFloats(left: source.x,
                        top: source.y,
                        right: source.z,
                        bottom: source.w);
                    break;

                case Vector4Order.LeftBottomRightTop:
                    SetFloats(left: source.x,
                        bottom: source.y,
                        right: source.z,
                        top: source.w);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }


        protected void Set(T left, T right, T top, T bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public Vector4 ToVector4(Vector4Order order)
        {
            float l = AsFloat(left);
            float r = AsFloat(right);
            float t = AsFloat(top);
            float b = AsFloat(bottom);

            switch (order)
            {
                case Vector4Order.LeftTopRightBottom:
                    return new Vector4(l, t, r, b);

                case Vector4Order.LeftBottomRightTop:
                    return new Vector4(l, b, r, t);

                default:
                    throw new NotSupportedException();
            }
        }

        protected abstract float AsFloat(T value);
        protected abstract void SetFloats(float left, float right, float top, float bottom);
        public override string ToString()
        {
            return string.Format("(left: {0}, right: {1}, top: {2}, bottom: {3})",
                left, right, top, bottom);
        }
    }
}
