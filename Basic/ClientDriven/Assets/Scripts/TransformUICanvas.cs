using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class TransformUICanvas : NetworkBehaviour
{
    public GameObject InfoPanel;

    private void Start()
    {
        if (InfoPanel != null)
        {
            InfoPanel.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (InfoPanel != null)
        {
            InfoPanel.gameObject.SetActive(true);
        }
        
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (InfoPanel != null)
        {
            InfoPanel.gameObject.SetActive(false);
        }
        base.OnNetworkDespawn();
    }
}
