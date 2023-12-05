using System;
using UnityEngine;

public class Dropzone : ServerObjectWithIngredientType
{
    [SerializeField]
    ServerScoreReplicator m_ScoreTracker;

    [SerializeField]
    Animator m_Animator;

    [SerializeField]
    float m_AnimationOffset;

    void Start()
    {
        m_Animator.SetFloat("Offset", m_AnimationOffset);
    }

    void OnTriggerEnter(Collider other)
    {
#if NGO_DAMODE
        if ((NetworkManager.DistributedAuthorityMode && !IsOwner) || (!NetworkManager.DistributedAuthorityMode && !IsServer))
        {
            return;
        }
#else
        if (!IsServer) return;
#endif

        var ingredient = other.gameObject.GetComponent<ServerIngredient>();
        if (ingredient == null)
        {
            return;
        }

        if (ingredient.currentIngredientType.Value != currentIngredientType.Value)
        {
            return;
        }

        m_ScoreTracker.Score += 1;
        if (ingredient.NetworkObject.IsOwner)
        {
            ingredient.NetworkObject.Despawn(destroy: true);
        }
        else
        {
            ingredient.DespawnServerIngedientServerRpc();
        }
        
    }


}
