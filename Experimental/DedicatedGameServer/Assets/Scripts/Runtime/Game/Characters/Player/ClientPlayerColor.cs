using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using Unity.DedicatedGameServerSample.Shared;

namespace Unity.DedicatedGameServerSample.Runtime
{
    class ClientPlayerColor : NetworkBehaviour
    {
        [ColorUsage(false, true)]
        [SerializeField]
        Color[] m_PlayerColors;
        [ColorUsage(false, true)]
        [SerializeField]
        Color[] m_PlayerEmissiveColors;
        int m_PlayerNumber;
        Vector2 m_PlayerNumberVector;

        [SerializeField]
        VisualTreeAsset m_PlayerNumberAsset;

        [SerializeField]
        UIDocument m_PlayerNumberDocument;

        VisualElement m_PlayerNumberVisual;

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

            m_PlayerNumberVisual = m_PlayerNumberAsset.CloneTree().GetFirstChild();
            WorldSpaceUIHandler.Instance.AddUIElement(m_PlayerNumberVisual, transform);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            WorldSpaceUIHandler.Instance.RemoveUIElement(transform);
        }
    }
}
