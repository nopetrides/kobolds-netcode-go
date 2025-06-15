using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    [Serializable]
    public class Vector2IntSizeConfigCollection : SizeConfigCollection<Vector2IntSizeModifier> { }

    [Serializable]
    public class Vector2IntSizeModifier : ScreenDependentSize<Vector2Int>
    {
        public SizeModifierCollection ModX;
        public SizeModifierCollection ModY;


        public Vector2IntSizeModifier(Vector2Int optimizedSize, Vector2Int minSize, Vector2Int maxSize)
            : base(optimizedSize, minSize, maxSize, optimizedSize)
        {
            ModX = new SizeModifierCollection(new SizeModifierCollection.SizeModifier(ImpactMode.PixelHeight, 1));
            ModY = new SizeModifierCollection(new SizeModifierCollection.SizeModifier(ImpactMode.PixelHeight, 1));
        }

        public override IEnumerable<SizeModifierCollection> GetModifiers()
        {
            yield return ModX;
            yield return ModY;
        }

        protected override void AdjustSize(float factor, SizeModifierCollection mod, int index)
        {
            value[index] = Mathf.RoundToInt(GetSize(factor, OptimizedSize[index], MinSize[index], MaxSize[index]));
        }

        protected override void CalculateOptimizedSize(Vector2Int baseValue, float factor, SizeModifierCollection mod, int index)
        {
            OptimizedSize[index] = Mathf.RoundToInt(factor * baseValue[index]);
        }
    }
}
