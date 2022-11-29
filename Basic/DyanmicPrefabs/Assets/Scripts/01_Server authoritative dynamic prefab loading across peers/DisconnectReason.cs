using System;

namespace Game
{
    public enum DisconnectReason
    {
        Undefined,
        ClientNeedsToPreload,     //client needs to preload the dynamic prefabs before connecting
    }
}
