using System;
using Unity.Netcode;
using UnityEngine;

public class ServerIngredient : ServerObjectWithIngredientType
{
}

public enum IngredientType
{
    Red,
    Blue,
    Purple,
    MAX // should be always last
}

public class ServerObjectWithIngredientType : NetworkBehaviour
{
    public NetworkVariable<IngredientType> currentIngredientType;

    public event Action ingredientDespawned;

    private Vector3 m_OriginalScale;

    [SerializeField]
    private GameObject m_IngredientVisuals;

    private void Awake()
    {
        m_OriginalScale = transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (m_IngredientVisuals != null && m_IngredientVisuals.transform.parent != transform)
        {
            m_IngredientVisuals.transform.parent = transform;
        }

        ingredientDespawned?.Invoke();
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {

        if (!IsSpawned)
        {
            return;
        }

        var serverPlayerMove = parentNetworkObject == null ? (ServerPlayerMove)null : parentNetworkObject.GetComponent<ServerPlayerMove>();

        if (m_IngredientVisuals != null)
        {
            if (parentNetworkObject != null)
            {
                
                if (serverPlayerMove != null && serverPlayerMove.IngredientHoldPosition != null)
                {
                    m_IngredientVisuals.transform.SetParent(serverPlayerMove.IngredientHoldPosition.transform, false);
                    m_IngredientVisuals.transform.localScale = Vector3.one * 0.333f;
                }
            }
            else
            {
                m_IngredientVisuals.transform.SetParent(transform, false);
                m_IngredientVisuals.transform.localScale = Vector3.one;
            }
        }

        base.OnNetworkObjectParentChanged(parentNetworkObject);
    }
}
