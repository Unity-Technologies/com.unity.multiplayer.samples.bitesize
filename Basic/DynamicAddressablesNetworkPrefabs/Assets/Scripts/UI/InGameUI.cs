using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class InGameUI : MonoBehaviour
    {
        // UI Documents
        [SerializeField]
        UIDocument m_ConnectionsUIDoc;
        [SerializeField]
        UIDocument m_InGameUIDoc;

        // UI Roots
        VisualElement m_InGameUIRoot;
        VisualElement m_HostUIRoot;

        //UI Elements
        [SerializeField]
        VisualTreeAsset m_ConnectionTemplatePrefab;
        [SerializeField]
        VisualTreeAsset m_RowTemplatePrefab;
        VisualElement m_ConnectionsTemplatesHolder;
        VisualElement m_RowTemplatesHolder;
        VisualElement m_ArtificialDelaySliderHolder;
        VisualElement m_NetworkSpawnTimeoutSliderHolder;
        VisualElement m_ApiControlsHolder;
        Button m_LoadAllAsyncButton;
        Button m_SpawnSynchronouslyButton;
        Button m_SpawnWithVisibilityButton;
        Slider m_ArtificialDelaySlider;
        Slider m_NetworkSpawnTimeoutSlider;
        TextField m_ArtificialLatencyTextField;
        TextField m_NetworkSpawnTimeoutTextFieldLabel;

        // checkboxes to configure UI
        [SerializeField]
        ButtonVisibility m_ShowArtificialDelaySlider;
        [SerializeField]
        ButtonVisibility m_ShowNetworkSpawnTimeoutSlider;
        [SerializeField]
        ButtonVisibility m_ShowApiControls;
        [SerializeField]
        ButtonVisibility m_ShowLoadAllAsyncButton;
        [SerializeField]
        ButtonVisibility m_ShowTrySpawnSynchronouslyButton;
        [SerializeField]
        ButtonVisibility m_ShowSpawnUsingVisibilityButton;

        public int ArtificialDelayMilliseconds { get; private set; } = 1000;
        public float NetworkSpawnTimeoutSeconds { get; private set; } = 3000;

        Dictionary<ulong, ClientUI> m_Clients = new Dictionary<ulong, ClientUI>();

        public event Action LoadAllAsyncButtonPressed;

        public event Action TrySpawnSynchronouslyButtonPressed;

        public event Action SpawnUsingVisibilityButtonPressed;

        [Flags]
        public enum ButtonVisibility
        {
            None = 0,
            Server = 1,
            Client = 2
        }

        // internal enum used for testing
        public enum LoadStatus
        {
            Loaded,
            Loading
        }

        internal struct ClientUI
        {
            TemplateContainer m_ClientInstance;
            VisualTreeAsset m_RowTemplatePrefabReference;
            Dictionary<int, TemplateContainer> m_RowInstances;
            VisualElement m_RowHolder;

            internal void InitializeClient(VisualTreeAsset clientPrefab, VisualTreeAsset rowPrefab, ulong id, VisualElement clientHolder)
            {
                m_ClientInstance = clientPrefab.Instantiate();
                m_ClientInstance.Q<Label>("ClientID").text = id.ToString();
                m_RowInstances = new Dictionary<int, TemplateContainer>();
                m_RowTemplatePrefabReference = rowPrefab;
                clientHolder.Add(m_ClientInstance);
                m_RowHolder = m_ClientInstance.Q<VisualElement>("PrefabsArray");
            }

            internal void SetRow(int prefabHash, string prefabName, LoadStatus loadStatus)
            {
                if (!m_RowInstances.TryGetValue(prefabHash, out var rowTemplateInstance))
                {
                    rowTemplateInstance = m_RowTemplatePrefabReference.Instantiate();
                    m_RowInstances.Add(prefabHash, rowTemplateInstance);
                    m_RowHolder.Add(rowTemplateInstance);
                }

                rowTemplateInstance.Q<Label>("NetObjName").text = prefabName;
                rowTemplateInstance.Q<Label>("NetObjStatus").text = loadStatus.ToString();
            }

            internal void RemoveRow(int prefabHash)
            {
                if (m_RowInstances.ContainsKey(prefabHash))
                {
                    m_RowHolder.Remove(m_RowInstances[prefabHash]);
                }
            }

            internal void RemoveClientUI(VisualElement clientHolder)
            {
                clientHolder.Remove(m_ClientInstance);
            }

            internal void DeleteAllRows()
            {
                m_RowHolder.Clear();
                m_RowInstances.Clear();
            }
        }

        void Awake()
        {
            SetupConnectionsUI();

            // register UI elements to methods using callbacks
            m_LoadAllAsyncButton.clickable.clicked += OnLoadAllPrefabsAsyncPressed;

            m_SpawnSynchronouslyButton.clickable.clicked += OnTrySpawnSynchronouslyPressed;

            m_SpawnWithVisibilityButton.clickable.clicked += OnSpawnUsingVisibilityPressed;

            m_ArtificialDelaySlider.RegisterValueChangedCallback(OnArtificialLatencySliderChanged);
            m_NetworkSpawnTimeoutSlider.RegisterValueChangedCallback(OnNetworkSpawnTimeoutSliderChanged);
            m_ArtificialLatencyTextField.RegisterValueChangedCallback(OnArtificialLatencyInputChanged);
            m_NetworkSpawnTimeoutTextFieldLabel.RegisterValueChangedCallback(OnNetworkSpawnTimeoutInputChanged);
        }

        void OnDestroy()
        {
            // un-register UI elements from methods using callbacks for when they're clicked 
            m_LoadAllAsyncButton.clickable.clicked -= OnLoadAllPrefabsAsyncPressed;
            m_SpawnSynchronouslyButton.clickable.clicked -= OnTrySpawnSynchronouslyPressed;
            m_SpawnWithVisibilityButton.clickable.clicked -= OnSpawnUsingVisibilityPressed;
            m_ArtificialDelaySlider.UnregisterValueChangedCallback(OnArtificialLatencySliderChanged);
            m_NetworkSpawnTimeoutSlider.UnregisterValueChangedCallback(OnNetworkSpawnTimeoutSliderChanged);
            m_ArtificialLatencyTextField.UnregisterValueChangedCallback(OnArtificialLatencyInputChanged);
            m_NetworkSpawnTimeoutTextFieldLabel.UnregisterValueChangedCallback(OnNetworkSpawnTimeoutInputChanged);
        }

        void Start()
        {
            m_ArtificialDelaySlider.value = ArtificialDelayMilliseconds;
            m_NetworkSpawnTimeoutSlider.value = NetworkSpawnTimeoutSeconds;
            m_ArtificialLatencyTextField.value = ArtificialDelayMilliseconds.ToString();
            m_NetworkSpawnTimeoutTextFieldLabel.value = NetworkSpawnTimeoutSeconds.ToString();
        }

        public void Show(ButtonVisibility visibility)
        {
            SetUIElementVisibility(m_InGameUIRoot, true);

            SetUIElementVisibility(m_ArtificialDelaySliderHolder, m_ShowArtificialDelaySlider.HasFlag(visibility));
            SetUIElementVisibility(m_NetworkSpawnTimeoutSliderHolder, m_ShowNetworkSpawnTimeoutSlider.HasFlag(visibility));
            SetUIElementVisibility(m_ApiControlsHolder, m_ShowApiControls.HasFlag(visibility));
            SetUIElementVisibility(m_LoadAllAsyncButton, m_ShowLoadAllAsyncButton.HasFlag(visibility));
            SetUIElementVisibility(m_SpawnSynchronouslyButton, m_ShowTrySpawnSynchronouslyButton.HasFlag(visibility));
            SetUIElementVisibility(m_SpawnWithVisibilityButton, m_ShowSpawnUsingVisibilityButton.HasFlag(visibility));
        }

        public void Hide()
        {
            SetUIElementVisibility(m_InGameUIRoot, false);
        }

        public void DisconnectRequested()
        {
            ResetInGameUI();
            Hide();
        }

        ClientUI GetClientUI(ulong clientId)
        {
            if (!m_Clients.TryGetValue(clientId, out ClientUI clientUI))
            {
                clientUI = new ClientUI();
                clientUI.InitializeClient(m_ConnectionTemplatePrefab, m_RowTemplatePrefab, clientId, m_ConnectionsTemplatesHolder);
                m_Clients.Add(clientId, clientUI);
            }

            return clientUI;
        }

        public void ClientLoadedPrefabStatusChanged(ulong clientId, int prefabHash, string prefabName, LoadStatus loadStatus)
        {
            var clientUI = GetClientUI(clientId);
            clientUI.SetRow(prefabHash, prefabName, loadStatus);
        }

        public void AddConnectionUIInstance(ulong clientID, int[] prefabHashes, string[] prefabNames)
        {
            var clientUI = GetClientUI(clientID);
            for (int i = 0; i < prefabHashes.Length; i++)
            {
                clientUI.SetRow(prefabHashes[i], prefabNames[i], LoadStatus.Loaded);
            }
        }

        public void RemoveConnectionUIInstance(ulong clientID)
        {
            var clientUI = GetClientUI(clientID);
            if (m_Clients.ContainsKey(clientID))
            {
                clientUI.RemoveClientUI(m_ConnectionsTemplatesHolder);
                m_Clients.Remove(clientID);
            }
        }

        void RemoveConnectionUIRow(ulong clientID, int prefabHash)
        {
            var clientUI = GetClientUI(clientID);
            if (m_Clients.ContainsKey(clientID))
            {
                clientUI.RemoveRow(prefabHash);
            }
        }

        void OnLoadAllPrefabsAsyncPressed()
        {
            Debug.Log("Load all prefabs async button clicked");
            LoadAllAsyncButtonPressed?.Invoke();
        }

        void OnTrySpawnSynchronouslyPressed()
        {
            Debug.Log("Try spawn synchronously button clicked");
            TrySpawnSynchronouslyButtonPressed?.Invoke();
        }

        void OnSpawnUsingVisibilityPressed()
        {
            Debug.Log("Spawn using server visibility button clicked");
            SpawnUsingVisibilityButtonPressed?.Invoke();
        }

        void OnArtificialLatencySliderChanged(ChangeEvent<float> changeEvent)
        {
            ArtificialDelayMilliseconds = Mathf.RoundToInt(changeEvent.newValue);
            m_ArtificialLatencyTextField.value = ArtificialDelayMilliseconds.ToString();
        }

        void OnNetworkSpawnTimeoutSliderChanged(ChangeEvent<float> changeEvent)
        {
            NetworkSpawnTimeoutSeconds = changeEvent.newValue;
            m_NetworkSpawnTimeoutTextFieldLabel.value = NetworkSpawnTimeoutSeconds.ToString();
        }

        void OnArtificialLatencyInputChanged(ChangeEvent<string> changeEvent)
        {
            ArtificialDelayMilliseconds = int.Parse(changeEvent.newValue);
            Math.Clamp(ArtificialDelayMilliseconds, 0, 9999);
            m_ArtificialDelaySlider.value = ArtificialDelayMilliseconds;
        }

        void OnNetworkSpawnTimeoutInputChanged(ChangeEvent<string> changeEvent)
        {
            NetworkSpawnTimeoutSeconds = int.Parse(changeEvent.newValue);
            Math.Clamp(NetworkSpawnTimeoutSeconds, 0, 99999);
            m_NetworkSpawnTimeoutSlider.value = NetworkSpawnTimeoutSeconds;
        }

        void SetUIElementVisibility(VisualElement visualElement, bool isVisible)
        {
            visualElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ResetInGameUI()
        {
            foreach (var client in m_Clients)
            {
                client.Value.DeleteAllRows();
            }
            m_ConnectionsTemplatesHolder.Clear();
            m_Clients.Clear();
        }

        void SetupConnectionsUI()
        {
            m_HostUIRoot = m_ConnectionsUIDoc.rootVisualElement;
            m_InGameUIRoot = m_InGameUIDoc.rootVisualElement;
            m_ConnectionsTemplatesHolder = m_HostUIRoot.Q<VisualElement>("ConnectionsHolder");
            m_ArtificialDelaySliderHolder = m_InGameUIRoot.Q<VisualElement>("ArtificialDelaySliderHolder");
            m_NetworkSpawnTimeoutSliderHolder = m_InGameUIRoot.Q<VisualElement>("SpawnTimeoutSliderHolder");
            m_ApiControlsHolder = m_InGameUIRoot.Q<VisualElement>("ButtonHolder");
            m_LoadAllAsyncButton = m_InGameUIRoot.Q<Button>("LoadAsync");
            m_SpawnSynchronouslyButton = m_InGameUIRoot.Q<Button>("SpawnSync");
            m_SpawnWithVisibilityButton = m_InGameUIRoot.Q<Button>("SpawnInvisible");
            m_ArtificialDelaySlider = m_InGameUIRoot.Q<Slider>("SliderArtificialDelay");
            m_NetworkSpawnTimeoutSlider = m_InGameUIRoot.Q<Slider>("SliderSpawnTimeout");
            m_ArtificialLatencyTextField = m_InGameUIRoot.Q<TextField>("ArtificialDelayValue");
            m_NetworkSpawnTimeoutTextFieldLabel = m_InGameUIRoot.Q<TextField>("NetworkTimeoutValue");
        }
    }
}
