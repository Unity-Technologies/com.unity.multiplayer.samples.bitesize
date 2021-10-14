using UnityEngine;

namespace Unity.Netcode.Samples
{
    public class ClientObjectWithIngredientType : ClientServerBaseNetworkBehaviour
    {
        protected override bool ClientOnly => true;

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
            UpdateMaterial(); // don't do this at home kids, this is me being lazy, this shouldn't happen every update...
        }
    }
}