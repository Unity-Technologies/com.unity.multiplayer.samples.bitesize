using Unity.Netcode;

public class ServerScoreReplicator : NetworkBehaviour
{

    public static ServerScoreReplicator Instance { get; private set; }

    NetworkVariable<int> m_ReplicatedScore = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> ReplicatedScore => m_ReplicatedScore;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    public void AddScorePoint()
    {
        if (IsOwner) 
        { 
            
        }
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
