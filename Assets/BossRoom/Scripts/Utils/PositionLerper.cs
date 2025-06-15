using UnityEngine;

namespace Unity.BossRoom.Utils
{
    /// <summary>
    /// Utility struct to linearly interpolate between two Vector3 values. Allows for flexible linear interpolations
    /// where current and target change over time.
    /// </summary>
    public struct PositionLerper
    {
        // Calculated start for the most recent interpolation
        Vector3 _mLerpStart;

        // Calculated time elapsed for the most recent interpolation
        float _mCurrentLerpTime;

        // The duration of the interpolation, in seconds
        float _mLerpTime;

        public PositionLerper(Vector3 start, float lerpTime)
        {
            _mLerpStart = start;
            _mCurrentLerpTime = 0f;
            _mLerpTime = lerpTime;
        }

        /// <summary>
        /// Linearly interpolate between two Vector3 values.
        /// </summary>
        /// <param name="current"> Start of the interpolation. </param>
        /// <param name="target"> End of the interpolation. </param>
        /// <returns> A Vector3 value between current and target. </returns>
        public Vector3 LerpPosition(Vector3 current, Vector3 target)
        {
            if (current != target)
            {
                _mLerpStart = current;
                _mCurrentLerpTime = 0f;
            }

            _mCurrentLerpTime += Time.deltaTime;
            if (_mCurrentLerpTime > _mLerpTime)
            {
                _mCurrentLerpTime = _mLerpTime;
            }

            var lerpPercentage = _mCurrentLerpTime / _mLerpTime;

            return Vector3.Lerp(_mLerpStart, target, lerpPercentage);
        }
    }
}
