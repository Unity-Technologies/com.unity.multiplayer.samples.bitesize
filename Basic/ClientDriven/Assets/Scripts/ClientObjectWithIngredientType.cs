using UnityEngine;

namespace Unity.Netcode.Samples
{
    public class ClientObjectWithIngredientType : NetworkBehaviour
    {
        [SerializeField]
        Material m_PurpleMaterial;

        [SerializeField]
        Material m_BlueMaterial;

        [SerializeField]
        Material m_RedMaterial;

        ServerObjectWithIngredientType m_Server;
        
        Renderer m_Renderer;

        void Awake()
        {
            m_Server = GetComponent<ServerObjectWithIngredientType>();
            m_Renderer = GetComponent<Renderer>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            enabled = IsClient;
        }

        void UpdateMaterial()
        {
            switch (m_Server.CurrentIngredientType.Value)
            {
                case IngredientType.Blue:
                    m_Renderer.material = m_BlueMaterial;
                    break;
                case IngredientType.Red:
                    m_Renderer.material = m_RedMaterial;
                    break;
                case IngredientType.Purple:
                    m_Renderer.material = m_PurpleMaterial;
                    break;
            }
        }

        protected void Update()
        {
            UpdateMaterial(); // this is not performant to be called every update, don't do this.
        }
    }
}