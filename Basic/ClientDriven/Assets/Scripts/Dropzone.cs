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

    private void Awake()
    {
#if NGO_DAMODE
        m_ScoreTracker = ServerScoreReplicator.Instance;
#endif
    }

    void Start()
    {
        m_Animator.SetFloat("Offset", m_AnimationOffset);
    }

#if NGO_DAMODE
    public override void OnNetworkSpawn()
    {
        m_ScoreTracker = ServerScoreReplicator.Instance;
        base.OnNetworkSpawn();
    }

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

        if (!m_ScoreTracker)
        {
            m_ScoreTracker = ServerScoreReplicator.Instance;
        }
        m_ScoreTracker.Score += 1;

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
