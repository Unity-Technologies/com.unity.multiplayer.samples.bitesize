using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using UnityEngine;

/// <summary>
/// Component inheriting from <see cref="ClientNetworkTransform"/>, where server-driven player position changes are
/// applied to the owning client.
/// </summary>
/// <remarks>
/// Handling movement inside this component's OnNetworkSpawn method only ensures the mitigation of race condition issues
/// arising due to the execution order of other NetworkBehaviours' OnNetworkSpawn methods.
/// </remarks>
[RequireComponent(typeof(ServerPlayerMove))]
[DisallowMultipleComponent]
public class ClientDrivenNetworkTransform : ClientNetworkTransform
{
    [SerializeField]
    ServerPlayerMove m_ServerPlayerMove;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient && IsOwner)
        {
            SetPosition(Vector3.zero, m_ServerPlayerMove.spawnPosition.Value);
            m_ServerPlayerMove.spawnPosition.OnValueChanged += SetPosition;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (m_ServerPlayerMove != null)
        {
            m_ServerPlayerMove.spawnPosition.OnValueChanged -= SetPosition;
        }
    }

    void SetPosition(Vector3 previousValue, Vector3 newValue)
    {
        transform.position = newValue;
    }
}
