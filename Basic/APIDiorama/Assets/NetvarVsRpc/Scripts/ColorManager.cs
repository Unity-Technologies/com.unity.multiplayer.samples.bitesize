using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
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
                return;
            }

            bool playerIsCloseEnough = true; //todo: implement behaviour
            if (playerIsCloseEnough)
            {
                if (Input.GetKeyUp(KeyCode.E))
                {
                    OnClientRequestColorChange();
                }
            }
        }

        void OnClientRequestColorChange()
        {
            OnServerChangeColorServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void OnServerChangeColorServerRpc()
        {
            Color32 newColor = GetRandomColor();
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

        Color32 GetRandomColor() => new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 255);
    }
}
