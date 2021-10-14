using UnityEngine;

public class Dropzone : ServerObjectWithIngredientType
{
    [SerializeField]
    private ServerScoreReplicator m_ScoreTracker;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
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
