using com.unity.multiplayer.samples.distributed_authority.gameplay;
using UnityEngine;
using Unity.Netcode;

public class NetworkAvatarManager : MonoBehaviour
{
    public CameraControl cameraControl;

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        if(NetworkManager.Singleton == null)
        {
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Register to network spawn event
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientAvatarSpawned;
        }
    }

    private void OnClientAvatarSpawned(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Find the player's avatar transform once it has been spawned
            AvatarTransform avatarTransform = FindFirstObjectByType<AvatarTransform>();
            if (avatarTransform != null)
            {
                //cameraControl.SetAvatarTransform(avatarTransform);
            }
            else
            {
                Debug.LogError("AvatarTransform not found for the connected client.");
            }

            // Unregister from the event after the avatar has been assigned
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientAvatarSpawned;
        }
    }
}
