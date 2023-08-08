using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class MetagameModel : Model<MetagameApplication>
    {
        internal ClientConnectingModel ClientConnecting => m_ClientConnectingModel;

        [SerializeField]
        ClientConnectingModel m_ClientConnectingModel;
    }
}
