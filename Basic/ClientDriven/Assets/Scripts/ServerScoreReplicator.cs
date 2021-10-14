using Unity.Netcode;
using Unity.Netcode.Samples;

public class ServerScoreReplicator : ClientServerBaseNetworkBehaviour
{
    private NetworkVariable<int> m_ReplicatedScore = new NetworkVariable<int>();

    public int Score
    {
        get => m_ReplicatedScore.Value;
        set => m_ReplicatedScore.Value = value;
    }
}
