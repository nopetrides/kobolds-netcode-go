using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class ChargedLaunchProjectileAction
    {
        /// <summary>
        /// A list of the special particle graphics we spawned.
        /// </summary>
        /// <remarks>
        /// Performance note: repeatedly creating and destroying GameObjects is not optimal, and on low-resource platforms
        /// (like mobile devices), it can lead to major performance problems. On mobile platforms, visual graphics should
        /// use object-pooling (i.e. reusing the same GameObjects repeatedly). But that's outside the scope of this demo.
        /// </remarks>
        private List<SpecialFXGraphic> _mGraphics = new List<SpecialFXGraphic>();

        private bool _mChargeEnded;

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            base.OnStartClient(clientCharacter);

            _mGraphics = InstantiateSpecialFXGraphics(clientCharacter.transform, true);
            return true;
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return !_mChargeEnded;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            if (!_mChargeEnded)
            {
                foreach (var graphic in _mGraphics)
                {
                    if (graphic)
                    {
                        graphic.Shutdown();
                    }
                }
            }
        }

        public override void OnStoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage)
        {
            _mChargeEnded = true;
            foreach (var graphic in _mGraphics)
            {
                if (graphic)
                {
                    graphic.Shutdown();
                }
            }

            // the graphics will now take care of themselves and shutdown, so we can forget about 'em
            _mGraphics.Clear();
        }
    }
}
