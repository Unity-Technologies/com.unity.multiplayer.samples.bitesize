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
        // The physic needs to be disabled when the ingredient is parented so that it's following the parent instead of moving freely.
        if (parentNetworkObject != null)
        {
            // Setting isKinematic to true will prevent the object from being affected by the physic in the game,
            // which will let it follow the parented player and not being bumped around by other rigidbodies.
            m_Rigidbody.isKinematic = true;
            // Rigibodies in Unity ignore parenting when updating their transform position.
            // Changing the interpolation to None when a Kinematic rigibody is the child of another rigidbody
            // will allow the object to keep the same local position.
            m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            m_NetworkTransform.InLocalSpace = true;
        }
        // If the ingredient is not parented anymore, the physic is turned back on.
        else
        {
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_NetworkTransform.InLocalSpace = false;
        }
    }
}
