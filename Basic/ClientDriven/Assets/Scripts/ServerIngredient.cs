using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ServerIngredient : ServerObjectWithIngredientType
{
    [ServerRpc(RequireOwnership = false)]
    public void DespawnServerIngedientServerRpc()
    {
        Consumed();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CookIngedientServerRpc(IngredientType ingredient, Vector3 position, float time)
    {
        OnCookIngredient(ingredient, position, time);
    }

    public void OnCookIngredient(IngredientType ingredient, Vector3 position, float time)
    {
        StartCoroutine(CookIngedient(ingredient, position, time));
    }

    private IEnumerator CookIngedient(IngredientType ingredient, Vector3 position, float time)
    {
        var rigibody = GetComponent<Rigidbody>();
        rigibody.isKinematic = true;
        transform.position = position;
        yield return new WaitForSeconds(time);

        currentIngredientType.Value = ingredient;
        GetComponent<Rigidbody>().isKinematic = false;
    }
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
    public IngredientType IngredientType;

    [HideInInspector]
    public NetworkVariable<IngredientType> currentIngredientType = new Unity.Netcode.NetworkVariable<IngredientType>(default, Unity.Netcode.NetworkVariableReadPermission.Everyone, Unity.Netcode.NetworkVariableWritePermission.Owner);

    public event Action ingredientDespawned;

    [HideInInspector]
    public bool Consuming;

    private Vector3 m_OriginalScale;

    [SerializeField]
    private GameObject m_IngredientVisuals;

    private void Awake()
    {
        m_OriginalScale = transform.localScale;
    }

    protected virtual bool ShouldAutoAdjustScale()
    {
        return true;
    }

    protected virtual bool ShouldDisable()
    {
        return (!IsServer && !NetworkManager.DistributedAuthorityMode) || (!IsOwner && NetworkManager.DistributedAuthorityMode);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            currentIngredientType.Value = IngredientType;
        }
        Consuming = false;
        base.OnNetworkSpawn();
#if NGO_DAMODE
        if (ShouldDisable())
        {
            enabled = false;
            return;
        }
#else
        if (!IsServer)
        {
            enabled = false;
            return;
        }
#endif
    }

    public void Consumed()
    {
        if (m_IngredientVisuals != null && m_IngredientVisuals.transform.parent != transform)
        {
            m_IngredientVisuals.transform.parent = transform;
        }
        NetworkObject.Despawn(destroy: true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner && m_IngredientVisuals != null && m_IngredientVisuals.transform.parent != transform)
        {
            m_IngredientVisuals.transform.parent = transform;
        }
        ingredientDespawned?.Invoke();
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {

        if (!IsSpawned || !ShouldAutoAdjustScale())
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
                m_IngredientVisuals.transform.localScale = m_OriginalScale;
            }
        }

        base.OnNetworkObjectParentChanged(parentNetworkObject);
    }
}
