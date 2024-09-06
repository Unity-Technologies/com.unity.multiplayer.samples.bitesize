using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
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
        Collider m_InteractCollider;

        [SerializeField]
        float m_MinTossForce;

        [SerializeField]
        float m_MaxTossForce;



        Collider[] m_Results = new Collider[1];

        LayerMask m_PickupableLayerMask;

        NetworkVariable<NetworkBehaviourReference> m_CurrentTransferableObject = new NetworkVariable<NetworkBehaviourReference>(new NetworkBehaviourReference());

        TransferableObject m_TransferableObject;

        const float k_MinDurationHeld = 0f;
        const float k_MaxDurationHeld = 2f;

        static readonly int k_PickupId = Animator.StringToHash("Pickup");
        static readonly int k_DropId = Animator.StringToHash("Drop");
        static readonly int k_ThrowId = Animator.StringToHash("Throw");
        static readonly int k_ThrowReleaseId = Animator.StringToHash("ThrowRelease");

        private Vector3 m_OriginalBoundsLocalPosition;
        private Vector3 m_OriginalBoundsScale;

        void Awake()
        {
            m_PickupableLayerMask = 1 << LayerMask.NameToLayer("Pickupable");

            m_OriginalBoundsLocalPosition = m_InteractCollider.transform.localPosition;
            m_OriginalBoundsScale = m_InteractCollider.transform.localScale;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_InteractCollider.enabled = HasAuthority;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            if (!HasAuthority)
            {
                return;
            }

            if (!m_AvatarInputs)
            {
                Debug.LogWarning("Assign AvatarInputs in the inspector!");
                return;
            }

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
                PickUp();
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

        void PickUp()
        {
            if (UnityEngine.Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position, m_InteractCollider.bounds.extents, m_Results, Quaternion.identity, mask: m_PickupableLayerMask) > 0)
            {
                if (m_Results[0].TryGetComponent(out NetworkObject otherNetworkObject)
                    && otherNetworkObject.TryGetComponent(out TransferableObject otherTransferableObject))
                {
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
            OnPickupAction(OwnerClientId);
        }

        [Rpc(SendTo.NotAuthority)]
        void OnObjectPickedUpRpc(NetworkBehaviourReference networkBehaviourReference, RpcParams rpcParams = default)
        {
            if (networkBehaviourReference.TryGet(out m_TransferableObject, NetworkManager))
            {
                OnPickupAction(rpcParams.Receive.SenderClientId);
            }
        }

        private void ScaleToColliderBounds(Collider boxCollider)
        {
            var scale = m_InteractCollider.transform.localScale;
            scale.x = boxCollider.bounds.extents.x / m_InteractCollider.bounds.extents.x;
            scale.y = boxCollider.bounds.extents.y / m_InteractCollider.bounds.extents.y;
            scale.z = boxCollider.bounds.extents.z / m_InteractCollider.bounds.extents.z;
            m_InteractCollider.transform.localScale = scale * 0.90f;
            m_InteractCollider.transform.position = m_PickupLocFixedJoint.transform.parent.position;
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, boxCollider, true);
            UnityEngine.Physics.IgnoreCollision(m_InteractCollider, boxCollider, true);
            UnityEngine.Physics.IgnoreCollision(m_InteractCollider, m_MainCollider, true);
            m_InteractCollider.isTrigger = false;
        }

        private void ResetColliderBounds(Collider boxCollider)
        {
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, boxCollider, false);
            UnityEngine.Physics.IgnoreCollision(m_InteractCollider, boxCollider, false);
            UnityEngine.Physics.IgnoreCollision(m_InteractCollider, m_MainCollider, false);
            m_InteractCollider.transform.localScale = m_OriginalBoundsScale;
            m_InteractCollider.transform.localPosition = m_OriginalBoundsLocalPosition;
            m_InteractCollider.isTrigger = true;
        }


        void OnPickupAction(ulong clientId)
        {
            var transferableObjectTransform = m_TransferableObject.transform;
            // Create FixedJoint and connect it to the player's hand
            transferableObjectTransform.position = m_PickupLocChild.transform.position;
            transferableObjectTransform.rotation = m_PickupLocChild.transform.rotation;

            // prevent collisions from this collider to the picked up object and vice-versa
            var objectCollider = m_TransferableObject.GetComponent<Collider>();
            ScaleToColliderBounds(objectCollider);

            UnityEngine.Physics.IgnoreCollision(m_MainCollider, objectCollider, true);

            var transferableObjectRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            transferableObjectRigidbody.detectCollisions = false;
            if (HasAuthority)
            {
                transferableObjectRigidbody.useGravity = false;
                m_PickupLocFixedJoint.connectedBody = transferableObjectRigidbody;
            }

            // align hand contacts with prop hands
            m_LeftHandContact.transform.position = m_TransferableObject.LeftHand.transform.position;
            m_RightHandContact.transform.position = m_TransferableObject.RightHand.transform.position;
            m_LeftHandContact.transform.rotation = m_TransferableObject.LeftHand.transform.rotation;
            m_RightHandContact.transform.rotation = m_TransferableObject.RightHand.transform.rotation;
        }

        void DropAction()
        {
            m_AvatarNetworkAnimator.SetTrigger(k_DropId);
            ResetColliderBounds(m_TransferableObject.GetComponent<Collider>());
            m_TransferableObject.GetComponent<Rigidbody>().detectCollisions = true;
            m_PickupLocFixedJoint.connectedBody = null;
            m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
            // set ownership status to request required, now that this object is being held
            m_TransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
            m_TransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            OnDropAction();
        }

        void OnDropAction()
        {
            var transferableRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            var objectCollider = m_TransferableObject.GetComponent<Collider>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, objectCollider, false);
            UnityEngine.Physics.IgnoreCollision(m_InteractCollider, objectCollider, false);
            transferableRigidbody.useGravity = true;
            m_TransferableObject = null;
        }


        void ThrowAction(double holdDuration)
        {
            var transferableObjectRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            var objectCollider = m_TransferableObject.GetComponent<Collider>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, objectCollider, false);
            m_AvatarNetworkAnimator.SetTrigger(k_ThrowReleaseId);
            m_PickupLocFixedJoint.connectedBody = null;
            ResetColliderBounds(objectCollider);
            transferableObjectRigidbody.detectCollisions = true;
            // Unlock the object when we drop it
            m_TransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
            m_TransferableObject.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            transferableObjectRigidbody.detectCollisions = true;
            transferableObjectRigidbody.useGravity = true;

            // apply a force to the released object
            float timeHeldClamped = Mathf.Clamp((float)holdDuration, k_MinDurationHeld, k_MaxDurationHeld);
            float tossForce = Mathf.Lerp(m_MinTossForce, m_MaxTossForce, Mathf.Clamp(timeHeldClamped, 0f, 1f));
            transferableObjectRigidbody.AddForce(transform.forward * tossForce, ForceMode.Impulse);

            m_TransferableObject = null;
            m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
        }

        void OnThrowAction()
        {
            var transferableRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);
            transferableRigidbody.useGravity = true;
            m_TransferableObject = null;
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
                case NetworkUpdateStage.FixedUpdate:
                    // if this instance is carrying something, then keep connection points synchronized with object being carried
                    if (m_TransferableObject != null)
                    {
                        m_LeftHandContact.transform.position = m_TransferableObject.LeftHand.transform.position;
                        m_RightHandContact.transform.position = m_TransferableObject.RightHand.transform.position;
                        m_InteractCollider.transform.position = m_TransferableObject.GetCenterOffset();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
    }
}
