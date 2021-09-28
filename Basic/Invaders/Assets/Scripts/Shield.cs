using Unity.Netcode;
using UnityEngine;

public class Shield : MonoBehaviour
{
    protected void Start()
    {
        var networkingManager = NetworkManager.Singleton;
        if (networkingManager && networkingManager.IsServer) InvadersGame.Singleton.RegisterSpawnableObject(InvadersObjectType.Shield, gameObject);
    }

    protected void OnDestroy()
    {
        var networkingManager = NetworkManager.Singleton;
        if (networkingManager && networkingManager.IsServer) InvadersGame.Singleton.UnregisterSpawnableObject(InvadersObjectType.Shield, gameObject);
    }
}
