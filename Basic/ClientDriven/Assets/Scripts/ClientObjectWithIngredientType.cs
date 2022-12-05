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
        
        [SerializeField]
        Renderer m_ColorMesh;

        void Awake()
        {
            m_Server = GetComponent<ServerObjectWithIngredientType>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            enabled = IsClient;
        }

        void UpdateMaterial()
        {
            switch (m_Server.currentIngredientType.Value)
            {
                case IngredientType.Blue:
                    m_ColorMesh.material = m_BlueMaterial;
                    break;
                case IngredientType.Red:
                    m_ColorMesh.material = m_RedMaterial;
                    break;
                case IngredientType.Purple:
                    m_ColorMesh.material = m_PurpleMaterial;
                    break;
            }
        }

        protected void Update()
        {
            UpdateMaterial(); // this is not performant to be called every update, don't do this.
        }
    }
}