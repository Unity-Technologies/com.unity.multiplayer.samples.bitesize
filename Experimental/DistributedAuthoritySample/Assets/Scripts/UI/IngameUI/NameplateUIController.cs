using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// UI controller which displays nameplates over player's heads in screen space.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class NameplateUIController : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_NameplateAsset;

        [SerializeField]
        Vector2 m_NameplateScale;

        [SerializeField]
        float m_PanelMinSize = 0.8f;

        [SerializeField]
        float m_PanelMaxSize = 1.1f;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        List<Transform> m_TrackingTarget = new();

        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private List<VisualElement> m_Nameplates = new();

        private void OnEnable()
        {
           // NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            m_Nameplates = new List<VisualElement>();
            m_UIDocument = GetComponent<UIDocument>();
            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("NameTags");


            // foreach (var target in m_TrackingTarget)
            // {
            //     VisualElement nameplate = m_NameplateAsset.Instantiate().Children().First();
            //     SetPlayerNameLabel(nameplate, target.gameObject.name);
            //     m_Nameplates.Add(nameplate);
            //     m_Root.Add(nameplate);
            // }
        }

        void OnConnectionEvent(NetworkManager manager, ConnectionEventData evt)
        {
            switch (evt.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    OnClientConnected(evt.ClientId);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    OnClientDisconnected(evt.ClientId);
                    break;
            }
        }

        void SetPlayerNameLabel(VisualElement namePlate, string name)
        {
            namePlate.Q<Label>().text = name;
        }

        void OnClientConnected(ulong clientId)
        {
            Debug.Log("Client with ID" +clientId +"joined---- new count is" + NetworkManager.Singleton.ConnectedClients.Count);
        }

        void OnClientDisconnected(ulong clientId)
        {
            Debug.Log("Client with ID" +clientId +"left---- new count is" + NetworkManager.Singleton.ConnectedClients.Count);
        }

        void HandleNameTag(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                var playerUIAnchor = networkClient.PlayerObject.gameObject.transform.Find("UI_Anchor");

                if (playerUIAnchor == null)
                {
                    Debug.Log("Did not find UI_Anchor on player object cannot attach UI.");
                    return;
                }

                if (!m_TrackingTarget.Contains(playerUIAnchor))
                {
                    m_TrackingTarget.Add(playerUIAnchor);
                    VisualElement nameplate = m_NameplateAsset.Instantiate().Children().First();
                    SetPlayerNameLabel(nameplate, networkClient.PlayerObject.gameObject.name);
                    m_Nameplates.Add(nameplate);
                    m_Root.Add(nameplate);
                }
            }
        }


        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }

            foreach (var nameplate in m_Nameplates)
            {
                m_Root.Remove(nameplate);
            }

            m_Nameplates.Clear();
        }

        private void Update()
        {
            foreach (var connectedClient in NetworkManager.Singleton.ConnectedClients)
            {
                HandleNameTag(connectedClient.Value.ClientId);
            }

            UpdateNameplatePositions();
        }

        private void UpdateNameplatePositions()
        {
            for (var i = 0; i < m_Nameplates.Count; i++)
            {
                if (m_TrackingTarget[i] == null)
                {
                    continue;
                }

                // Get position of nameplate in screen space.
                Vector2 screenSpacePosition = m_Camera.WorldToScreenPoint(m_TrackingTarget[i].position);
                var distance = Vector3.Distance(m_Camera.transform.position, m_TrackingTarget[i].position);
                Vector2 panelSpacePosition = RuntimePanelUtils.ScreenToPanel(m_Root.panel, new Vector2(screenSpacePosition.x, Screen.height - screenSpacePosition.y));

                var mappedScale  = Mathf.Lerp (m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp (5, 10, distance));

                m_Nameplates[i].style.left = Mathf.Round(panelSpacePosition.x);
                m_Nameplates[i].style.top = Mathf.Round(panelSpacePosition.y);
                m_Nameplates[i].style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
            }
        }
    }
}
