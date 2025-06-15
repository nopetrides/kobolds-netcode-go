using System;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace Unity.BossRoom.Gameplay.UserInput
{
    /// <summary>
    /// Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    [RequireComponent(typeof(ServerCharacter))]
    public class ClientInputSender : NetworkBehaviour
    {
        const float KMouseInputRaycastDistance = 100f;

        //The movement input rate is capped at 40ms (or 25 fps). This provides a nice balance between responsiveness and
        //upstream network conservation. This matters when holding down your mouse button to move.
        const float KMoveSendRateSeconds = 0.04f; //25 fps.

        const float KTargetMoveTimeout = 0.45f;  //prevent moves for this long after targeting someone (helps prevent walking to the guy you clicked).

        float _mLastSentMove;

        // Cache raycast hit array so that we can use non alloc raycasts
        readonly RaycastHit[] _kCachedHit = new RaycastHit[4];

        // This is basically a constant but layer masks cannot be created in the constructor, that's why it's assigned int Awake.
        LayerMask _mGroundLayerMask;

        LayerMask _mActionLayerMask;

        const float KMaxNavMeshDistance = 1f;

        RaycastHitComparer _mRaycastHitComparer;

        [SerializeField]
        ServerCharacter m_ServerCharacter;

        /// <summary>
        /// This event fires at the time when an action request is sent to the server.
        /// </summary>
        public event Action<ActionRequestData> ActionInputEvent;

        /// <summary>
        /// This describes how a skill was requested. Skills requested via mouse click will do raycasts to determine their target; skills requested
        /// in other matters will use the stateful target stored in NetworkCharacterState.
        /// </summary>
        public enum SkillTriggerStyle
        {
            None,        //no skill was triggered.
            MouseClick,  //skill was triggered via mouse-click implying you should do a raycast from the mouse position to find a target.
            Keyboard,    //skill was triggered via a Keyboard press, implying target should be taken from the active target.
            KeyboardRelease, //represents a released key.
            UI,          //skill was triggered from the UI, and similar to Keyboard, target should be inferred from the active target.
            UIRelease,   //represents letting go of the mouse-button on a UI button
        }

        bool IsReleaseStyle(SkillTriggerStyle style)
        {
            return style == SkillTriggerStyle.KeyboardRelease || style == SkillTriggerStyle.UIRelease;
        }

        /// <summary>
        /// This struct essentially relays the call params of RequestAction to FixedUpdate. Recall that we may need to do raycasts
        /// as part of doing the action, and raycasts done outside of FixedUpdate can give inconsistent results (otherwise we would
        /// just expose PerformAction as a public method, and let it be called in whatever scoped it liked.
        /// </summary>
        /// <remarks>
        /// Reference: https://answers.unity.com/questions/1141633/why-does-fixedupdate-work-when-update-doesnt.html
        /// </remarks>
        struct ActionRequest
        {
            public SkillTriggerStyle TriggerStyle;
            public ActionID RequestedActionID;
            public ulong TargetId;
        }

        /// <summary>
        /// List of ActionRequests that have been received since the last FixedUpdate ran. This is a static array, to avoid allocs, and
        /// because we don't really want to let this list grow indefinitely.
        /// </summary>
        readonly ActionRequest[] _mActionRequests = new ActionRequest[5];

        /// <summary>
        /// Number of ActionRequests that have been queued since the last FixedUpdate.
        /// </summary>
        int _mActionRequestCount;

        BaseActionInput _mCurrentSkillInput;

        bool _mMoveRequest;

        Camera _mMainCamera;

        public event Action<Vector3> ClientMoveEvent;

        /// <summary>
        /// Convenience getter that returns our CharacterData
        /// </summary>
        CharacterClass CharacterClass => m_ServerCharacter.CharacterClass;

        [SerializeField]
        PhysicsWrapper m_PhysicsWrapper;

        public ActionState ActionState1 { get; private set; }

        public ActionState ActionState2 { get; private set; }

        public ActionState ActionState3 { get; private set; }

        public System.Action Action1ModifiedCallback;

        ServerCharacter _mTargetServerCharacter;

        void Awake()
        {
            _mMainCamera = Camera.main;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                // dont need to do anything else if not the owner
                return;
            }

            m_ServerCharacter.TargetId.OnValueChanged += OnTargetChanged;
            m_ServerCharacter.HeldNetworkObject.OnValueChanged += OnHeldNetworkObjectChanged;

            if (CharacterClass.Skill1 &&
                GameDataSource.Instance.TryGetActionPrototypeByID(CharacterClass.Skill1.ActionID, out var action1))
            {
                ActionState1 = new ActionState() { ActionID = action1.ActionID, Selectable = true };
            }
            if (CharacterClass.Skill2 &&
                GameDataSource.Instance.TryGetActionPrototypeByID(CharacterClass.Skill2.ActionID, out var action2))
            {
                ActionState2 = new ActionState() { ActionID = action2.ActionID, Selectable = true };
            }
            if (CharacterClass.Skill3 &&
                GameDataSource.Instance.TryGetActionPrototypeByID(CharacterClass.Skill3.ActionID, out var action3))
            {
                ActionState3 = new ActionState() { ActionID = action3.ActionID, Selectable = true };
            }

            _mGroundLayerMask = LayerMask.GetMask(new[] { "Ground" });
            _mActionLayerMask = LayerMask.GetMask(new[] { "PCs", "NPCs", "Ground" });

            _mRaycastHitComparer = new RaycastHitComparer();
        }

        public override void OnNetworkDespawn()
        {
            if (m_ServerCharacter)
            {
                m_ServerCharacter.TargetId.OnValueChanged -= OnTargetChanged;
                m_ServerCharacter.HeldNetworkObject.OnValueChanged -= OnHeldNetworkObjectChanged;
            }

            if (_mTargetServerCharacter)
            {
                _mTargetServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnTargetLifeStateChanged;
            }
        }

        void OnTargetChanged(ulong previousValue, ulong newValue)
        {
            if (_mTargetServerCharacter)
            {
                _mTargetServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnTargetLifeStateChanged;
            }

            _mTargetServerCharacter = null;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newValue, out var selection) &&
                selection.TryGetComponent(out _mTargetServerCharacter))
            {
                _mTargetServerCharacter.NetLifeState.LifeState.OnValueChanged += OnTargetLifeStateChanged;
            }

            UpdateAction1();
        }

        void OnHeldNetworkObjectChanged(ulong previousValue, ulong newValue)
        {
            UpdateAction1();
        }

        void OnTargetLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            UpdateAction1();
        }

        void FinishSkill()
        {
            _mCurrentSkillInput = null;
        }

        void SendInput(ActionRequestData action)
        {
            ActionInputEvent?.Invoke(action);
            m_ServerCharacter.ServerPlayActionRpc(action);
        }

        void FixedUpdate()
        {
            //play all ActionRequests, in FIFO order.
            for (int i = 0; i < _mActionRequestCount; ++i)
            {
                if (_mCurrentSkillInput != null)
                {
                    //actions requested while input is active are discarded, except for "Release" requests, which go through.
                    if (IsReleaseStyle(_mActionRequests[i].TriggerStyle))
                    {
                        _mCurrentSkillInput.OnReleaseKey();
                    }
                }
                else if (!IsReleaseStyle(_mActionRequests[i].TriggerStyle))
                {
                    var actionPrototype = GameDataSource.Instance.GetActionPrototypeByID(_mActionRequests[i].RequestedActionID);
                    if (actionPrototype.Config.ActionInput != null)
                    {
                        var skillPlayer = Instantiate(actionPrototype.Config.ActionInput);
                        skillPlayer.Initiate(m_ServerCharacter, m_PhysicsWrapper.Transform.position, actionPrototype.ActionID, SendInput, FinishSkill);
                        _mCurrentSkillInput = skillPlayer;
                    }
                    else
                    {
                        PerformSkill(actionPrototype.ActionID, _mActionRequests[i].TriggerStyle, _mActionRequests[i].TargetId);
                    }
                }
            }

            _mActionRequestCount = 0;

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                return;
            }

            if (_mMoveRequest)
            {
                _mMoveRequest = false;
                if ((Time.time - _mLastSentMove) > KMoveSendRateSeconds)
                {
                    _mLastSentMove = Time.time;
                    var ray = _mMainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);

                    var groundHits = Physics.RaycastNonAlloc(ray,
                        _kCachedHit,
                        KMouseInputRaycastDistance,
                        _mGroundLayerMask);

                    if (groundHits > 0)
                    {
                        if (groundHits > 1)
                        {
                            // sort hits by distance
                            Array.Sort(_kCachedHit, 0, groundHits, _mRaycastHitComparer);
                        }

                        // verify point is indeed on navmesh surface
                        if (NavMesh.SamplePosition(_kCachedHit[0].point,
                                out var hit,
                                KMaxNavMeshDistance,
                                NavMesh.AllAreas))
                        {
                            m_ServerCharacter.ServerSendCharacterInputRpc(hit.position);

                            //Send our client only click request
                            ClientMoveEvent?.Invoke(hit.position);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Perform a skill in response to some input trigger. This is the common method to which all input-driven skill plays funnel.
        /// </summary>
        /// <param name="actionID">The action you want to play. Note that "Skill1" may be overriden contextually depending on the target.</param>
        /// <param name="triggerStyle">What sort of input triggered this skill?</param>
        /// <param name="targetId">(optional) Pass in a specific networkID to target for this action</param>
        void PerformSkill(ActionID actionID, SkillTriggerStyle triggerStyle, ulong targetId = 0)
        {
            Transform hitTransform = null;

            if (targetId != 0)
            {
                // if a targetId is given, try to find the object
                NetworkObject targetNetObj;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out targetNetObj))
                {
                    hitTransform = targetNetObj.transform;
                }
            }
            else
            {
                // otherwise try to find an object under the input position
                int numHits = 0;
                if (triggerStyle == SkillTriggerStyle.MouseClick)
                {
                    var ray = _mMainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                    numHits = Physics.RaycastNonAlloc(ray, _kCachedHit, KMouseInputRaycastDistance, _mActionLayerMask);
                }

                int networkedHitIndex = -1;
                for (int i = 0; i < numHits; i++)
                {
                    if (_kCachedHit[i].transform.GetComponentInParent<NetworkObject>())
                    {
                        networkedHitIndex = i;
                        break;
                    }
                }

                hitTransform = networkedHitIndex >= 0 ? _kCachedHit[networkedHitIndex].transform : null;
            }

            if (GetActionRequestForTarget(hitTransform, actionID, triggerStyle, out ActionRequestData playerAction))
            {
                //Don't trigger our move logic for a while. This protects us from moving just because we clicked on them to target them.
                _mLastSentMove = Time.time + KTargetMoveTimeout;

                SendInput(playerAction);
            }
            else if (!GameDataSource.Instance.GetActionPrototypeByID(actionID).IsGeneralTargetAction)
            {
                // clicked on nothing... perform an "untargeted" attack on the spot they clicked on.
                // (Different Actions will deal with this differently. For some, like archer arrows, this will fire an arrow
                // in the desired direction. For others, like mage's bolts, this will fire a "miss" projectile at the spot clicked on.)

                var data = new ActionRequestData();
                PopulateSkillRequest(_kCachedHit[0].point, actionID, ref data);

                SendInput(data);
            }
        }

        /// <summary>
        /// When you right-click on something you will want to do contextually different things. For example you might attack an enemy,
        /// but revive a friend. You might also decide to do nothing (e.g. right-clicking on a friend who hasn't FAINTED).
        /// </summary>
        /// <param name="hit">The Transform of the entity we clicked on, or null if none.</param>
        /// <param name="actionID">The Action to build for</param>
        /// <param name="triggerStyle">How did this skill play get triggered? Mouse, Keyboard, UI etc.</param>
        /// <param name="resultData">Out parameter that will be filled with the resulting action, if any.</param>
        /// <returns>true if we should play an action, false otherwise. </returns>
        bool GetActionRequestForTarget(Transform hit, ActionID actionID, SkillTriggerStyle triggerStyle, out ActionRequestData resultData)
        {
            resultData = new ActionRequestData();

            var targetNetObj = hit != null ? hit.GetComponentInParent<NetworkObject>() : null;

            //if we can't get our target from the submitted hit transform, get it from our stateful target in our ServerCharacter.
            if (!targetNetObj && !GameDataSource.Instance.GetActionPrototypeByID(actionID).IsGeneralTargetAction)
            {
                ulong targetId = m_ServerCharacter.TargetId.Value;
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out targetNetObj);
            }

            //sanity check that this is indeed a valid target.
            if (targetNetObj == null || !ActionUtils.IsValidTarget(targetNetObj.NetworkObjectId))
            {
                return false;
            }

            if (targetNetObj.TryGetComponent<ServerCharacter>(out var serverCharacter))
            {
                //Skill1 may be contextually overridden if it was generated from a mouse-click.
                if (actionID == CharacterClass.Skill1.ActionID && triggerStyle == SkillTriggerStyle.MouseClick)
                {
                    if (!serverCharacter.IsNpc && serverCharacter.LifeState == LifeState.Fainted)
                    {
                        //right-clicked on a downed ally--change the skill play to Revive.
                        actionID = GameDataSource.Instance.ReviveActionPrototype.ActionID;
                    }
                }
            }

            Vector3 targetHitPoint;
            if (PhysicsWrapper.TryGetPhysicsWrapper(targetNetObj.NetworkObjectId, out var movementContainer))
            {
                targetHitPoint = movementContainer.Transform.position;
            }
            else
            {
                targetHitPoint = targetNetObj.transform.position;
            }

            // record our target in case this action uses that info (non-targeted attacks will ignore this)
            resultData.ActionID = actionID;
            resultData.TargetIds = new ulong[] { targetNetObj.NetworkObjectId };
            PopulateSkillRequest(targetHitPoint, actionID, ref resultData);
            return true;
        }

        /// <summary>
        /// Populates the ActionRequestData with additional information. The TargetIds of the action should already be set before calling this.
        /// </summary>
        /// <param name="hitPoint">The point in world space where the click ray hit the target.</param>
        /// <param name="actionID">The action to perform (will be stamped on the resultData)</param>
        /// <param name="resultData">The ActionRequestData to be filled out with additional information.</param>
        void PopulateSkillRequest(Vector3 hitPoint, ActionID actionID, ref ActionRequestData resultData)
        {
            resultData.ActionID = actionID;
            var actionConfig = GameDataSource.Instance.GetActionPrototypeByID(actionID).Config;

            //most skill types should implicitly close distance. The ones that don't are explicitly set to false in the following switch.
            resultData.ShouldClose = true;

            // figure out the Direction in case we want to send it
            Vector3 offset = hitPoint - m_PhysicsWrapper.Transform.position;
            offset.y = 0;
            Vector3 direction = offset.normalized;

            switch (actionConfig.Logic)
            {
                //for projectile logic, infer the direction from the click position.
                case ActionLogic.LaunchProjectile:
                    resultData.Direction = direction;
                    resultData.ShouldClose = false; //why? Because you could be lining up a shot, hoping to hit other people between you and your target. Moving you would be quite invasive.
                    return;
                case ActionLogic.Melee:
                    resultData.Direction = direction;
                    return;
                case ActionLogic.Target:
                    resultData.ShouldClose = false;
                    return;
                case ActionLogic.Emote:
                    resultData.CancelMovement = true;
                    return;
                case ActionLogic.RangedFXTargeted:
                    resultData.Position = hitPoint;
                    return;
                case ActionLogic.DashAttack:
                    resultData.Position = hitPoint;
                    return;
                case ActionLogic.PickUp:
                    resultData.CancelMovement = true;
                    resultData.ShouldQueue = false;
                    return;
            }
        }

        /// <summary>
        /// Request an action be performed. This will occur on the next FixedUpdate.
        /// </summary>
        /// <param name="actionID"> The action you'd like to perform. </param>
        /// <param name="triggerStyle"> What input style triggered this action. </param>
        /// <param name="targetId"> NetworkObjectId of target. </param>
        public void RequestAction(ActionID actionID, SkillTriggerStyle triggerStyle, ulong targetId = 0)
        {
            Assert.IsNotNull(GameDataSource.Instance.GetActionPrototypeByID(actionID),
                $"Action with actionID {actionID} must be contained in the Action prototypes of GameDataSource!");

            if (_mActionRequestCount < _mActionRequests.Length)
            {
                _mActionRequests[_mActionRequestCount].RequestedActionID = actionID;
                _mActionRequests[_mActionRequestCount].TriggerStyle = triggerStyle;
                _mActionRequests[_mActionRequestCount].TargetId = targetId;
                _mActionRequestCount++;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && CharacterClass.Skill1)
            {
                RequestAction(ActionState1.ActionID, SkillTriggerStyle.Keyboard);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha1) && CharacterClass.Skill1)
            {
                RequestAction(ActionState1.ActionID, SkillTriggerStyle.KeyboardRelease);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && CharacterClass.Skill2)
            {
                RequestAction(ActionState2.ActionID, SkillTriggerStyle.Keyboard);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha2) && CharacterClass.Skill2)
            {
                RequestAction(ActionState2.ActionID, SkillTriggerStyle.KeyboardRelease);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && CharacterClass.Skill3)
            {
                RequestAction(ActionState3.ActionID, SkillTriggerStyle.Keyboard);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha3) && CharacterClass.Skill3)
            {
                RequestAction(ActionState3.ActionID, SkillTriggerStyle.KeyboardRelease);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                RequestAction(GameDataSource.Instance.Emote1ActionPrototype.ActionID, SkillTriggerStyle.Keyboard);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                RequestAction(GameDataSource.Instance.Emote2ActionPrototype.ActionID, SkillTriggerStyle.Keyboard);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                RequestAction(GameDataSource.Instance.Emote3ActionPrototype.ActionID, SkillTriggerStyle.Keyboard);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                RequestAction(GameDataSource.Instance.Emote4ActionPrototype.ActionID, SkillTriggerStyle.Keyboard);
            }

            if (!EventSystem.current.IsPointerOverGameObject() && _mCurrentSkillInput == null)
            {
                //IsPointerOverGameObject() is a simple way to determine if the mouse is over a UI element. If it is, we don't perform mouse input logic,
                //to model the button "blocking" mouse clicks from falling through and interacting with the world.

                if (Input.GetMouseButtonDown(1))
                {
                    RequestAction(CharacterClass.Skill1.ActionID, SkillTriggerStyle.MouseClick);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    RequestAction(GameDataSource.Instance.GeneralTargetActionPrototype.ActionID, SkillTriggerStyle.MouseClick);
                }
                else if (Input.GetMouseButton(0))
                {
                    _mMoveRequest = true;
                }
            }
        }

        void UpdateAction1()
        {
            var isHoldingNetworkObject =
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_ServerCharacter.HeldNetworkObject.Value,
                    out var heldNetworkObject);

            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_ServerCharacter.TargetId.Value,
                out var selection);

            var isSelectable = true;
            if (isHoldingNetworkObject)
            {
                // show drop!

                ActionState1.ActionID = GameDataSource.Instance.DropActionPrototype.ActionID;
            }
            else if ((m_ServerCharacter.TargetId.Value != 0
                    && selection != null
                    && selection.TryGetComponent(out PickUpState pickUpState))
               )
            {
                // special case: targeting a pickup-able item or holding a pickup object

                ActionState1.ActionID = GameDataSource.Instance.PickUpActionPrototype.ActionID;
            }
            else if (m_ServerCharacter.TargetId.Value != 0
                && selection != null
                && selection.NetworkObjectId != m_ServerCharacter.NetworkObjectId
                && selection.TryGetComponent(out ServerCharacter charState)
                && !charState.IsNpc)
            {
                // special case: when we have a player selected, we change the meaning of the basic action
                // we have another player selected! In that case we want to reflect that our basic Action is a Revive, not an attack!
                // But we need to know if the player is alive... if so, the button should be disabled (for better player communication)

                ActionState1.ActionID = GameDataSource.Instance.ReviveActionPrototype.ActionID;
                isSelectable = charState.NetLifeState.LifeState.Value != LifeState.Alive;
            }
            else
            {
                ActionState1.SetActionState(CharacterClass.Skill1.ActionID);
            }

            ActionState1.Selectable = isSelectable;

            Action1ModifiedCallback?.Invoke();
        }

        public class ActionState
        {
            public ActionID ActionID { get; internal set; }

            public bool Selectable { get; internal set; }

            internal void SetActionState(ActionID newActionID, bool isSelectable = true)
            {
                ActionID = newActionID;
                Selectable = isSelectable;
            }
        }
    }
}
