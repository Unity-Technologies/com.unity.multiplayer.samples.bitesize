using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// UI controller which displays nameplate and mic icon on top of each connected player.
    /// </summary>
    public class PlayersTopUIController : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_NameplateAsset;

        [SerializeField]
        Vector2 m_NameplateScale;

        [SerializeField]
        float m_PanelMinSize = 0.8f;

        [SerializeField]
        float m_PanelMaxSize = 1.1f;

        [SerializeField]
        float m_DisplayYOffset = 1.3f;

        [SerializeField]
        Camera m_Camera;

        List<PlayerHeadDisplay> m_PlayerHeadDisplayPool = new();

        List<Tuple<NetworkObject, PlayerHeadDisplay>> m_PlayersToDisplayMap = new();

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
        }

        private void Update()
        {
            if (!NetworkManager.Singleton || NetworkManager.Singleton.SpawnManager == null)
                return;

            foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
            {
                var playerAlreadyExists = false;
                for (var i = 0; i < m_PlayersToDisplayMap.Count; i++)
                {
                    var currentPlayer = m_PlayersToDisplayMap[i].Item1;
                    var correspondingDisplay = m_PlayersToDisplayMap[i].Item2;
                    if (currentPlayer == player)
                    {
                        // Player has already a UI, update it
                        UpdateDisplayPosition(currentPlayer.transform, correspondingDisplay);
                        playerAlreadyExists = true;
                        break;
                    }
                }

                if (playerAlreadyExists)
                    continue;

                // New player found, create a new UI
                var display = GetDisplayForPlayer();
                display.SetPlayerName(player.gameObject.name);
                UpdateDisplayPosition(player.transform, display);
                m_PlayersToDisplayMap.Add(new Tuple<NetworkObject, PlayerHeadDisplay>(player, display));
            }

            ReturnUnusedDisplaysToPool();
        }

        void ReturnUnusedDisplaysToPool()
        {
            //Return unused displays to the pool
            for (var i = 0; i < m_PlayersToDisplayMap.Count; i++)
            {
                if (!m_PlayersToDisplayMap[i].Item1)
                {
                    m_PlayerHeadDisplayPool.Add(m_PlayersToDisplayMap[i].Item2);
                    m_PlayersToDisplayMap[i].Item2.RemoveFromHierarchy();
                    m_PlayersToDisplayMap.RemoveAt(i);
                }
            }
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
            UIUtils.TranslateVEWorldToScreenspace(m_Camera, playerTransform, headDisplay, m_DisplayYOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, playerTransform.position);
            var mappedScale = Mathf.Lerp(m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp(5, 20, distance));
            headDisplay.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }
    }
}
