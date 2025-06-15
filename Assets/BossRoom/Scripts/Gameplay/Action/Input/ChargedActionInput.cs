using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public class ChargedActionInput : BaseActionInput
    {
        protected float MStartTime;

        private void Start()
        {
            // get our particle near the right spot!
            transform.position = MOrigin;

            MStartTime = Time.time;
            // right now we only support "untargeted" charged attacks.
            // Will need more input (e.g. click position) for fancier types of charged attacks!
            var data = new ActionRequestData
            {
                Position = transform.position,
                ActionID = MActionPrototypeID,
                ShouldQueue = false,
                TargetIds = null
            };
            MSendInput(data);
        }

        public override void OnReleaseKey()
        {
            MPlayerOwner.ServerStopChargingUpRpc();
            Destroy(gameObject);
        }

    }
}
