using Unity.Netcode;

public class ServerScoreReplicator : NetworkBehaviour
{

    public static ServerScoreReplicator Instance { get; private set; }

    NetworkVariable<int> m_ReplicatedScore = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> ReplicatedScore => m_ReplicatedScore;
    public override void OnNetworkSpawn()
    {
        Instance = this;
        base.OnNetworkSpawn();

#if !NGO_DAMODE
        if (!IsServer)
        {
            enabled = false;
            return;
        }
#endif
    }

    public int Score
    {
        get => m_ReplicatedScore.Value;
        set
        {
            if (IsOwner)
            {
                m_ReplicatedScore.Value = value;
            }
            else
            {
                UpdateScoreServerRpc(value);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateScoreServerRpc(int score)
    {
        Score = score;
    }
}
