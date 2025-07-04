using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    //
    // GENERIC
    //
    [Serializable]
    public abstract class ScreenDependentSize<T> : ScreenDependentSize, IScreenConfigConnection
    {
        [SerializeField]
        string screenConfigName;

        public override string ScreenConfigName
        {
            get { return screenConfigName; }
            set { screenConfigName = value; }
        }

        public T OptimizedSize;
        public T MinSize;
        public T MaxSize;

        public bool UseMinSize;
        public bool UseMaxSize;

        protected T value;

        public T LastCalculatedSize { get { return value; } }

        protected ScreenDependentSize(T opt, T min, T max, T initValue)
        {
            this.OptimizedSize = opt;
            this.MinSize = min;
            this.MaxSize = max;
            this.value = initValue;

            this.UseMinSize = false;
            this.UseMaxSize = false;
        }

        public T CalculateSize(Component caller, string callingPropertyName)
        {
            if (GlobalApplier.Instance.TryGetValueOverride(caller, callingPropertyName, screenConfigName, out T result))
            {
                value = result;
                return value;
            }

            base.UpdateSize(caller);
            return value;
        }

        protected float GetSize(float factor, float opt, float min, float max)
        {
            var result = factor * opt;

            if (UseMinSize && result < min)
                return min;

            if (UseMaxSize && result > max)
                return max;

            return result;
        }

        /// <summary>
        /// This method can be called during runtime to apply a calculated size.
        /// This will change the optimized size to be able to still work resolution independently.
        /// </summary>
        /// <param name="caller">The component which contains this sizer.</param>
        /// <param name="size">The size to apply.</param>
        public void SetSize(Component caller, T size)
        {
            int i = 0;
            foreach (var mod in GetModifiers())
            {
                float invFac = 1 / mod.CalculateFactor(caller, screenConfigName);
                CalculateOptimizedSize(size, invFac, mod, i);
                i++;
            }

            value = size; // TODO: clamp
        }

        /// <summary>
        /// This method just sets the last calculated size to track additional calculations from outside.
        /// The Optimized Size will not be affected.
        /// </summary>
        /// <param name="newValue"></param>
        public void OverrideLastCalculatedSize(T newValue)
        {
            this.value = newValue;
        }

        protected abstract void CalculateOptimizedSize(T baseValue, float factor, SizeModifierCollection mod, int index);
    }

    //
    // NON GENERIC
    //
    [Serializable]
    public abstract class ScreenDependentSize
    {
        public abstract string ScreenConfigName { get; set; }

        protected void UpdateSize(Component caller)
        {
            int i = 0;
            foreach (var mod in GetModifiers())
            {
                float factor = mod.CalculateFactor(caller, ScreenConfigName);
                AdjustSize(factor, mod, i);
                i++;
            }
        }

        public virtual void DynamicInitialization()
        {
            // most do not need initialization.
        }

        public abstract IEnumerable<SizeModifierCollection> GetModifiers();
        protected abstract void AdjustSize(float factor, SizeModifierCollection mod, int index);
    }
}
