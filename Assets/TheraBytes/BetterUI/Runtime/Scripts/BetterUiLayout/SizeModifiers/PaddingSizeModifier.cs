using System;
using System.Collections.Generic;

namespace TheraBytes.BetterUi
{
    [Serializable]
    public class PaddingSizeConfigCollection : SizeConfigCollection<PaddingSizeModifier> { }

    /// <summary>
    /// This is for Vector4 objects. 
    /// For an easier mapping for left / right / top / bottom, this wrapper class is used.
    /// </summary>
    [Serializable]
    public class PaddingSizeModifier : ScreenDependentSize<Padding>
    {
        public SizeModifierCollection ModLeft;
        public SizeModifierCollection ModRight;
        public SizeModifierCollection ModTop;
        public SizeModifierCollection ModBottom;

        public PaddingSizeModifier(Padding optimizedSize, Padding minSize, Padding maxSize)
            : base(optimizedSize, minSize, maxSize, optimizedSize.Clone())
        {
            ModLeft = new SizeModifierCollection(new SizeModifierCollection.SizeModifier(ImpactMode.PixelWidth, 1));
            ModRight = new SizeModifierCollection(new SizeModifierCollection.SizeModifier(ImpactMode.PixelWidth, 1));
            ModTop = new SizeModifierCollection(new SizeModifierCollection.SizeModifier(ImpactMode.PixelWidth, 1));
            ModBottom = new SizeModifierCollection(new SizeModifierCollection.SizeModifier(ImpactMode.PixelWidth, 1));
        }

        public override void DynamicInitialization()
        {
            if (this.value == null)
                this.value = new Padding();
        }

        public override IEnumerable<SizeModifierCollection> GetModifiers()
        {
            yield return ModLeft;
            yield return ModRight;
            yield return ModTop;
            yield return ModBottom;
        }

        protected override void AdjustSize(float factor, SizeModifierCollection mod, int index)
        {
            if (this.value == null)
                this.value = new Padding();

            value[index] = GetSize(factor, OptimizedSize[index], MinSize[index], MaxSize[index]);
        }

        protected override void CalculateOptimizedSize(Padding baseValue, float factor, SizeModifierCollection mod, int index)
        {
            OptimizedSize[index] = factor * baseValue[index];
        }
    }
}
