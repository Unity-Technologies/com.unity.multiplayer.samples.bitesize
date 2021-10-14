using UnityEngine;

namespace Unity.Netcode.Samples
{
    public class ClientObjectWithIngredientType : ClientServerNetworkBehaviour
    {
        protected override bool ClientOnly => true;

        [SerializeField]
        private Material m_PurpleMaterial;

        [SerializeField]
        private Material m_BlueMaterial;

        [SerializeField]
        private Material m_RedMaterial;

        private ServerObjectWithIngredientType m_Server;

        private Material Material
        {
            get { return GetComponent<Renderer>().material; }
            set { GetComponent<Renderer>().material = value; }
        }

        private void Awake()
        {
            m_Server = GetComponent<ServerObjectWithIngredientType>();
        }

        void UpdateMaterial()
        {
            switch (m_Server.CurrentIngredientType.Value)
            {
                case IngredientType.blue:
                    Material = m_BlueMaterial;
                    break;
                case IngredientType.red:
                    Material = m_RedMaterial;
                    break;
                case IngredientType.purple:
                    Material = m_PurpleMaterial;
                    break;
            }
        }

        protected void Update()
        {
            UpdateMaterial();
        }
    }
}