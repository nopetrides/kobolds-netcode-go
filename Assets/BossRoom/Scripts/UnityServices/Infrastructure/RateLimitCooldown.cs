using System;
using UnityEngine;

namespace Unity.BossRoom.UnityServices
{
    public class RateLimitCooldown
    {
        public float CooldownTimeLength => _mCooldownTimeLength;

        readonly float _mCooldownTimeLength;
        private float _mCooldownFinishedTime;

        public RateLimitCooldown(float cooldownTimeLength)
        {
            _mCooldownTimeLength = cooldownTimeLength;
            _mCooldownFinishedTime = -1f;
        }

        public bool CanCall => Time.unscaledTime > _mCooldownFinishedTime;

        public void PutOnCooldown()
        {
            _mCooldownFinishedTime = Time.unscaledTime + _mCooldownTimeLength;
        }
    }
}
