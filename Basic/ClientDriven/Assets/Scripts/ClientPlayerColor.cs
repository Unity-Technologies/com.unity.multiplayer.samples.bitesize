using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A script to set the color of each player based on OwnerClientId
/// </summary>

public class ClientPlayerColor : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SkinnedMeshRenderer m_Renderer = GetComponent<SkinnedMeshRenderer>();

        foreach (var material in m_Renderer.materials)
        {
            material.SetFloat("_Color_Index", OwnerClientId);
        }
        
    }
}
