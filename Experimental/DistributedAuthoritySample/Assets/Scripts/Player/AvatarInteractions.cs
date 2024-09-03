using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(AvatarInputs))]
    class AvatarInteractions : NetworkBehaviour
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

        void Awake()
        {
            m_PickupableLayerMask = 1 << LayerMask.NameToLayer("Pickupable");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_InteractCollider.enabled = HasAuthority;

            // authority and non-authority subscribe to this event
            NetworkObjectDespawnedEvent.Susbscribe(OnNetworkObjectDespawned);

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

            if (m_AnimationEventRelayer)
            {
                m_AnimationEventRelayer.PickupActionAnimationEvent -= OnPickupActionAnimationEvent;
            }
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

        void OnNetworkObjectDespawned(NetworkObject other)
        {
            // compare to what's picked up -- if it matches our picked up object, release
        }

        void OnNetworkObjectOwnershipChaned(ulong previous, ulong current, NetworkObject other)
        {
            // compare to what's picked up -- if it matches our picked up object, release
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

            var gameplayEventInvokable = other.GetComponent<IGameplayEventInvokable>();
            gameplayEventInvokable.OnGameplayEvent += OnGameplayEvent;
        }

        IEnumerator SmoothLookAt(Transform target)
        {
            Quaternion initialRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            var elapsedTime = 0f;
            var duration = 0.23f; // Duration of the rotation in seconds
            var rotation = transform.rotation;
            while (elapsedTime < duration)
            {
                rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / duration);
                rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0); // Keep only the y-axis rotation
                transform.rotation = rotation;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure the final rotation is exactly towards the target
            rotation = targetRotation;
            rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0); // Keep only the y-axis rotation
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

        void OnPickupAction(ulong clientId)
        {
            var transferableObjectTransform = m_TransferableObject.transform;
            // Create FixedJoint and connect it to the player's hand
            transferableObjectTransform.position = m_PickupLocChild.transform.position;
            transferableObjectTransform.rotation = m_PickupLocChild.transform.rotation;

            var transferableObjectRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            // prevent collisions from this collider to the picked up object and vice-versa
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), true);

            transferableObjectRigidbody.useGravity = false;
            m_PickupLocFixedJoint.connectedBody = transferableObjectRigidbody;

            // get prop hands location
            var leftHand = m_TransferableObject.LeftHand;
            var rightHand = m_TransferableObject.RightHand;

            //m_TransferableObject.RightHandContact = m_RightHandContact.transform;
            //m_TransferableObject.LeftHandContact = m_LeftHandContact.transform;

            // align hand contacts with prop hands
            m_LeftHandContact.transform.position = leftHand.transform.position;
            m_RightHandContact.transform.position = rightHand.transform.position;
            m_LeftHandContact.transform.rotation = leftHand.transform.rotation;
            m_RightHandContact.transform.rotation = rightHand.transform.rotation;

            Debug.Log($"[Client-{clientId}] Picked up: " + m_TransferableObject.name);
        }

        // todo: make this a public static event to subscribe to, since all avatars need to know when a held
        // NetworkObject has just been despawned / changed ownership
        void OnGameplayEvent(NetworkObject networkObject, GameplayEvent gameplayEvent)
        {
            switch (gameplayEvent)
            {
                case GameplayEvent.Despawned:
                case GameplayEvent.OwnershipChange:

                    // unsubscribe
                    var gameplayEventInvokable = networkObject.GetComponent<IGameplayEventInvokable>();
                    gameplayEventInvokable.OnGameplayEvent -= OnGameplayEvent;

                    // revert collision
                    if (networkObject.TryGetComponent(out Collider otherCollider))
                    {
                        UnityEngine.Physics.IgnoreCollision(m_MainCollider, otherCollider, false);
                    }

                    // need a way to reset animator
                    m_AvatarNetworkAnimator.SetTrigger(k_DropId);

                    m_PickupLocFixedJoint.connectedBody = null;
                    m_TransferableObject = null;
                    m_CurrentTransferableObject.Value = new NetworkBehaviourReference();
                    break;
                default:
                    throw new Exception($"Unknown GameplayEvent {gameplayEvent}!");
            }
        }

        void DropAction()
        {
            if (!HasAuthority)
            {
                return;
            }

            m_AvatarNetworkAnimator.SetTrigger(k_DropId);
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
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);
            transferableRigidbody.useGravity = true;
            m_TransferableObject = null;
        }

        void ThrowAction(double holdDuration)
        {
            var transferableObjectRigidbody = m_TransferableObject.GetComponent<Rigidbody>();
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_TransferableObject.GetComponent<Collider>(), false);

            m_AvatarNetworkAnimator.SetTrigger(k_ThrowReleaseId);
            m_PickupLocFixedJoint.connectedBody = null;
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
    }
}
