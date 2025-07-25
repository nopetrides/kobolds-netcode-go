using UnityEngine;

namespace TheraBytes.BetterUi
{
    public interface IImageAppearanceProvider
    {
        ColorMode ColoringMode { get; }
        Color SecondColor { get; set; }
        Color color { get; set; }


        string MaterialType { get; set; }
        MaterialEffect MaterialEffect { get; set; }
        VertexMaterialData MaterialProperties { get; }

        Material material { get; }

        void SetMaterialProperty(int propertyIndex, float value);
        float GetMaterialPropertyValue(int propertyIndex);
        void SetMaterialDirty();
    }
}
