using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.UI;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(AvatarInputs))]
    class AvatarInteractions : NetworkBehaviour, INetworkUpdateSystem
    {
        [SerializeField]
        AvatarNetworkAnimator m_AvatarNetworkAnimator;
        [SerializeField]
        AvatarAnimationEventRelayer m_AnimationEventRelayer;

        [SerializeField]
        Collider m_MainCollider;

        [SerializeField]
        AvatarInputs m_AvatarInputs;

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

        Collider[] m_Results = new Collider[1];

        LayerMask m_PickupableLayerMask;

        NetworkVariable<NetworkBehaviourReference> m_CurrentTransferableObject = new NetworkVariable<NetworkBehaviourReference>(new NetworkBehaviourReference());

        TransferableObject m_TransferableObject;

        PickUpIndicator m_PickUpIndicator;

        CarryBoxIndicator m_CarryBoxIndicator;

        PlayersTopUIController m_TopUIController;

        const float k_MinDurationHeld = 0f;
        const float k_MaxDurationHeld = 2f;

        static readonly int k_PickupId = Animator.StringToHash("Pickup");
        static readonly int k_DropId = Animator.StringToHash("Drop");
        static readonly int k_ThrowId = Animator.StringToHash("Throw");
        static readonly int k_ThrowReleaseId = Animator.StringToHash("ThrowRelease");

        Vector3 m_InitialInteractColliderSize;
        Vector3 m_BoneLocalPosition;

        void Awake()
        {
            m_PickupableLayerMask = 1 << LayerMask.NameToLayer("Pickupable");
            m_InitialInteractColliderSize = m_InteractCollider.size;
            m_BoneLocalPosition = transform.InverseTransformPoint(m_PickupLocChild.transform.parent.position);
        }

        void Update()
        {
            CheckForPickupsInRange();
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

            if (!m_AvatarInputs)
            {
                Debug.LogWarning("Assign AvatarInputs in the inspector!");
                return;
            }

            m_PickUpIndicator = FindFirstObjectByType<PickUpIndicator>();
            m_CarryBoxIndicator = FindFirstObjectByType<CarryBoxIndicator>();
            m_TopUIController = FindFirstObjectByType<PlayersTopUIController>();

            m_TopUIController.AddPlayer(gameObject);

            m_AvatarInputs.TapInteractionPerformed += OnTapPerformed;
            m_AvatarInputs.HoldInteractionPerformed += OnHoldStarted;
            m_AvatarInputs.HoldInteractionCancelled += OnHoldReleased;

            GameplayEventHandler.OnNetworkObjectDespawned += OnNetworkObjectDespawned;
            GameplayEventHandler.OnNetworkObjectOwnershipChanged += OnNetworkObjectOwnershipChanged;

            m_AnimationEventRelayer.PickupActionAnimationEvent += OnPickupActionAnimationEvent;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (m_AvatarInputs)
            {
                m_AvatarInputs.TapInteractionPerformed -= OnTapPerformed;
                m_AvatarInputs.HoldInteractionPerformed -= OnHoldStarted;
                m_AvatarInputs.HoldInteractionCancelled -= OnHoldReleased;
            }

            GameplayEventHandler.OnNetworkObjectDespawned -= OnNetworkObjectDespawned;
            GameplayEventHandler.OnNetworkObjectOwnershipChanged -= OnNetworkObjectOwnershipChanged;

            if (m_AnimationEventRelayer != null)
            {
                m_AnimationEventRelayer.PickupActionAnimationEvent -= OnPickupActionAnimationEvent;
            }

            m_TopUIController.RemovePlayer(gameObject);

            this.UnregisterAllNetworkUpdates();
        }

        protected override void OnNetworkSessionSynchronized()
        {
            // Synchronize late joining players with the item being carried
            if (!HasAuthority)
            {
                if (m_CurrentTransferableObject.Value.TryGet(out m_TransferableObject))
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
            if (m_TransferableObject != null && networkObject == m_TransferableObject.NetworkObject)
            {
                if (IsSpawned)
                {
                    m_AvatarNetworkAnimator.SetTrigger(k_DropId);
                }
                UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);
                m_TransferableObject = null;
                m_PickupLocFixedJoint.connectedBody = null;
                m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
            }
        }

        // invoked on authoritative instances
        void OnNetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
        {
            // compare to what's picked up -- if it matches our picked up object, drop
            if (m_TransferableObject != null && m_TransferableObject.NetworkObject == networkObject && !networkObject.HasAuthority)
            {
                m_AvatarNetworkAnimator.SetTrigger(k_DropId);
                m_InteractCollider.isTrigger = false;
                UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);
                m_TransferableObject = null;
                m_PickupLocFixedJoint.connectedBody = null;
                m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
            }
        }

        void OnTapPerformed()
        {
            if (m_TransferableObject != null)
            {
                OnObjectDroppedRpc(false);
                DropAction();
            }
            else
            {
                TryPickUp();
            }
        }

        void OnHoldStarted()
        {
            if (m_TransferableObject != null)
            {
                m_AvatarNetworkAnimator.SetTrigger(k_ThrowId);
            }
        }

        void OnHoldReleased(double holdDuration)
        {
            if (m_TransferableObject != null)
            {
                OnObjectDroppedRpc(true);
                ThrowAction(holdDuration);
            }
        }

        void CheckForPickupsInRange()
        {
            if (m_TransferableObject != null)
            {
                m_PickUpIndicator.ClearPickup();
                return;
            }

            m_CarryBoxIndicator.HideCarry();

            if (UnityEngine.Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position, m_InteractCollider.bounds.extents, m_Results, Quaternion.identity, mask: m_PickupableLayerMask) > 0)
            {
                if (m_Results[0].TryGetComponent(out NetworkObject otherNetworkObject))
                {
                    m_PickUpIndicator.ShowPickup(otherNetworkObject.transform);
                    return;
                }
            }
            m_PickUpIndicator.ClearPickup();
        }

        void TryPickUp()
        {
            if (UnityEngine.Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position + (transform.forward * 0.5f), Vector3.one, m_Results, Quaternion.identity, mask: m_PickupableLayerMask) > 0)
            {
                if (m_Results[0].TryGetComponent(out TransferableObject otherTransferableObject))
                {
                    HandleOwnershipTransfer(otherTransferableObject);
                }
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

            m_TransferableObject = otherTransferableObject;
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
                if (m_Results[0].TryGetComponent(out IOwnershipRequestable otherRequestable))
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
            StartPickup(other);
        }

        void StartPickup(NetworkBehaviour other)
        {
            // For late joining players
            m_CurrentTransferableObject.Value = new NetworkBehaviourReference(other);
            // set ownership status to request required, now that this object is being held
            other.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, clearAndSet: true);
            m_TransferableObject.SetObjectState(TransferableObject.ObjectState.PickedUp);
            // For immediate notification
            OnObjectPickedUpRpc(m_CurrentTransferableObject.Value);
            // Rotate the player to face the item smoothly
            StartCoroutine(SmoothLookAt(other.transform));
            m_AvatarNetworkAnimator.SetTrigger(k_PickupId);
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

            // show indicator for carry
            m_CarryBoxIndicator.ShowCarry(transform);

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

            if (m_TransferableObject == null || !m_TransferableObject.IsSpawned)
            {
                // object being picked up may have been despawned while trying to pick it up
                return;
            }

            OnPickupAction(OwnerClientId);
        }

        [Rpc(SendTo.NotAuthority)]
        void OnObjectPickedUpRpc(NetworkBehaviourReference networkBehaviourReference, RpcParams rpcParams = default)
        {
            if (networkBehaviourReference.TryGet(out m_TransferableObject, NetworkManager)
                && m_TransferableObject.IsSpawned)
            {
                OnPickupAction(rpcParams.Receive.SenderClientId);
            }
        }

        void OnPickupAction(ulong _)
        {
            var transferableObjectTransform = m_TransferableObject.transform;
            // Create FixedJoint and connect it to the player's hand
            transferableObjectTransform.position = m_PickupLocChild.transform.position;
            transferableObjectTransform.rotation = m_PickupLocChild.transform.rotation;

            // prevent collisions from the main collider to the picked up object and vice versa
            var transferableObjectCollider = m_TransferableObject.GetComponent<Collider>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, transferableObjectCollider, true);

            if (HasAuthority)
            {
                m_InteractCollider.isTrigger = false;
                if (transferableObjectCollider is BoxCollider boxCollider)
                {
                    m_InteractCollider.size = boxCollider.size;
                    m_InteractCollider.center = boxCollider.center;
                    m_InteractCollider.transform.localPosition = m_BoneLocalPosition;
                }
                else
                {
                    m_InteractCollider.size = transferableObjectCollider.bounds.size;
                }

                var transferableObjectRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
                transferableObjectRigidbody.useGravity = false;
                m_PickupLocFixedJoint.connectedBody = transferableObjectRigidbody;
            }

            // align hand contacts with prop hands
            m_LeftHandContact.transform.position = m_TransferableObject.LeftHand.transform.position;
            m_RightHandContact.transform.position = m_TransferableObject.RightHand.transform.position;
            m_LeftHandContact.transform.rotation = m_TransferableObject.LeftHand.transform.rotation;
            m_RightHandContact.transform.rotation = m_TransferableObject.RightHand.transform.rotation;
        }

        // invoked by authority
        void DropAction()
        {
            m_AvatarNetworkAnimator.SetTrigger(k_DropId);
            m_PickupLocFixedJoint.connectedBody = null;
            // unlock the object when dropped
            SetTransferableObjectAsTransferableDistributable();
            OnDropAction();
            m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
        }

        // invoked on all clients
        void OnDropAction()
        {
            ResetMainCollider();
            var transferableRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);
            transferableRigidbody.useGravity = true;
            m_TransferableObject.SetObjectState(TransferableObject.ObjectState.AtRest);
            m_TransferableObject = null;
        }

        // invoked by authority
        void ThrowAction(double holdDuration)
        {
            m_AvatarNetworkAnimator.SetTrigger(k_ThrowReleaseId);
            m_PickupLocFixedJoint.connectedBody = null;
            // unlock the object when thrown
            SetTransferableObjectAsTransferableDistributable();

            // apply a force to the released object
            var transferableObjectRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            float timeHeldClamped = Mathf.Clamp((float)holdDuration, k_MinDurationHeld, k_MaxDurationHeld);
            float tossForce = Mathf.Lerp(m_MinTossForce, m_MaxTossForce, Mathf.Clamp(timeHeldClamped, 0f, 1f));
            transferableObjectRigidbody.AddForce(transform.forward * tossForce, ForceMode.Impulse);

            OnThrowAction();
            m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
        }

        // invoked on all clients
        void OnThrowAction()
        {
            ResetMainCollider();
            m_TransferableObject.SetObjectState(TransferableObject.ObjectState.Thrown);
            var transferableRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);
            transferableRigidbody.useGravity = true;
            m_TransferableObject = null;
        }

        void SetTransferableObjectAsTransferableDistributable()
        {
            m_TransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
            m_TransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
        }

        void ResetMainCollider()
        {
            m_InteractCollider.isTrigger = true;
            m_InteractCollider.center = Vector3.zero;
            m_InteractCollider.size = m_InitialInteractColliderSize;
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

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.PreLateUpdate:
                    // if this instance is carrying something, then keep connection points synchronized with object being carried
                    if (m_TransferableObject != null)
                    {
                        m_LeftHandContact.transform.position = m_TransferableObject.LeftHand.transform.position;
                        m_RightHandContact.transform.position = m_TransferableObject.RightHand.transform.position;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
    }
}
