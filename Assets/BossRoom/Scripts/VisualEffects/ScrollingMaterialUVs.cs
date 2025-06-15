using UnityEngine;

namespace Unity.BossRoom.VisualEffects
{
    public class ScrollingMaterialUVs : MonoBehaviour
    {
        public float ScrollX = .01f;
        public float ScrollY = .01f;

        [SerializeField]
        Material m_Material;

        float _mOffsetX;
        float _mOffsetY;

        void Update()
        {
            _mOffsetX = Time.time * ScrollX;
            _mOffsetY = Time.time * ScrollY;
            m_Material.mainTextureOffset = new Vector2(_mOffsetX, _mOffsetY);
        }

        void OnDestroy()
        {
            ResetMaterialOffset();
        }

        void OnApplicationQuit()
        {
            ResetMaterialOffset();
        }

        void ResetMaterialOffset()
        {
            // reset UVs to avoid modifying the material file; this will be refactored
            m_Material.mainTextureOffset = new Vector2(0f, 0f);
        }
    }
}
