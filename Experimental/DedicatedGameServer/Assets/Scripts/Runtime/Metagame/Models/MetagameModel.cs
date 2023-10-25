using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main Model of the <see cref="MetagameApplication"></see>
    /// </summary>
    public class MetagameModel : Model<MetagameApplication>
    {
        internal ClientConnectingModel ClientConnecting => m_ClientConnectingModel;

        [SerializeField]
        ClientConnectingModel m_ClientConnectingModel;
    }
}
