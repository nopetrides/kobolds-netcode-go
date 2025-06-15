using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Logic that handles an FX-based pretend-missile.
    /// </summary>
    public class FXProjectile : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> m_ProjectileGraphics;

        [SerializeField]
        private List<GameObject> m_TargetHitGraphics;

        [SerializeField]
        private List<GameObject> m_TargetMissedGraphics;

        [SerializeField]
        [Tooltip("If this projectile plays an impact particle, how long should we stay alive for it to keep playing?")]
        private float m_PostImpactDurationSeconds = 1;

        private Vector3 _mStartPoint;
        private Transform _mTargetDestination; // null if we're a "miss" projectile (i.e. we hit nothing)
        private Vector3 _mMissDestination; // only used if m_TargetDestination is null
        private float _mFlightDuration;
        private float _mAge;
        private bool _mHasImpacted;

        public void Initialize(Vector3 startPoint, Transform target, Vector3 missPos, float flightTime)
        {
            _mStartPoint = startPoint;
            _mTargetDestination = target;
            _mMissDestination = missPos;
            _mFlightDuration = flightTime;
            _mHasImpacted = false;

            // the projectile graphics are actually already enabled in the prefab, but just in case, turn them on
            foreach (var projectileGo in m_ProjectileGraphics)
            {
                projectileGo.SetActive(true);
            }
        }

        public void Cancel()
        {
            // we could play a "poof" particle... but for now we just instantly disappear
            Destroy(gameObject);
        }

        private void Update()
        {
            _mAge += Time.deltaTime;
            if (!_mHasImpacted)
            {
                if (_mAge >= _mFlightDuration)
                {
                    Impact();
                }
                else
                {
                    // we're flying through the air. Reposition ourselves to be closer to the destination
                    float progress = _mAge / _mFlightDuration;
                    transform.position = Vector3.Lerp(_mStartPoint, _mTargetDestination ? _mTargetDestination.position : _mMissDestination, progress);
                }
            }
            else if (_mAge >= _mFlightDuration + m_PostImpactDurationSeconds)
            {
                Destroy(gameObject);
            }
        }


        private void Impact()
        {
            _mHasImpacted = true;

            foreach (var projectileGo in m_ProjectileGraphics)
            {
                projectileGo.SetActive(false);
            }

            // is it impacting an actual enemy? We allow different graphics for the "miss" case
            if (_mTargetDestination)
            {
                foreach (var hitGraphicGo in m_TargetHitGraphics)
                {
                    hitGraphicGo.SetActive(true);
                }
            }
            else
            {
                foreach (var missGraphicGo in m_TargetMissedGraphics)
                {
                    missGraphicGo.SetActive(true);
                }
            }
        }
    }
}
