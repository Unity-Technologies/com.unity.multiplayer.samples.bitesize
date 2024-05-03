using System;
using UnityEngine;
using Unity.Netcode;

namespace Unity.DedicatedGameServerSample.Runtime
{
    class ClientPlayerColor : NetworkBehaviour
    {
        [SerializeField]
        Renderer m_PlayerNumberMesh;
        [ColorUsage(false, true)]
        [SerializeField]
        Color[] m_PlayerColors;
        [ColorUsage(false, true)]
        [SerializeField]
        Color[] m_PlayerEmissiveColors;
        [SerializeField]
        int m_PlayerNumber;
        [SerializeField]
        Vector2 m_PlayerNumberVector;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SkinnedMeshRenderer m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            m_PlayerNumber = Convert.ToInt32(OwnerClientId.ToString());
            var playerColorIndex = m_PlayerNumber % m_PlayerColors.Length;

            foreach (var material in m_Renderer.materials)
            {
                material.SetColor("_Character_Color", m_PlayerColors[playerColorIndex]);
                material.SetColor("_Character_Emissive_Color", m_PlayerEmissiveColors[playerColorIndex]);
            }

            if (m_PlayerNumber < 10)
            {
                m_PlayerNumberVector = new Vector2(0, m_PlayerNumber);
            }
            else
            {
                char[] numberCharArray = m_PlayerNumber.ToString().ToCharArray();
                m_PlayerNumberVector = new Vector2(Int32.Parse(numberCharArray[0].ToString()), Int32.Parse(numberCharArray[1].ToString()));
            }

            m_PlayerNumberMesh.material.SetVector("_PlayerNumber", m_PlayerNumberVector);
            m_PlayerNumberMesh.material.SetColor("_PlayerColor", m_PlayerColors[playerColorIndex]);
        }
    }
}
