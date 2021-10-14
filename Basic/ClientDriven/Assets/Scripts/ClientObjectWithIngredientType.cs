using UnityEngine;

namespace Unity.Netcode.Samples
{

    public class ClientObjectWithIngredientType : SamNetworkBehaviour
    {
        protected override bool ClientOnly { get; } = true;

        [SerializeField]
        private Material purpleMaterial;

        [SerializeField]
        private Material blueMaterial;

        [SerializeField]
        private Material redMaterial;

        private ServerObjectWithIngredientType m_Server;

        private Material m_Material
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
                    m_Material = blueMaterial;
                    break;
                case IngredientType.red:
                    m_Material = redMaterial;
                    break;
                case IngredientType.purple:
                    m_Material = purpleMaterial;
                    break;
            }
        }

        protected virtual void Update()
        {
            UpdateMaterial();
        }
    }
}