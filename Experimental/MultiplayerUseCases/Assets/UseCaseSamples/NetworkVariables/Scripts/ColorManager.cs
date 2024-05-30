using Unity.Netcode.Samples.MultiplayerUseCases.Common;
using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.NetworkVariables
{
    /// <summary>
    /// Manages the color of a Networked object
    /// </summary>
    public class ColorManager : NetworkBehaviour
    {
        /// <summary>
        /// The NetworkVariable holding the color
        /// </summary>
        NetworkVariable<Color32> m_NetworkedColor = new NetworkVariable<Color32>();
        Material m_Material;

        [SerializeField, Tooltip("The seconds that will elapse between color changes")]
        float m_SecondsBetweenColorChanges;
        float m_ElapsedSecondsSinceLastChange;

        void Awake()
        {
            m_Material = GetComponent<Renderer>().material;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                /*
                 * We call the color change method manually when we connect to ensure that our color is correctly initialized. 
                 * This is helpful for when a client joins mid-game and needs to catch up with the current state of the game.
                 */
                OnClientColorChanged(m_Material.color, m_NetworkedColor.Value);
                m_NetworkedColor.OnValueChanged += OnClientColorChanged; //this will be called on the client whenever the value is changed by the server
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsClient)
            {
                m_NetworkedColor.OnValueChanged -= OnClientColorChanged;
            }
        }

        void Update()
        {
            if (!IsSpawned)
            {
                //the player disconnected
                return;
            }
            if (!IsServer)
            {
                /*
                 * By default, only the server is allowed to change the value of NetworkVariables. 
                 * This can be changed through the NetworkVariable's constructor.
                 */
                return;
            }

            m_ElapsedSecondsSinceLastChange += Time.deltaTime;

            if (m_ElapsedSecondsSinceLastChange >= m_SecondsBetweenColorChanges)
            {
                m_ElapsedSecondsSinceLastChange = 0;
                OnServerChangeColor();
            }
        }

        void OnServerChangeColor()
        {
            m_NetworkedColor.Value = MultiplayerUseCasesUtilities.GetRandomColor();
        }

        void OnClientColorChanged(Color32 previousColor, Color32 newColor)
        {
            m_Material.color = newColor;
        }
    }
}
