using System;
using System.Collections;
using System.Collections.Generic;
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
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        // ChangeScoreOnTrigger(other, 1);
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
        ingredient.NetworkObject.Despawn(destroy:true);
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (!enabled) return;
    //     ChangeScoreOnTrigger(other, -1);
    // }

    void ChangeScoreOnTrigger(Collider other, int amount)
    {

    }
}
