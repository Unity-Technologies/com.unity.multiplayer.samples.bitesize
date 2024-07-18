using Unity.Netcode;

public interface ICollisionHandler
{
    /// <summary>
    /// Invoked by non-authority objects detecting a collision, this determines the 
    /// appropriate targeted authority of the object being collided with. If the local
    /// instance is authority it handles collision locally
    /// </summary>
    void SendCollisionMessage(CollisionMessageInfo collisionMessage);

    /// <summary>
    /// Authority instances receive collision messages from non-authority instances via this implemented method
    /// </summary>
    /// <param name="damageMessage"></param>
    /// <param name="rpcParams"></param>
    [Rpc(SendTo.Authority, DeferLocal = true)]
    void HandleCollisionRpc(CollisionMessageInfo collisionMessage, RpcParams rpcParams = default);
}
