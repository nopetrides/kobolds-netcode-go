using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// UI object that visually represents an object's health. Visuals are updated when NetworkVariable is modified.
    /// </summary>
    public class UIHealth : MonoBehaviour
    {
        [SerializeField]
        Slider m_HitPointsSlider;

        NetworkVariable<int> _mNetworkedHealth;

        public void Initialize(NetworkVariable<int> networkedHealth, int maxValue)
        {
            _mNetworkedHealth = networkedHealth;

            m_HitPointsSlider.minValue = 0;
            m_HitPointsSlider.maxValue = maxValue;
            HealthChanged(maxValue, maxValue);

            _mNetworkedHealth.OnValueChanged += HealthChanged;
        }

        void HealthChanged(int previousValue, int newValue)
        {
            m_HitPointsSlider.value = newValue;
            // disable slider when we're at full health!
            m_HitPointsSlider.gameObject.SetActive(m_HitPointsSlider.value != m_HitPointsSlider.maxValue);
        }

        void OnDestroy()
        {
            _mNetworkedHealth.OnValueChanged -= HealthChanged;
        }
    }
}
