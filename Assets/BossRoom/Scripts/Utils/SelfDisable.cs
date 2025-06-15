using UnityEngine;

namespace Unity.BossRoom.Utils
{
    /// <summary>
    /// Will Disable this game object once active after the delay duration has passed.
    /// </summary>
    public class SelfDisable : MonoBehaviour
    {
        [SerializeField]
        float m_DisabledDelay;
        float _mDisableTimestamp;

        void Update()
        {
            if (Time.time >= _mDisableTimestamp)
            {
                gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            _mDisableTimestamp = Time.time + m_DisabledDelay;
        }
    }
}
