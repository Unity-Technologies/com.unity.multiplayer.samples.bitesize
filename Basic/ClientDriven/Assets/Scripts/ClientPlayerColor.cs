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
            // OwnerClientId is used here for debugging purposes. A live game should use a session manager to make sure reconnecting players still get the same color, as client IDs could be reused for other clients between disconnect and reconnect. See Boss Room for a session manager example.
            material.SetFloat("_Color_Index", OwnerClientId);
        }

    }
}
