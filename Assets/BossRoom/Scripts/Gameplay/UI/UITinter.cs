using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    [RequireComponent(typeof(Image))]
    public class UITinter : MonoBehaviour
    {
        [SerializeField]
        Color[] m_TintColors;
        Image _mImage;
        void Awake()
        {
            _mImage = GetComponent<Image>();
        }

        public void SetToColor(int colorIndex)
        {
            if (colorIndex >= m_TintColors.Length)
                return;
            _mImage.color = m_TintColors[colorIndex];
        }
    }
}
