using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class ConnectionPayload
    {
        public int hashOfDynamicPrefabGUIDs;
    }
    
    [Serializable]
    public class DisconnectionPayload
    {
        public DisconnectReason reason;
        public List<string> guids;
    }
}
