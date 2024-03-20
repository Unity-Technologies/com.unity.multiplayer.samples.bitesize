using Unity.Netcode.Samples.MultiplayerUseCases.Common;
using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.NetVarVsRPC
{
    /// <summary>
    /// Manages the color of a Networked object
    /// </summary>
    public class ColorManager : NetworkBehaviour
    {
        [SerializeField]
        bool m_UseNetworkVariableForColor;

        NetworkVariable<Color32> m_NetworkedColor = new NetworkVariable<Color32>();
        Material m_Material;

        void Awake()
        {
            m_Material = GetComponent<Renderer>().material;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                if (m_UseNetworkVariableForColor)
                {
                    /* in this case, you need to manually load the initial Color to catch up with the state of the network variable.
                     * This is particularly useful when re-connecting or hot-joining a session
                    */
                    OnClientColorChanged(m_Material.color, m_NetworkedColor.Value);
                    m_NetworkedColor.OnValueChanged += OnClientColorChanged;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsClient)
            {
                if (m_UseNetworkVariableForColor)
                {
                    m_NetworkedColor.OnValueChanged -= OnClientColorChanged;
                }
            }
        }

        void Update()
        {
            if (!IsClient) 
            {
                /* note: in this case there's only client-side logic and therefore the scripts returns early.
                 * In a real production scenario, you would have an UpdateManager script running all Updates from a centralized point.
                 * An alternative to that is to disable behaviours on client/server depending to what is/is not going to be executed on that instance. */
                return;
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                OnClientRequestColorChange();
            }
        }

        void OnClientRequestColorChange()
        {
            OnServerChangeColorServerRpc();
        }

        [ServerRpc(RequireOwnership = false)] //note: please refer to RPCs documentation to learn more about the pros and cons of the RequireOwnership parameter
        void OnServerChangeColorServerRpc()
        {
            Color32 newColor = MultiplayerUseCasesUtilities.GetRandomColor();
            if (m_UseNetworkVariableForColor)
            {
                m_NetworkedColor.Value = newColor;
                return;
            }
            OnClientNotifyColorChangedClientRpc(newColor);
        }

        [ClientRpc]
        void OnClientNotifyColorChangedClientRpc(Color32 newColor)
        {
            m_Material.color = newColor;
        }

        void OnClientColorChanged(Color32 previousColor, Color32 newColor)
        {
            m_Material.color = newColor;
        }
    }
}
