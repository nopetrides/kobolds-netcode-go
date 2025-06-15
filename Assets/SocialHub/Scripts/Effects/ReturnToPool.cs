using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    [RequireComponent(typeof(ParticleSystem))]
    class ReturnToPool : BaseFxObject
    {
        ParticleSystem _mParticleSystem;

        void Awake()
        {
            _mParticleSystem = GetComponent<ParticleSystem>();
            var systemMain = _mParticleSystem.main;
            systemMain.stopAction = ParticleSystemStopAction.Callback;
        }

        void OnEnable()
        {
            _mParticleSystem.Play();
        }

        void OnParticleSystemStopped()
        {
            StopFx();
        }
    }
}
