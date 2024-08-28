using System;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(AvatarInputs))]
    class AvatarInteractions : NetworkBehaviour
    {
        [SerializeField]
        Collider m_MainCollider;

        [SerializeField]
        AvatarInputs m_AvatarInputs;

        [SerializeField]
        NetworkRigidbody m_NetworkRigidbody;

        [SerializeField]
        Transform m_HoldTransform;

        [SerializeField]
        Collider m_InteractCollider;

        [SerializeField]
        float m_MinTossForce;

        [SerializeField]
        float m_MaxTossForce;

        Collider[] m_Results = new Collider[1];

        LayerMask m_PickupableLayerMask;

        NetworkRigidbody m_HoldingRigidbody;

        const float k_MinDurationHeld = 0f;
        const float k_MaxDurationHeld = 2f;

        void Awake()
        {
            m_PickupableLayerMask = 1 << LayerMask.NameToLayer("Pickupable");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority)
            {
                return;
            }

            if (!m_AvatarInputs)
            {
                Debug.LogWarning("Assign AvatarInputs in the inspector!");
                return;
            }

            m_AvatarInputs.InteractTapped += OnTapPerformed;
            m_AvatarInputs.InteractHeld += OnHoldReleased;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (m_AvatarInputs)
            {
                m_AvatarInputs.InteractTapped -= OnTapPerformed;
                m_AvatarInputs.InteractHeld -= OnHoldReleased;
            }
        }

        void OnTapPerformed()
        {
            if (m_HoldingRigidbody != null)
            {
                ReleaseHeldObject();
            }
            else
            {
                PickUp();
            }
        }

        void OnHoldReleased(double holdDuration)
        {
            if (m_HoldingRigidbody != null)
            {
                Toss(holdDuration);
            }
        }

        void PickUp()
        {
            if (UnityEngine.Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position, m_InteractCollider.bounds.extents, m_Results, Quaternion.identity, mask: m_PickupableLayerMask) > 0)
            {
                if (m_Results[0].TryGetComponent(out NetworkObject otherNetworkObject))
                {
                    // if NetworkObject is locked, nothing we can do but retry a pickup at another time
                    if (otherNetworkObject.IsOwnershipLocked)
                    {
                        return;
                    }

                    // trivial case: other NetworkObject is owned by this client, we can attach to fixed joint
                    if (otherNetworkObject.HasAuthority)
                    {
                        AttachToFixedJoint(otherNetworkObject);
                        return;
                    }

                    if (otherNetworkObject.IsOwnershipTransferable)
                    {
                        // can use change ownership directly
                        otherNetworkObject.ChangeOwnership(OwnerClientId);

                        // we can attach it via FixedPoint now as we are now owning this NetworkObject
                        AttachToFixedJoint(otherNetworkObject);
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

        void OnOwnershipRequestResponse(NetworkObject other, NetworkObject.OwnershipRequestResponseStatus status)
        {
            // unsubscribe
            var ownershipRequestable = other.GetComponent<IOwnershipRequestable>();
            ownershipRequestable.OnNetworkObjectOwnershipRequestResponse -= OnOwnershipRequestResponse;

            if (status != NetworkObject.OwnershipRequestResponseStatus.Approved)
            {
                return;
            }

            AttachToFixedJoint(other);
        }

        void AttachToFixedJoint(NetworkObject other)
        {
            if (!other.TryGetComponent(out NetworkRigidbody otherNetworkRigidbody))
            {
                return;
            }

            var success = otherNetworkRigidbody.AttachToFixedJoint(m_NetworkRigidbody, m_HoldTransform.position, massScale: 0.00001f);
            if (success)
            {
                m_HoldingRigidbody = otherNetworkRigidbody;

                // set ownership status to request required, now that this object is being held
                m_HoldingRigidbody.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, clearAndSet: true);

                var gameplayEventInvokable = other.GetComponent<IGameplayEventInvokable>();
                gameplayEventInvokable.OnGameplayEvent += OnGameplayEvent;

                // prevent collisions from this collider to the picked up object and vice-versa
                UnityEngine.Physics.IgnoreCollision(m_MainCollider, other.GetComponent<Collider>(), true);
            }
        }

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

                    // don't have ownership of the item, thus we can't invoke DetachFromFixedJoint(), but we need to remove created FixedJoint component
                    if (networkObject.TryGetComponent(out FixedJoint fixedJoint))
                    {
                        Destroy(fixedJoint);
                    }

                    m_HoldingRigidbody = null;

                    break;
                default:
                    throw new Exception($"Unknown GameplayEvent {gameplayEvent}!");
            }
        }

        void ReleaseHeldObject()
        {
            // set ownership status to request required, now that this object is being held
            m_HoldingRigidbody.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
            m_HoldingRigidbody.NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);

            m_HoldingRigidbody.DetachFromFixedJoint();
            m_HoldingRigidbody.GetComponent<Rigidbody>().useGravity = true;
            UnityEngine.Physics.IgnoreCollision(m_MainCollider, m_HoldingRigidbody.GetComponent<Collider>(), false);
            m_HoldingRigidbody = null;
        }

        void Toss(double holdDuration)
        {
            var heldRigidbody = m_HoldingRigidbody.GetComponent<Rigidbody>();
            ReleaseHeldObject();

            // apply a force to the released object
            float timeHeldClamped = Mathf.Clamp((float)holdDuration, k_MinDurationHeld, k_MaxDurationHeld);
            float tossForce = Mathf.Lerp(m_MinTossForce, m_MaxTossForce, Mathf.Clamp(timeHeldClamped, 0f, 1f));
            heldRigidbody.AddForce(transform.forward * tossForce, ForceMode.Impulse);
        }
    }
}
