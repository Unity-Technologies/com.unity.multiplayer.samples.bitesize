using System;
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
        SetPhysics(parentNetworkObject == null);
    }

    void SetPhysics(bool isEnabled)
    {
        m_Rigidbody.isKinematic = !isEnabled;
        m_Rigidbody.interpolation = isEnabled ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        m_NetworkTransform.InLocalSpace = !isEnabled;
    }
}
