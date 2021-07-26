using MLAPI;

public class Explosion : NetworkBehaviour
{
    NetworkObjectPool m_PoolToReturn;

    public void Config(float lifetime, NetworkObjectPool poolToReturn)
    {
        m_PoolToReturn = poolToReturn;

        if (IsServer)
        {
            Invoke(nameof(DestroyObject), lifetime);
        }
    }

    public void DestroyObject()
    {
        if (!NetworkObject.IsSpawned)
        {
            return;
        }

        NetworkObject.Despawn();
        m_PoolToReturn.ReturnNetworkObject(NetworkObject);
    }
}
