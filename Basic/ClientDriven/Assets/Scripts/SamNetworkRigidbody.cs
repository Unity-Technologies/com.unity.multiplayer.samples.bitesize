using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class SamNetworkRigidbody : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // GetComponent<Collider>().enabled = IsServer;
    }
}
