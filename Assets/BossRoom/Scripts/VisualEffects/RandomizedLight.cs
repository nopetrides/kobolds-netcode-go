using UnityEngine;

namespace Unity.BossRoom.VisualEffects
{
    /// <summary>
    /// This script randomly varies a light source to create a flickering effect.
    /// </summary>
    public class RandomizedLight : MonoBehaviour
    {
        private const int KIntensityScale = 100;

        [Tooltip("External light to vary. Leave null if this script is itself attached to a Light")]
        public Light m_TargetLight;

        [Tooltip("Minimum light intensity to randomize to")]
        public float m_MinIntensity = 0f;

        [Tooltip("Maximum light intensity to randomize to")]
        public float m_MaxIntensity = 1f;

        [Tooltip("How much smoothing to apply to the signal. Lower values will be less smoothed.")]
        [Range(1, 50)]
        public int m_Smoothing = 5;

        private int[] _mRingBuffer;   //a buffer full of noise ranging from min to max.
        private int _mRingSum;        //the sum of all the values in the current ring buffer.
        private int _mRingIndex;      //the current index of the buffer.

        // Start is called before the first frame update
        void Start()
        {
            _mRingBuffer = new int[m_Smoothing];
            for (int i = 0; i < _mRingBuffer.Length; ++i)
            {
                UpdateNoiseBuffer();
            }

            if (m_TargetLight == null)
            {
                m_TargetLight = GetComponent<Light>();
            }
        }

        private void UpdateNoiseBuffer()
        {
            int newValue = (int)(Random.Range(m_MinIntensity, m_MaxIntensity) * KIntensityScale);
            _mRingSum += (newValue - _mRingBuffer[_mRingIndex]);
            _mRingBuffer[_mRingIndex] = newValue;

            _mRingIndex = (_mRingIndex + 1) % _mRingBuffer.Length;
        }

        // Update is called once per frame
        void Update()
        {
            //should be a value between 0-1
            float lightIntensity = _mRingSum / (float)(_mRingBuffer.Length * KIntensityScale);
            m_TargetLight.intensity = lightIntensity;

            UpdateNoiseBuffer();
        }
    }
}
