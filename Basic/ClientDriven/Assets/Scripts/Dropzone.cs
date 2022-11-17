using UnityEngine;

public class Dropzone : ServerObjectWithIngredientType
{
    [SerializeField]
    ServerScoreReplicator m_ScoreTracker;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        var ingredient = other.gameObject.GetComponent<ServerIngredient>();
        if (ingredient == null)
        {
            return;
        }

        if (ingredient.CurrentIngredientType.Value != CurrentIngredientType.Value)
        {
            return;
        }

        m_ScoreTracker.Score += 1;
        ingredient.NetworkObject.Despawn(destroy: true);
    }
}
