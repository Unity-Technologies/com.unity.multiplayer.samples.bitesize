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

            UpdateMaterial(default(IngredientType), m_Server.currentIngredientType.Value);
            m_Server.currentIngredientType.OnValueChanged += UpdateMaterial;
        }

        public override void OnNetworkDespawn()
        {
            m_Server.currentIngredientType.OnValueChanged -= UpdateMaterial;
        }

        void UpdateMaterial(IngredientType previousValue, IngredientType newValue)
        {
            switch (newValue)
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
    }
}
