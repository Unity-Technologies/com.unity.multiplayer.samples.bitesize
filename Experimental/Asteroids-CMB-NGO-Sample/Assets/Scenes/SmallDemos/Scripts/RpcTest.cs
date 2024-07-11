using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SendToNotAuthorityRpc();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                TakeOwnershipRpc();
            }
        }

    }

    [Rpc(SendTo.NotAuthority)]
    private void SendToNotAuthorityRpc(RpcParams rpcParams = default)
    {
        NetworkManagerHelper.Instance.LogMessage($"[Client-{NetworkManager.LocalClientId}] Received NotAuthority Rpc from Client-{rpcParams.Receive.SenderClientId}!");
    }

    [Rpc(SendTo.Authority)]
    private void TakeOwnershipRpc(RpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);

        NetworkManagerHelper.Instance.LogMessage($"[Client-{OwnerClientId}] Now owns {gameObject.name}!");
    }

    public override void OnGainedOwnership()
    {
        if (OwnerClientId == NetworkManager.LocalClientId)
        {
            NetworkManagerHelper.Instance.LogMessage($"[Client-{NetworkManager.LocalClientId}] Now owns {gameObject.name}!");
        }
        base.OnGainedOwnership();
    }
}
