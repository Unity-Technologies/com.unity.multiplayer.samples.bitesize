using Unity.Netcode;
using Unity.Netcode.Samples;

public class ServerScoreReplicator : NetworkBehaviour
{
    private NetworkVariable<int> m_ReplicatedScore = new NetworkVariable<int>();

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
