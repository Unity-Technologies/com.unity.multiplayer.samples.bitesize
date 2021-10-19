using UnityEngine;

namespace Unity.Netcode.Samples
{
    public class ClientObjectWithIngredientType : NetworkBehaviour
    {
        [SerializeField]
        private Material m_PurpleMaterial;

        [SerializeField]
        private Material m_BlueMaterial;

        [SerializeField]
        private Material m_RedMaterial;

        private ServerObjectWithIngredientType m_Server;
        private Renderer m_Renderer;

        private void Awake()
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
                case IngredientType.blue:
                    m_Renderer.material = m_BlueMaterial;
                    break;
                case IngredientType.red:
                    m_Renderer.material = m_RedMaterial;
                    break;
                case IngredientType.purple:
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