using Unity.Netcode;

public class ServerScoreReplicator : NetworkBehaviour
{
    NetworkVariable<int> m_ReplicatedScore = new NetworkVariable<int>();

    public NetworkVariable<int> ReplicatedScore => m_ReplicatedScore;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    public int Score
    {
        get => m_ReplicatedScore.Value;
        set => m_ReplicatedScore.Value = value;
    }
}
