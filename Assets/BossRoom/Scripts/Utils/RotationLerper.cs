using System;
using UnityEngine;

namespace Unity.BossRoom.Utils
{
    /// <summary>
    /// Utility struct to linearly interpolate between two Quaternion values. Allows for flexible linear interpolations
    /// where current and target change over time.
    /// </summary>
    public struct RotationLerper
    {
        // Calculated start for the most recent interpolation
        Quaternion _mLerpStart;

        // Calculated time elapsed for the most recent interpolation
        float _mCurrentLerpTime;

        // The duration of the interpolation, in seconds
        float _mLerpTime;

        public RotationLerper(Quaternion start, float lerpTime)
        {
            _mLerpStart = start;
            _mCurrentLerpTime = 0f;
            _mLerpTime = lerpTime;
        }

        /// <summary>
        /// Linearly interpolate between two Quaternion values.
        /// </summary>
        /// <param name="current"> Start of the interpolation. </param>
        /// <param name="target"> End of the interpolation. </param>
        /// <returns> A Quaternion value between current and target. </returns>
        public Quaternion LerpRotation(Quaternion current, Quaternion target)
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

            return Quaternion.Slerp(_mLerpStart, target, lerpPercentage);
        }
    }
}
