using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Manages the color of a Networked object
    /// </summary>
    public class ColorManager : NetworkBehaviour
    {

        NetworkVariable<Color32> m_NetworkedColor = new NetworkVariable<Color32>();
        Material m_Material;
        ProximityChecker m_ProximityChecker;

        void Awake()
        {
            m_Material = GetComponent<Renderer>().material;
            m_ProximityChecker = GetComponent<ProximityChecker>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                OnClientColorChanged(m_Material.color, m_NetworkedColor.Value);
                m_NetworkedColor.OnValueChanged += OnClientColorChanged;
                m_ProximityChecker.AddListener(OnClientLocalPlayerProximityStatusChanged);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsClient)
            {
                m_NetworkedColor.OnValueChanged -= OnClientColorChanged;
                m_ProximityChecker.RemoveListener(OnClientLocalPlayerProximityStatusChanged);
            }
        }

        void Update()
        {
            if (!IsClient || !m_ProximityChecker.LocalPlayerIsClose)
            {
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

        [ServerRpc(RequireOwnership = false)]
        void OnServerChangeColorServerRpc()
        {
            m_NetworkedColor.Value = DioramaUtilities.GetRandomColor();
        }

        void OnClientColorChanged(Color32 previousColor, Color32 newColor)
        {
            m_Material.color = newColor;
        }

        void OnClientLocalPlayerProximityStatusChanged(bool isClose)
        {
            Debug.Log($"Local player is now {(isClose ? "close" : "far")}");
        }
    }
}
