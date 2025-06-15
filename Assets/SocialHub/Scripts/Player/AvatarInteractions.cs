using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    class AvatarInteractions : NetworkBehaviour, INetworkUpdateSystem
    {
        [SerializeField]
        AvatarNetworkAnimator m_AvatarNetworkAnimator;
        [SerializeField]
        AvatarAnimationEventRelayer m_AnimationEventRelayer;

        [SerializeField]
        Collider m_MainCollider;

        [SerializeField]
        FixedJoint m_PickupLocFixedJoint;
        [SerializeField]
        GameObject m_PickupLocChild;
        [SerializeField]
        GameObject m_LeftHandContact;
        [SerializeField]
        GameObject m_RightHandContact;

        [SerializeField]
        BoxCollider m_InteractCollider;

        [SerializeField]
        float m_MinTossForce;

        [SerializeField]
        float m_MaxTossForce;

        Collider[] _mResults = new Collider[4];

        LayerMask _mPickupableLayerMask;

        Collider _mPotentialPickupCollider;

        NetworkVariable<NetworkBehaviourReference> _mCurrentTransferableObject = new NetworkVariable<NetworkBehaviourReference>(new NetworkBehaviourReference());

        TransferableObject _mTransferableObject;

        const float KMinDurationHeld = 0f;
        const float KMaxDurationHeld = 2f;

        static readonly int KPickupId = Animator.StringToHash("Pickup");
        static readonly int KDropId = Animator.StringToHash("Drop");
        static readonly int KThrowId = Animator.StringToHash("Throw");
        static readonly int KThrowReleaseId = Animator.StringToHash("ThrowRelease");
        static readonly int KPickUpDefault = Animator.StringToHash("Pick-Up.Default");

        Vector3 _mInitialInteractColliderSize;
        Vector3 _mInitialInteractColliderLocalPosition;
        Vector3 _mBoneLocalPosition;

        // tracking when a Hold interaction has started/ended
        bool _mHoldingInteractionPerformed;

        void Awake()
        {
            _mPickupableLayerMask = 1 << LayerMask.NameToLayer("Pickupable");
            _mInitialInteractColliderSize = m_InteractCollider.size;
            _mInitialInteractColliderLocalPosition = m_InteractCollider.transform.localPosition;
            _mBoneLocalPosition = transform.InverseTransformPoint(m_PickupLocChild.transform.parent.position);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_InteractCollider.enabled = HasAuthority;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.PreLateUpdate);

            if (!HasAuthority)
            {
                return;
            }

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            GameInput.Actions.Player.Interact.performed += OnInteractPerformed;
            GameInput.Actions.Player.Interact.canceled += OnInteractCanceled;

            GameplayEventHandler.OnNetworkObjectDespawned += OnNetworkObjectDespawned;
            GameplayEventHandler.OnNetworkObjectOwnershipChanged += OnNetworkObjectOwnershipChanged;

            m_AnimationEventRelayer.PickupActionAnimationEvent += OnPickupActionAnimationEvent;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            GameInput.Actions.Player.Interact.performed -= OnInteractPerformed;
            GameInput.Actions.Player.Interact.canceled -= OnInteractCanceled;

            GameplayEventHandler.OnNetworkObjectDespawned -= OnNetworkObjectDespawned;
            GameplayEventHandler.OnNetworkObjectOwnershipChanged -= OnNetworkObjectOwnershipChanged;

            if (m_AnimationEventRelayer != null)
            {
                m_AnimationEventRelayer.PickupActionAnimationEvent -= OnPickupActionAnimationEvent;
            }

            this.UnregisterAllNetworkUpdates();
        }

        protected override void OnNetworkSessionSynchronized()
        {
            // Synchronize late joining players with the item being carried
            if (!HasAuthority)
            {
                if (_mCurrentTransferableObject.Value.TryGet(out _mTransferableObject))
                {
                    OnPickupAction(OwnerClientId);
                }
            }
            base.OnNetworkSessionSynchronized();
        }

        // invoked on authoritative instances
        void OnNetworkObjectDespawned(NetworkObject networkObject)
        {
            // compare to what's picked up -- if it matches our picked up object, release
            if (_mTransferableObject != null && networkObject == _mTransferableObject.NetworkObject)
            {
                DropAction();
            }
        }

        // invoked on authoritative instances
        void OnNetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
        {
            // compare to what's picked up -- if it matches our picked up object, drop
            if (_mTransferableObject != null && _mTransferableObject.NetworkObject == networkObject && !networkObject.HasAuthority)
            {
                DropAction();
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            switch (context.interaction)
            {
                case HoldInteraction:
                    _mHoldingInteractionPerformed = true;
                    OnHoldStarted();
                    break;
                case TapInteraction:
                    OnTapPerformed();
                    break;
            }
        }

        void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (context.interaction is HoldInteraction)
            {
                if (_mHoldingInteractionPerformed)
                {
                    OnHoldReleased(context.duration);
                }
                _mHoldingInteractionPerformed = false;
            }
        }

        void OnTapPerformed()
        {
            if (_mTransferableObject != null)
            {
                DropAction();
            }
            else
            {
                TryPickUp();
            }
        }

        void OnHoldStarted()
        {
            if (_mTransferableObject != null)
            {
                m_AvatarNetworkAnimator.SetTrigger(KThrowId);
            }
        }

        void OnHoldReleased(double holdDuration)
        {
            if (_mTransferableObject != null)
            {
                ThrowAction(holdDuration);
            }
        }

        void TryPickUp()
        {
            if (IsAbleToPickUp() && _mPotentialPickupCollider != null && _mPotentialPickupCollider.TryGetComponent(out TransferableObject otherTransferableObject))
            {
                HandleOwnershipTransfer(otherTransferableObject);
            }
        }

        void HandleOwnershipTransfer(TransferableObject otherTransferableObject)
        {
            var otherNetworkObject = otherTransferableObject.NetworkObject;
            // if NetworkObject is locked, nothing we can do but retry a pickup at another time
            if (otherNetworkObject.IsOwnershipLocked)
            {
                return;
            }

            // trivial case: other NetworkObject is owned by this client, we can attach to fixed joint
            if (otherNetworkObject.HasAuthority)
            {
                StartPickup(otherTransferableObject);
                return;
            }

            if (otherNetworkObject.IsOwnershipTransferable)
            {
                // can use change ownership directly
                otherNetworkObject.ChangeOwnership(OwnerClientId);

                StartPickup(otherTransferableObject);
            }
            else if (otherNetworkObject.IsOwnershipRequestRequired)
            {
                // if not transferable, we must request access to become owner
                if (otherTransferableObject is IOwnershipRequestable otherRequestable)
                {
                    var ownershipRequestStatus = otherNetworkObject.RequestOwnership();
                    if (ownershipRequestStatus == NetworkObject.OwnershipRequestStatus.RequestSent)
                    {
                        otherRequestable.OnNetworkObjectOwnershipRequestResponse += OnOwnershipRequestResponse;
                    }
                }
            }
        }

        void OnOwnershipRequestResponse(NetworkBehaviour other, NetworkObject.OwnershipRequestResponseStatus status)
        {
            // unsubscribe
            var ownershipRequestable = other.GetComponent<IOwnershipRequestable>();
            ownershipRequestable.OnNetworkObjectOwnershipRequestResponse -= OnOwnershipRequestResponse;

            if (status != NetworkObject.OwnershipRequestResponseStatus.Approved)
            {
                return;
            }

            if (other.TryGetComponent(out TransferableObject transferableObject))
            {
                StartPickup(transferableObject);
            }
        }

        void StartPickup(TransferableObject other)
        {
            // For late joining players
            _mCurrentTransferableObject.Value = new NetworkBehaviourReference(other);
            _mTransferableObject = other;
            // set ownership status to request required, now that this object is being held
            _mTransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, clearAndSet: true);
            _mTransferableObject.SetObjectState(TransferableObject.ObjectState.PickedUp);
            // For immediate notification
            OnObjectPickedUpRpc(_mCurrentTransferableObject.Value);
            // Rotate the player to face the item smoothly
            StartCoroutine(SmoothLookAt(other.transform));
            m_AvatarNetworkAnimator.SetTrigger(KPickupId);
            GameplayEventHandler.SetAvatarPickupState(PickupState.Carry, _mTransferableObject.transform);
        }

        IEnumerator SmoothLookAt(Transform target)
        {
            Quaternion initialRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            var elapsedTime = 0f;
            const float duration = 0.23f; // Duration of the rotation in seconds
            while (elapsedTime < duration)
            {
                Quaternion currentRotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / duration);
                currentRotation = Quaternion.Euler(0, currentRotation.eulerAngles.y, 0); // Keep only the y-axis rotation
                transform.rotation = currentRotation;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure the final rotation is exactly towards the target
            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0); // Keep only the y-axis rotation
        }

        /// <summary>
        /// Authority invokes this via animation
        /// </summary>
        void OnPickupActionAnimationEvent()
        {
            if (!HasAuthority)
            {
                return;
            }

            if (_mTransferableObject == null || !_mTransferableObject.IsSpawned)
            {
                // object being picked up may have been despawned while trying to pick it up
                return;
            }

            OnPickupAction(OwnerClientId);
        }

        [Rpc(SendTo.NotAuthority)]
        void OnObjectPickedUpRpc(NetworkBehaviourReference networkBehaviourReference, RpcParams rpcParams = default)
        {
            if (networkBehaviourReference.TryGet(out _mTransferableObject, NetworkManager)
                && _mTransferableObject.IsSpawned)
            {
                OnPickupAction(rpcParams.Receive.SenderClientId);
            }
        }

        void OnPickupAction(ulong _)
        {
            var transferableObjectTransform = _mTransferableObject.transform;
            // Create FixedJoint and connect it to the player's hand
            transferableObjectTransform.position = m_PickupLocChild.transform.position;
            transferableObjectTransform.rotation = m_PickupLocChild.transform.rotation;

            // prevent collisions from the main collider to the picked up object and vice versa
            var transferableObjectCollider = _mTransferableObject.GetComponent<Collider>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, transferableObjectCollider, true);

            if (HasAuthority)
            {
                m_InteractCollider.isTrigger = false;
                if (transferableObjectCollider is BoxCollider boxCollider)
                {
                    m_InteractCollider.size = boxCollider.size;
                    m_InteractCollider.center = boxCollider.center;
                    m_InteractCollider.transform.localPosition = _mBoneLocalPosition;
                }
                else
                {
                    m_InteractCollider.size = transferableObjectCollider.bounds.size;
                }

                var transferableObjectRigidbody = _mTransferableObject.GetComponent<Rigidbody>();
                transferableObjectRigidbody.useGravity = false;
                m_PickupLocFixedJoint.connectedBody = transferableObjectRigidbody;
            }

            // align hand contacts with prop hands
            m_LeftHandContact.transform.position = _mTransferableObject.LeftHand.transform.position;
            m_RightHandContact.transform.position = _mTransferableObject.RightHand.transform.position;
            m_LeftHandContact.transform.rotation = _mTransferableObject.LeftHand.transform.rotation;
            m_RightHandContact.transform.rotation = _mTransferableObject.RightHand.transform.rotation;
        }

        // invoked by authority
        void DropAction()
        {
            OnObjectDroppedRpc(false);
            m_AvatarNetworkAnimator.SetTrigger(KDropId);
            m_PickupLocFixedJoint.connectedBody = null;
            // unlock the object when dropped
            SetTransferableObjectAsTransferableDistributable();
            OnDropAction();
            _mCurrentTransferableObject.Value = new NetworkBehaviourReference();
            GameplayEventHandler.SetAvatarPickupState(PickupState.Inactive, null);
        }

        // invoked on all clients
        void OnDropAction()
        {
            ResetMainCollider();
            if (_mTransferableObject == null)
            {
                // object may be destroyed while dropped
                return;
            }
            var transferableRigidbody = _mTransferableObject.GetComponent<Rigidbody>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, _mTransferableObject.GetComponent<Collider>(), false);
            transferableRigidbody.useGravity = true;
            _mTransferableObject.SetObjectState(TransferableObject.ObjectState.AtRest);
            _mTransferableObject = null;
        }

        // invoked by authority
        void ThrowAction(double holdDuration)
        {
            OnObjectDroppedRpc(true);
            m_AvatarNetworkAnimator.SetTrigger(KThrowReleaseId);
            m_PickupLocFixedJoint.connectedBody = null;
            // unlock the object when thrown
            SetTransferableObjectAsTransferableDistributable();

            // apply a force to the released object
            var transferableObjectRigidbody = _mTransferableObject.GetComponent<Rigidbody>();
            float timeHeldClamped = Mathf.Clamp((float)holdDuration, KMinDurationHeld, KMaxDurationHeld);
            float tossForce = Mathf.Lerp(m_MinTossForce, m_MaxTossForce, Mathf.Clamp(timeHeldClamped, 0f, 1f));
            transferableObjectRigidbody.AddForce(transform.forward * tossForce, ForceMode.Impulse);

            OnThrowAction();
            _mCurrentTransferableObject.Value = new NetworkBehaviourReference();
            GameplayEventHandler.SetAvatarPickupState(PickupState.Inactive, null);
        }

        // invoked on all clients
        void OnThrowAction()
        {
            ResetMainCollider();
            if (_mTransferableObject == null)
            {
                // object may be destroyed while thrown
                return;
            }
            _mTransferableObject.SetObjectState(TransferableObject.ObjectState.Thrown);
            var transferableRigidbody = _mTransferableObject.GetComponent<Rigidbody>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, _mTransferableObject.GetComponent<Collider>(), false);
            transferableRigidbody.useGravity = true;
            _mTransferableObject = null;
        }

        void SetTransferableObjectAsTransferableDistributable()
        {
            if (_mTransferableObject != null)
            {
                _mTransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
                _mTransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            }
        }

        void ResetMainCollider()
        {
            m_InteractCollider.isTrigger = true;
            m_InteractCollider.center = Vector3.zero;
            m_InteractCollider.size = _mInitialInteractColliderSize;
            m_InteractCollider.transform.localPosition = _mInitialInteractColliderLocalPosition;
        }

        [Rpc(SendTo.NotAuthority)]
        void OnObjectDroppedRpc(bool isThrowing)
        {
            if (isThrowing)
            {
                OnThrowAction();
            }
            else
            {
                OnDropAction();
            }
        }

        void CheckForPickupsInRange()
        {
            if (_mTransferableObject != null)
            {
                return;
            }

            var hits = UnityEngine.Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position, m_InteractCollider.bounds.extents, _mResults, Quaternion.identity, mask: _mPickupableLayerMask);
            if (hits > 0)
            {
                var closestDistanceSqr = Mathf.Infinity;
                var position = transform.position;

                for (int i = 0; i < hits; i++)
                {
                    var resultCollider = _mResults[i];
                    var directionToTarget = resultCollider.transform.position - position;
                    var dSqrToTarget = directionToTarget.sqrMagnitude;

                    if (dSqrToTarget < closestDistanceSqr)
                    {
                        closestDistanceSqr = dSqrToTarget;
                        _mPotentialPickupCollider = resultCollider;
                    }
                }
                GameplayEventHandler.SetAvatarPickupState(PickupState.PickupInRange, _mPotentialPickupCollider.transform);
            }
            else
            {
                _mPotentialPickupCollider = null;
                GameplayEventHandler.SetAvatarPickupState(PickupState.Inactive, null);
            }
        }

        bool IsAbleToPickUp()
        {
            // Get the current state info for the base layer (layer 0)
            var currentStateInfo = m_AvatarNetworkAnimator.Animator.GetCurrentAnimatorStateInfo(1);
            return currentStateInfo.fullPathHash == KPickUpDefault;
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.FixedUpdate:
                    CheckForPickupsInRange();
                    break;
                case NetworkUpdateStage.PreLateUpdate:
                    // if this instance is carrying something, then keep connection points synchronized with object being carried
                    if (_mTransferableObject != null)
                    {
                        m_LeftHandContact.transform.position = _mTransferableObject.LeftHand.transform.position;
                        m_RightHandContact.transform.position = _mTransferableObject.RightHand.transform.position;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
    }
}
