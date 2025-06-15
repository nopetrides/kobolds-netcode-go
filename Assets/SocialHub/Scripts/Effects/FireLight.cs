using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    class FireLight : MonoBehaviour
    {
        [SerializeField]
        AnimationCurve m_LightCurve;

        [SerializeField]
        float m_FireSpeed = 1f;

        Light _mLight;
        float _mInitialIntensity;

        void Awake()
        {
            _mLight = GetComponent<Light>();
            _mInitialIntensity = _mLight.intensity;
        }

        void Update()
        {
            _mLight.intensity = _mInitialIntensity * m_LightCurve.Evaluate(Time.time * m_FireSpeed);
        }
    }
}
