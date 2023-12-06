using System;
using UnityEngine;

public class Dropzone : ServerObjectWithIngredientType
{
    [SerializeField]
    Animator m_Animator;

    [SerializeField]
    float m_AnimationOffset;

    private void Awake()
    {
    }

    void Start()
    {
        m_Animator.SetFloat("Offset", m_AnimationOffset);
    }

#if NGO_DAMODE
    protected override bool ShouldAutoAdjustScale()
    {
        return false;
    }

    // Only disable in non-distributed authority mode
    protected override bool ShouldDisable()
    {
        return (!IsServer && !NetworkManager.DistributedAuthorityMode);
    }

#endif

    void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned) 
        {
            return;
        }

        if (!NetworkObject.HasAuthority)
        {
            return;
        }

        var ingredient = other.gameObject.GetComponent<ServerIngredient>();
        if (ingredient == null)
        {
            return;
        }

        if (ingredient.currentIngredientType.Value != currentIngredientType.Value)
        {
            return;
        }

        ServerScoreReplicator.Instance.Score += 1;

        if (ingredient.NetworkObject.HasAuthority)
        {
            ingredient.Consumed();
        }
        else
        {
            ingredient.DespawnServerIngedientServerRpc();
        }
    }
}
