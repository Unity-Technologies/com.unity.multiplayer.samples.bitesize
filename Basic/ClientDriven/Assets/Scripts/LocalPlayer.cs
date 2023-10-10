using System;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Debug = System.Diagnostics.Debug;

public class LocalPlayer : NetworkBehaviour
{
    public static event Action<ClientNetworkTransform> OnNetworkSpawnEvent;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            return;
        }        
        Debug.Assert(OnNetworkSpawnEvent != null, nameof(OnNetworkSpawnEvent) + " != null");
        OnNetworkSpawnEvent?.Invoke(GetComponent<ClientNetworkTransform>());
    }
}
