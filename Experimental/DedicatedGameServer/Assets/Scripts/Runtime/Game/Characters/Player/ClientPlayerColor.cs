using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using Unity.DedicatedGameServerSample.Shared;

namespace Unity.DedicatedGameServerSample.Runtime
{
    class ClientPlayerColor : NetworkBehaviour
    {
        [SerializeField]
        SkinnedMeshRenderer m_Renderer;
        [ColorUsage(false, true)]
        [SerializeField]
        Color[] m_PlayerColors;
        [ColorUsage(false, true)]
        [SerializeField]
        Color[] m_PlayerEmissiveColors;

        [SerializeField]
        VisualTreeAsset m_PlayerNumberAsset;

        VisualElement m_PlayerNumberVisual;

        const string k_CharacterColorKey = "_Character_Color";
        const string k_CharacterEmissiveColorKey = "_Character_Emissive_Color";
        static readonly int k_CharacterColor = Shader.PropertyToID(k_CharacterColorKey);
        static readonly int k_CharacterEmissiveColor = Shader.PropertyToID(k_CharacterEmissiveColorKey);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            var playerNumber = Convert.ToInt32(OwnerClientId.ToString());
            var playerColorIndex = playerNumber % m_PlayerColors.Length;

            foreach (var material in m_Renderer.materials)
            {
                material.SetColor(k_CharacterColor, m_PlayerColors[playerColorIndex]);
                material.SetColor(k_CharacterEmissiveColor, m_PlayerEmissiveColors[playerColorIndex]);
            }

            m_PlayerNumberVisual = m_PlayerNumberAsset.CloneTree().GetFirstChild();
            var label = m_PlayerNumberVisual.Q<Label>("Label");
            label.text = playerNumber < 10 ? "0" + playerNumber : playerNumber.ToString();
            var playerColor = m_PlayerColors[playerColorIndex];
            playerColor.a = 1f;
            label.style.color = new StyleColor(playerColor);
            WorldSpaceView.Instance.AddUIElement(m_PlayerNumberVisual, transform);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            WorldSpaceView.Instance.RemoveUIElement(transform);
        }
    }
}
