using System.Collections;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.GameplayObjects.Character.AI;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Action = Unity.BossRoom.Gameplay.Actions.Action;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Contains all NetworkVariables, RPCs and server-side logic of a character.
    /// This class was separated in two to keep client and server context self contained. This way you don't have to continuously ask yourself if code is running client or server side.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState),
        typeof(NetworkLifeState),
        typeof(NetworkAvatarGuidState))]
    public class ServerCharacter : NetworkBehaviour, ITargetable
    {
        [FormerlySerializedAs("m_ClientVisualization")]
        [SerializeField]
        ClientCharacter m_ClientCharacter;

        public ClientCharacter ClientCharacter => m_ClientCharacter;

        [SerializeField]
        CharacterClass m_CharacterClass;

        public CharacterClass CharacterClass
        {
            get
            {
                if (m_CharacterClass == null)
                {
                    m_CharacterClass = _mState.RegisteredAvatar.CharacterClass;
                }

                return m_CharacterClass;
            }

            set => m_CharacterClass = value;
        }

        /// Indicates how the character's movement should be depicted.
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        public NetworkVariable<ulong> HeldNetworkObject { get; } = new NetworkVariable<ulong>();

        /// <summary>
        /// Indicates whether this character is in "stealth mode" (invisible to monsters and other players).
        /// </summary>
        public NetworkVariable<bool> IsStealthy { get; } = new NetworkVariable<bool>();

        public NetworkHealthState NetHealthState { get; private set; }

        /// <summary>
        /// The active target of this character.
        /// </summary>
        public NetworkVariable<ulong> TargetId { get; } = new NetworkVariable<ulong>();

        /// <summary>
        /// Current HP. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public int HitPoints
        {
            get => NetHealthState.HitPoints.Value;
            private set => NetHealthState.HitPoints.Value = value;
        }

        public NetworkLifeState NetLifeState { get; private set; }

        /// <summary>
        /// Current LifeState. Only Players should enter the FAINTED state.
        /// </summary>
        public LifeState LifeState
        {
            get => NetLifeState.LifeState.Value;
            private set => NetLifeState.LifeState.Value = value;
        }

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc => CharacterClass.IsNpc;

        public bool IsValidTarget => LifeState != LifeState.Dead;

        /// <summary>
        /// Returns true if the Character is currently in a state where it can play actions, false otherwise.
        /// </summary>
        public bool CanPerformActions => LifeState == LifeState.Alive;

        /// <summary>
        /// Character Type. This value is populated during character selection.
        /// </summary>
        public CharacterTypeEnum CharacterType => CharacterClass.CharacterType;

        private ServerActionPlayer _mServerActionPlayer;

        /// <summary>
        /// The Character's ActionPlayer. This is mainly exposed for use by other Actions. In particular, users are discouraged from
        /// calling 'PlayAction' directly on this, as the ServerCharacter has certain game-level checks it performs in its own wrapper.
        /// </summary>
        public ServerActionPlayer ActionPlayer => _mServerActionPlayer;

        [SerializeField]
        [Tooltip("If set to false, an NPC character will be denied its brain (won't attack or chase players)")]
        private bool m_BrainEnabled = true;

        [SerializeField]
        [Tooltip("Setting negative value disables destroying object after it is killed.")]
        private float m_KilledDestroyDelaySeconds = 3.0f;

        [SerializeField]
        [Tooltip("If set, the ServerCharacter will automatically play the StartingAction when it is created. ")]
        private Action m_StartingAction;


        [SerializeField]
        DamageReceiver m_DamageReceiver;

        [SerializeField]
        ServerCharacterMovement m_Movement;

        public ServerCharacterMovement Movement => m_Movement;

        [SerializeField]
        PhysicsWrapper m_PhysicsWrapper;

        public PhysicsWrapper PhysicsWrapper => m_PhysicsWrapper;

        [SerializeField]
        ServerAnimationHandler m_ServerAnimationHandler;

        public ServerAnimationHandler ServerAnimationHandler => m_ServerAnimationHandler;

        private AIBrain _mAIBrain;
        NetworkAvatarGuidState _mState;

        void Awake()
        {
            _mServerActionPlayer = new ServerActionPlayer(this);
            NetLifeState = GetComponent<NetworkLifeState>();
            NetHealthState = GetComponent<NetworkHealthState>();
            _mState = GetComponent<NetworkAvatarGuidState>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) { enabled = false; }
            else
            {
                NetLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
                m_DamageReceiver.DamageReceived += ReceiveHp;
                m_DamageReceiver.CollisionEntered += CollisionEntered;

                if (IsNpc)
                {
                    _mAIBrain = new AIBrain(this, _mServerActionPlayer);
                }

                if (m_StartingAction != null)
                {
                    var startingAction = new ActionRequestData() { ActionID = m_StartingAction.ActionID };
                    PlayAction(ref startingAction);
                }
                InitializeHitPoints();
            }
        }

        public override void OnNetworkDespawn()
        {
            NetLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;

            if (m_DamageReceiver)
            {
                m_DamageReceiver.DamageReceived -= ReceiveHp;
                m_DamageReceiver.CollisionEntered -= CollisionEntered;
            }
        }


        /// <summary>
        /// RPC to send inputs for this character from a client to a server.
        /// </summary>
        /// <param name="movementTarget">The position which this character should move towards.</param>
        [Rpc(SendTo.Server)]
        public void ServerSendCharacterInputRpc(Vector3 movementTarget)
        {
            if (LifeState == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                // if we're currently playing an interruptible action, interrupt it!
                if (_mServerActionPlayer.GetActiveActionInfo(out ActionRequestData data))
                {
                    if (GameDataSource.Instance.GetActionPrototypeByID(data.ActionID).Config.ActionInterruptible)
                    {
                        _mServerActionPlayer.ClearActions(false);
                    }
                }

                _mServerActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true); //clear target on move.
                m_Movement.SetMovementTarget(movementTarget);
            }
        }

        // ACTION SYSTEM

        /// <summary>
        /// Client->Server RPC that sends a request to play an action.
        /// </summary>
        /// <param name="data">Data about which action to play and its associated details. </param>
        [Rpc(SendTo.Server)]
        public void ServerPlayActionRpc(ActionRequestData data)
        {
            ActionRequestData data1 = data;
            if (!GameDataSource.Instance.GetActionPrototypeByID(data1.ActionID).Config.IsFriendly)
            {
                // notify running actions that we're using a new attack. (e.g. so Stealth can cancel itself)
                ActionPlayer.OnGameplayActivity(Action.GameplayActivity.UsingAttackAction);
            }

            PlayAction(ref data1);
        }

        // UTILITY AND SPECIAL-PURPOSE RPCs

        /// <summary>
        /// Called on server when the character's client decides they have stopped "charging up" an attack.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void ServerStopChargingUpRpc()
        {
            _mServerActionPlayer.OnGameplayActivity(Action.GameplayActivity.StoppedChargingUp);
        }

        void InitializeHitPoints()
        {
            HitPoints = CharacterClass.BaseHP.Value;

            if (!IsNpc)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData is { HasCharacterSpawned: true })
                {
                    HitPoints = sessionPlayerData.Value.CurrentHitPoints;
                    if (HitPoints <= 0)
                    {
                        LifeState = LifeState.Fainted;
                    }
                }
            }
        }

        /// <summary>
        /// Play a sequence of actions!
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            //the character needs to be alive in order to be able to play actions
            if (LifeState == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                if (action.CancelMovement)
                {
                    m_Movement.CancelMove();
                }

                _mServerActionPlayer.PlayAction(ref action);
            }
        }

        void OnLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            if (lifeState != LifeState.Alive)
            {
                _mServerActionPlayer.ClearActions(true);
                m_Movement.CancelMove();
            }
        }

        IEnumerator KilledDestroyProcess()
        {
            yield return new WaitForSeconds(m_KilledDestroyDelaySeconds);

            if (NetworkObject != null)
            {
                NetworkObject.Despawn(true);
            }
        }

        /// <summary>
        /// Receive an HP change from somewhere. Could be healing or damage.
        /// </summary>
        /// <param name="inflicter">Person dishing out this damage/healing. Can be null. </param>
        /// <param name="hp">The HP to receive. Positive value is healing. Negative is damage.  </param>
        void ReceiveHp(ServerCharacter inflicter, int hp)
        {
            //to our own effects, and modify the damage or healing as appropriate. But in this game, we just take it straight.
            if (hp > 0)
            {
                _mServerActionPlayer.OnGameplayActivity(Action.GameplayActivity.Healed);
                float healingMod = _mServerActionPlayer.GetBuffedValue(Action.BuffableValue.PercentHealingReceived);
                hp = (int)(hp * healingMod);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Don't apply damage if god mode is on
                if (NetLifeState.IsGodMode.Value)
                {
                    return;
                }
#endif

                _mServerActionPlayer.OnGameplayActivity(Action.GameplayActivity.AttackedByEnemy);
                float damageMod = _mServerActionPlayer.GetBuffedValue(Action.BuffableValue.PercentDamageReceived);
                hp = (int)(hp * damageMod);

                ServerAnimationHandler.NetworkAnimator.SetTrigger("HitReact1");
            }

            HitPoints = Mathf.Clamp(HitPoints + hp, 0, CharacterClass.BaseHP.Value);

            if (_mAIBrain != null)
            {
                //let the brain know about the modified amount of damage we received.
                _mAIBrain.ReceiveHp(inflicter, hp);
            }

            //we can't currently heal a dead character back to Alive state.
            //that's handled by a separate function.
            if (HitPoints <= 0)
            {
                if (IsNpc)
                {
                    if (m_KilledDestroyDelaySeconds >= 0.0f && LifeState != LifeState.Dead)
                    {
                        StartCoroutine(KilledDestroyProcess());
                    }

                    LifeState = LifeState.Dead;
                }
                else
                {
                    LifeState = LifeState.Fainted;
                }

                _mServerActionPlayer.ClearActions(false);
            }
        }

        /// <summary>
        /// Determines a gameplay variable for this character. The value is determined
        /// by the character's active Actions.
        /// </summary>
        /// <param name="buffType"></param>
        /// <returns></returns>
        public float GetBuffedValue(Action.BuffableValue buffType)
        {
            return _mServerActionPlayer.GetBuffedValue(buffType);
        }

        /// <summary>
        /// Receive a Life State change that brings Fainted characters back to Alive state.
        /// </summary>
        /// <param name="inflicter">Person reviving the character.</param>
        /// <param name="hp">The HP to set to a newly revived character.</param>
        public void Revive(ServerCharacter inflicter, int hp)
        {
            if (LifeState == LifeState.Fainted)
            {
                HitPoints = Mathf.Clamp(hp, 0, CharacterClass.BaseHP.Value);
                NetLifeState.LifeState.Value = LifeState.Alive;
            }
        }

        void Update()
        {
            _mServerActionPlayer.OnUpdate();
            if (_mAIBrain != null && LifeState == LifeState.Alive && m_BrainEnabled)
            {
                _mAIBrain.Update();
            }
        }

        void CollisionEntered(Collision collision)
        {
            if (_mServerActionPlayer != null)
            {
                _mServerActionPlayer.CollisionEntered(collision);
            }
        }

        /// <summary>
        /// This character's AIBrain. Will be null if this is not an NPC.
        /// </summary>
        public AIBrain AIBrain { get { return _mAIBrain; } }

    }
}
