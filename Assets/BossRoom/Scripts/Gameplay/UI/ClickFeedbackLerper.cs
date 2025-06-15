using System;
using Unity.BossRoom.Utils;
using UnityEngine;


namespace Unity.BossRoom.Gameplay.UI
{
    public class ClickFeedbackLerper : MonoBehaviour
    {
        PositionLerper _mPositionLerper;

        Vector3 _mTargetPosition;

        // The amount of offset to keep the click feedback object from intersecting with the floor
        const float KHoverHeight = 0.15f;
        const float KLerpTime = 0.04f;

        void Start()
        {
            _mPositionLerper = new PositionLerper(Vector3.zero, KLerpTime);
        }

        void Update()
        {
            transform.position = _mPositionLerper.LerpPosition(transform.position, _mTargetPosition);
        }

        public void SetTarget(Vector3 clientInputPosition)
        {
            _mTargetPosition.x = clientInputPosition.x;
            _mTargetPosition.y = KHoverHeight;
            _mTargetPosition.z = clientInputPosition.z;
        }
    }
}
