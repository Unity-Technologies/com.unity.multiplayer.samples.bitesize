using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

public class ServerScoreReplicator : SamNetworkBehaviour
{
    // [SerializeField]
    // private ScoreTracker m_Score;

    private NetworkVariable<int> m_ReplicatedScore = new NetworkVariable<int>();

    public int Score
    {
        get { return m_ReplicatedScore.Value;}
        set { m_ReplicatedScore.Value = value; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // m_Score.replicator = m_ReplicatedScore;
        // m_Score.Score = 0;
    }
}
