using System;

namespace Game
{
    /// <summary>
    /// Enum describing the reason a client was disconnected from the server. For the purposes of this sample, it will
    /// be used mostly at connection request time, when the client needs to preload dynamic prefabs. 
    /// </summary>
    public enum DisconnectReason
    {
        Undefined,
        ClientNeedsToPreload,     //client needs to preload the dynamic prefabs before connecting
    }
}
