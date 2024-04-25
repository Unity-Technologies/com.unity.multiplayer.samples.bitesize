using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ServerIngredientPhysics : NetworkBehaviour
{
    [SerializeField]
    NetworkTransform m_NetworkTransform;

    [SerializeField]
    Rigidbody m_Rigidbody;

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        // The ingredient's physic needs to be disabled when the ingredient is parented so that it's following the parent instead of moving freely.
        SetKinematic(parentNetworkObject != null);
    }

    private void SetKinematic(bool isEnabled)
    {
        // Setting isKinematic to true will prevent the object from being affected by the physic forces in the game.
        m_Rigidbody.isKinematic = isEnabled;
        // When parented, rigidbody children are only moving because they receive their parent forces along with their own physic.
        // When kinematic is true, they will ignore their parent forces and stay at the same world place.
        // Changing the interpolation to None in that case will allow the object to keep the same local position instead of the world position.
        m_Rigidbody.interpolation = isEnabled ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
        m_NetworkTransform.InLocalSpace = isEnabled;
    }
}
