using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class ChargedShieldAction
    {
        /// <summary>
        /// The "charging up" graphics. These are disabled as soon as the player stops charging up
        /// </summary>
        SpecialFXGraphic _mChargeGraphics;

        /// <summary>
        /// The "I'm fully charged" graphics. This is null until instantiated
        /// </summary>
        SpecialFXGraphic _mShieldGraphics;

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return IsChargingUp() || (Time.time - _mStoppedChargingUpTime) < Config.EffectDurationSeconds;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            if (IsChargingUp())
            {
                // we never actually stopped "charging up" so do necessary clean up here
                if (_mChargeGraphics)
                {
                    _mChargeGraphics.Shutdown();
                }
            }

            if (_mShieldGraphics)
            {
                _mShieldGraphics.Shutdown();
            }
        }

        public override void OnStoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage)
        {
            if (!IsChargingUp()) { return; }

            _mStoppedChargingUpTime = Time.time;
            if (_mChargeGraphics)
            {
                _mChargeGraphics.Shutdown();
                _mChargeGraphics = null;
            }

            // if fully charged, we show a special graphic
            if (Mathf.Approximately(finalChargeUpPercentage, 1))
            {
                _mShieldGraphics = InstantiateSpecialFXGraphic(Config.Spawns[1], clientCharacter.transform, true);
            }
        }

        public override void AnticipateActionClient(ClientCharacter clientCharacter)
        {
            // because this action can be visually started and stopped as often and as quickly as the player wants, it's possible
            // for several copies of this action to be playing at once. This can lead to situations where several
            // dying versions of the action raise the end-trigger, but the animator only lowers it once, leaving the trigger
            // in a raised state. So we'll make sure that our end-trigger isn't raised yet. (Generally a good idea anyway.)
            clientCharacter.OurAnimator.ResetTrigger(Config.Anim2);
            base.AnticipateActionClient(clientCharacter);
        }
    }
}
