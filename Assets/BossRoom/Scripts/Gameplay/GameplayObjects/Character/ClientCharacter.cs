using System;
using Unity.BossRoom.CameraUtils;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Utils;
using Unity.Netcode;
using UnityEngine;


namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// <see cref="ClientCharacter"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacter : NetworkBehaviour
    {
        [SerializeField]
        Animator m_ClientVisualsAnimator;

        [SerializeField]
        VisualizationConfiguration m_VisualizationConfiguration;

        /// <summary>
        /// Returns a reference to the active Animator for this visualization
        /// </summary>
        public Animator OurAnimator => m_ClientVisualsAnimator;

        /// <summary>
        /// Returns the targeting-reticule prefab for this character visualization
        /// </summary>
        public GameObject TargetReticulePrefab => m_VisualizationConfiguration.TargetReticule;

        /// <summary>
        /// Returns the Material to plug into the reticule when the selected entity is hostile
        /// </summary>
        public Material ReticuleHostileMat => m_VisualizationConfiguration.ReticuleHostileMat;

        /// <summary>
        /// Returns the Material to plug into the reticule when the selected entity is friendly
        /// </summary>
        public Material ReticuleFriendlyMat => m_VisualizationConfiguration.ReticuleFriendlyMat;

        CharacterSwap _mCharacterSwapper;

        public CharacterSwap CharacterSwap => _mCharacterSwapper;

        public bool CanPerformActions => _mServerCharacter.CanPerformActions;

        ServerCharacter _mServerCharacter;

        public ServerCharacter ServerCharacter => _mServerCharacter;

        ClientActionPlayer _mClientActionViz;

        PositionLerper _mPositionLerper;

        RotationLerper _mRotationLerper;

        // this value suffices for both positional and rotational interpolations; one may have a constant value for each
        const float KLerpTime = 0.08f;

        Vector3 _mLerpedPosition;

        Quaternion _mLerpedRotation;

        float _mCurrentSpeed;

        /// <summary>
        /// /// Server to Client RPC that broadcasts this action play to all clients.
        /// </summary>
        /// <param name="data"> Data about which action to play and its associated details. </param>
        [Rpc(SendTo.ClientsAndHost)]
        public void ClientPlayActionRpc(ActionRequestData data)
        {
            ActionRequestData data1 = data;
            _mClientActionViz.PlayAction(ref data1);
        }

        /// <summary>
        /// This RPC is invoked on the client when the active action FXs need to be cancelled (e.g. when the character has been stunned)
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void ClientCancelAllActionsRpc()
        {
            _mClientActionViz.CancelAllActions();
        }

        /// <summary>
        /// This RPC is invoked on the client when active action FXs of a certain type need to be cancelled (e.g. when the Stealth action ends)
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void ClientCancelActionsByPrototypeIDRpc(ActionID actionPrototypeID)
        {
            _mClientActionViz.CancelAllActionsWithSamePrototypeID(actionPrototypeID);
        }

        /// <summary>
        /// Called on all clients when this character has stopped "charging up" an attack.
        /// Provides a value between 0 and 1 inclusive which indicates how "charged up" the attack ended up being.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void ClientStopChargingUpRpc(float percentCharged)
        {
            _mClientActionViz.OnStoppedChargingUp(percentCharged);
        }

        void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || transform.parent == null)
            {
                return;
            }

            enabled = true;

            _mClientActionViz = new ClientActionPlayer(this);

            _mServerCharacter = GetComponentInParent<ServerCharacter>();

            _mServerCharacter.IsStealthy.OnValueChanged += OnStealthyChanged;
            _mServerCharacter.MovementStatus.OnValueChanged += OnMovementStatusChanged;
            OnMovementStatusChanged(MovementStatus.Normal, _mServerCharacter.MovementStatus.Value);

            // sync our visualization position & rotation to the most up to date version received from server
            transform.SetPositionAndRotation(ServerCharacter.PhysicsWrapper.Transform.position,
                ServerCharacter.PhysicsWrapper.Transform.rotation);
            _mLerpedPosition = transform.position;
            _mLerpedRotation = transform.rotation;

            // similarly, initialize start position and rotation for smooth lerping purposes
            _mPositionLerper = new PositionLerper(ServerCharacter.PhysicsWrapper.Transform.position, KLerpTime);
            _mRotationLerper = new RotationLerper(ServerCharacter.PhysicsWrapper.Transform.rotation, KLerpTime);

            if (!_mServerCharacter.IsNpc)
            {
                name = "AvatarGraphics" + _mServerCharacter.OwnerClientId;

                if (_mServerCharacter.TryGetComponent(out ClientAvatarGuidHandler clientAvatarGuidHandler))
                {
                    m_ClientVisualsAnimator = clientAvatarGuidHandler.GraphicsAnimator;
                }

                _mCharacterSwapper = GetComponentInChildren<CharacterSwap>();

                // ...and visualize the current char-select value that we know about
                SetAppearanceSwap();

                if (_mServerCharacter.IsOwner)
                {
                    ActionRequestData data = new ActionRequestData { ActionID = GameDataSource.Instance.GeneralTargetActionPrototype.ActionID };
                    _mClientActionViz.PlayAction(ref data);
                    gameObject.AddComponent<CameraController>();

                    if (_mServerCharacter.TryGetComponent(out ClientInputSender inputSender))
                    {
                        // anticipated actions will only be played on non-host, owning clients
                        if (!IsServer)
                        {
                            inputSender.ActionInputEvent += OnActionInput;
                        }
                        inputSender.ClientMoveEvent += OnMoveInput;
                    }
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_mServerCharacter)
            {
                _mServerCharacter.IsStealthy.OnValueChanged -= OnStealthyChanged;

                if (_mServerCharacter.TryGetComponent(out ClientInputSender sender))
                {
                    sender.ActionInputEvent -= OnActionInput;
                    sender.ClientMoveEvent -= OnMoveInput;
                }
            }

            enabled = false;
        }

        void OnActionInput(ActionRequestData data)
        {
            _mClientActionViz.AnticipateAction(ref data);
        }

        void OnMoveInput(Vector3 position)
        {
            if (!IsAnimating())
            {
                OurAnimator.SetTrigger(m_VisualizationConfiguration.AnticipateMoveTriggerID);
            }
        }

        void OnStealthyChanged(bool oldValue, bool newValue)
        {
            SetAppearanceSwap();
        }

        void SetAppearanceSwap()
        {
            if (_mCharacterSwapper)
            {
                var specialMaterialMode = CharacterSwap.SpecialMaterialMode.None;
                if (_mServerCharacter.IsStealthy.Value)
                {
                    if (_mServerCharacter.IsOwner)
                    {
                        specialMaterialMode = CharacterSwap.SpecialMaterialMode.StealthySelf;
                    }
                    else
                    {
                        specialMaterialMode = CharacterSwap.SpecialMaterialMode.StealthyOther;
                    }
                }

                _mCharacterSwapper.SwapToModel(specialMaterialMode);
            }
        }

        /// <summary>
        /// Returns the value we should set the Animator's "Speed" variable, given current gameplay conditions.
        /// </summary>
        float GetVisualMovementSpeed(MovementStatus movementStatus)
        {
            if (_mServerCharacter.NetLifeState.LifeState.Value != LifeState.Alive)
            {
                return m_VisualizationConfiguration.SpeedDead;
            }

            switch (movementStatus)
            {
                case MovementStatus.Idle:
                    return m_VisualizationConfiguration.SpeedIdle;
                case MovementStatus.Normal:
                    return m_VisualizationConfiguration.SpeedNormal;
                case MovementStatus.Uncontrolled:
                    return m_VisualizationConfiguration.SpeedUncontrolled;
                case MovementStatus.Slowed:
                    return m_VisualizationConfiguration.SpeedSlowed;
                case MovementStatus.Hasted:
                    return m_VisualizationConfiguration.SpeedHasted;
                case MovementStatus.Walking:
                    return m_VisualizationConfiguration.SpeedWalking;
                default:
                    throw new Exception($"Unknown MovementStatus {movementStatus}");
            }
        }

        void OnMovementStatusChanged(MovementStatus previousValue, MovementStatus newValue)
        {
            _mCurrentSpeed = GetVisualMovementSpeed(newValue);
        }

        void Update()
        {
            // On the host, Characters are translated via ServerCharacterMovement's FixedUpdate method. To ensure that
            // the game camera tracks a GameObject moving in the Update loop and therefore eliminate any camera jitter,
            // this graphics GameObject's position is smoothed over time on the host. Clients do not need to perform any
            // positional smoothing since NetworkTransform will interpolate position updates on the root GameObject.
            if (IsHost)
            {
                // Note: a cached position (m_LerpedPosition) and rotation (m_LerpedRotation) are created and used as
                // the starting point for each interpolation since the root's position and rotation are modified in
                // FixedUpdate, thus altering this transform (being a child) in the process.
                _mLerpedPosition = _mPositionLerper.LerpPosition(_mLerpedPosition,
                    ServerCharacter.PhysicsWrapper.Transform.position);
                _mLerpedRotation = _mRotationLerper.LerpRotation(_mLerpedRotation,
                    ServerCharacter.PhysicsWrapper.Transform.rotation);
                transform.SetPositionAndRotation(_mLerpedPosition, _mLerpedRotation);
            }

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                OurAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, _mCurrentSpeed);
            }

            _mClientActionViz.OnUpdate();
        }

        void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured.

            _mClientActionViz.OnAnimEvent(id);
        }

        public bool IsAnimating()
        {
            if (OurAnimator.GetFloat(m_VisualizationConfiguration.SpeedVariableID) > 0.0) { return true; }

            for (int i = 0; i < OurAnimator.layerCount; i++)
            {
                if (OurAnimator.GetCurrentAnimatorStateInfo(i).tagHash != m_VisualizationConfiguration.BaseNodeTagID)
                {
                    //we are in an active node, not the default "nothing" node.
                    return true;
                }
            }

            return false;
        }
    }
}
