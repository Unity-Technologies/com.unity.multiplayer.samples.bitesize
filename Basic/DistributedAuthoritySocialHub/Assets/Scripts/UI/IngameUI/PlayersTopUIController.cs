using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// UI controller which displays nameplate and mic icon on top of each connected player.
    /// </summary>
    class PlayersTopUIController : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_NameplateAsset;

        [SerializeField]
        float m_PanelMinSize = 0.8f;

        [SerializeField]
        float m_PanelMaxSize = 1.1f;

        [SerializeField]
        float m_DisplayYOffset = 1.3f;

        [SerializeField]
        Camera m_Camera;

        List<PlayerHeadDisplay> m_PlayerHeadDisplayPool = new();

        Dictionary<GameObject, PlayerHeadDisplay> m_PlayerToPlayerDisplayDict = new();

        VisualElement m_Root;

        const int k_PoolSize = 12;

        void OnEnable()
        {
            m_PlayerHeadDisplayPool = new List<PlayerHeadDisplay>();
            for (var i = 0; i < k_PoolSize; i++)
            {
                m_PlayerHeadDisplayPool.Add(new PlayerHeadDisplay(m_NameplateAsset));
            }

            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("player-top-display-container");

            GameplayEventHandler.OnParticipantJoinedVoiceChat -= AttachVivoxParticipant;
            GameplayEventHandler.OnParticipantJoinedVoiceChat += AttachVivoxParticipant;

            GameplayEventHandler.OnParticipantLeftVoiceChat -= RemoveVivoxParticipant;
            GameplayEventHandler.OnParticipantLeftVoiceChat += RemoveVivoxParticipant;
        }

        void Update()
        {
            foreach (var playerPair in m_PlayerToPlayerDisplayDict)
            {
                UpdateDisplayPosition(playerPair.Key.transform, playerPair.Value);
            }
        }

        internal void AddOrUpdatePlayer(GameObject player, string playerName, string playerId)
        {
            // if player has already been added, update values and return
            if (m_PlayerToPlayerDisplayDict.TryGetValue(player, out var playerHeadDisplay))
            {
                playerHeadDisplay.SetPlayerName(playerName);
                playerHeadDisplay.PlayerId = playerId;
                return;
            }

            var display = GetDisplayForPlayer();
            display.SetPlayerName(playerName);
            display.PlayerId = playerId;

            UpdateDisplayPosition(player.transform, display);
            m_PlayerToPlayerDisplayDict.Add(player, display);
        }

        internal void RemovePlayer(GameObject player)
        {
            var display = m_PlayerToPlayerDisplayDict[player];
            display.RemoveFromHierarchy();
            m_PlayerHeadDisplayPool.Add(display);
            m_PlayerToPlayerDisplayDict.Remove(player);
        }

        PlayerHeadDisplay GetDisplayForPlayer()
        {
            if (m_PlayerHeadDisplayPool.Count > 0)
            {
                var display = m_PlayerHeadDisplayPool[0];
                m_PlayerHeadDisplayPool.RemoveAt(0);
                m_Root.Add(display);
                return display;
            }

            var newDisplay = new PlayerHeadDisplay(m_NameplateAsset);
            m_Root.Add(newDisplay);
            return newDisplay;
        }

        void UpdateDisplayPosition(Transform playerTransform, VisualElement headDisplay)
        {
            headDisplay.TranslateVEWorldToScreenspace(m_Camera, playerTransform, m_DisplayYOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, playerTransform.position);
            var mappedScale = Mathf.Lerp(m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp(5, 20, distance));
            headDisplay.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }

        void OnDisable()
        {
            foreach (var display in m_PlayerToPlayerDisplayDict.Values)
            {
                display.RemoveFromHierarchy();
                display.RemoveVivoxParticipant();
            }
            m_PlayerHeadDisplayPool.Clear();
            m_PlayerToPlayerDisplayDict.Clear();

            GameplayEventHandler.OnParticipantJoinedVoiceChat -= AttachVivoxParticipant;
            GameplayEventHandler.OnParticipantLeftVoiceChat -= RemoveVivoxParticipant;
        }

        void AttachVivoxParticipant(VivoxParticipant vivoxParticipant)
        {
            foreach (var headDisplay in m_PlayerToPlayerDisplayDict.Values)
            {
                if(headDisplay.PlayerId == vivoxParticipant.PlayerId)
                {
                    headDisplay.AttachVivoxParticipant(vivoxParticipant);
                    return;
                }
            }

            Debug.LogWarning("Could not find player avatar to attach vivox user.");
        }

        void RemoveVivoxParticipant(VivoxParticipant vivoxParticipant)
        {
            foreach (var headDisplay in m_PlayerToPlayerDisplayDict.Values)
            {
                if(headDisplay.VivoxParticipant != null && headDisplay.VivoxParticipant== vivoxParticipant)
                {
                    headDisplay.RemoveVivoxParticipant();
                    return;
                }
            }

            Debug.LogWarning("Could not find player avatar display to remove vivox participant");
        }
    }
}
