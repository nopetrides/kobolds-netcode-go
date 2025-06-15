using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public abstract class BaseActionInput : MonoBehaviour
    {
        protected ServerCharacter MPlayerOwner;
        protected Vector3 MOrigin;
        protected ActionID MActionPrototypeID;
        protected Action<ActionRequestData> MSendInput;
        System.Action _mOnFinished;

        public void Initiate(ServerCharacter playerOwner, Vector3 origin, ActionID actionPrototypeID, Action<ActionRequestData> onSendInput, System.Action onFinished)
        {
            MPlayerOwner = playerOwner;
            MOrigin = origin;
            MActionPrototypeID = actionPrototypeID;
            MSendInput = onSendInput;
            _mOnFinished = onFinished;
        }

        public void OnDestroy()
        {
            _mOnFinished();
        }

        public virtual void OnReleaseKey() { }
    }
}
