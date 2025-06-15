using System;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class TargetAction
    {
        private GameObject _mTargetReticule;
        private ulong _mCurrentTarget;
        private ulong _mNewTarget;

        private const float KReticuleGroundHeight = 0.2f;

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            base.OnStartClient(clientCharacter);
            clientCharacter.ServerCharacter.TargetId.OnValueChanged += OnTargetChanged;
            clientCharacter.ServerCharacter.GetComponent<ClientInputSender>().ActionInputEvent += OnActionInput;

            return true;
        }

        private void OnTargetChanged(ulong oldTarget, ulong newTarget)
        {
            _mNewTarget = newTarget;
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (_mCurrentTarget != _mNewTarget)
            {
                _mCurrentTarget = _mNewTarget;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_mCurrentTarget, out NetworkObject targetObject))
                {
                    var targetEntity = targetObject != null ? targetObject.GetComponent<ITargetable>() : null;
                    if (targetEntity != null)
                    {
                        ValidateReticule(clientCharacter, targetObject);
                        _mTargetReticule.SetActive(true);

                        var parentTransform = targetObject.transform;
                        if (targetObject.TryGetComponent(out ServerCharacter serverCharacter) && serverCharacter.ClientCharacter)
                        {
                            //for characters, attach the reticule to the child graphics object.
                            parentTransform = serverCharacter.ClientCharacter.transform;
                        }

                        _mTargetReticule.transform.parent = parentTransform;
                        _mTargetReticule.transform.localPosition = new Vector3(0, KReticuleGroundHeight, 0);
                    }
                }
                else
                {
                    // null check here in case the target was destroyed along with the target reticule
                    if (_mTargetReticule != null)
                    {
                        _mTargetReticule.transform.parent = null;
                        _mTargetReticule.SetActive(false);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Ensures that the TargetReticule GameObject exists. This must be done prior to enabling it because it can be destroyed
        /// "accidentally" if its parent is destroyed while it is detached.
        /// </summary>
        void ValidateReticule(ClientCharacter parent, NetworkObject targetObject)
        {
            if (_mTargetReticule == null)
            {
                _mTargetReticule = Object.Instantiate(parent.TargetReticulePrefab);
            }

            bool targetIsnpc = targetObject.GetComponent<ITargetable>().IsNpc;
            bool myselfIsnpc = parent.ServerCharacter.CharacterClass.IsNpc;
            bool hostile = targetIsnpc != myselfIsnpc;

            _mTargetReticule.GetComponent<MeshRenderer>().material = hostile ? parent.ReticuleHostileMat : parent.ReticuleFriendlyMat;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            GameObject.Destroy(_mTargetReticule);

            clientCharacter.ServerCharacter.TargetId.OnValueChanged -= OnTargetChanged;
            if (clientCharacter.TryGetComponent(out ClientInputSender inputSender))
            {
                inputSender.ActionInputEvent -= OnActionInput;
            }
        }

        private void OnActionInput(ActionRequestData data)
        {
            //this method runs on the owning client, and allows us to anticipate our new target for purposes of FX visualization.
            if (GameDataSource.Instance.GetActionPrototypeByID(data.ActionID).IsGeneralTargetAction)
            {
                _mNewTarget = data.TargetIds[0];
            }
        }
    }
}
