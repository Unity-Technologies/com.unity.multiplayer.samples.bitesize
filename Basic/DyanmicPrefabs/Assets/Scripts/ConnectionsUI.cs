using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ConnectionsUI : MonoBehaviour
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
    Button m_ButtonLoadAllAsync;
    Button m_ButtonSpawnSynchronously;
    Button m_ButtonSpawnWithVisibility;
    Button m_ButtonResetScene;
    Slider m_SliderArtificialLatency;
    Slider m_SliderNetworkSpawnTimeout;
    TextField m_TextFieldArtificialLatency;
    TextField m_TextFieldLabelNetworkSpawnTimeout;

    // checkboxes to configure UI
    [SerializeField]
    bool m_ShowLoadAllAsyncButton;
    [SerializeField]
    bool m_ShowTrySpawnSynchronouslyButton;
    [SerializeField]
    bool m_ShowSpawnUsingVisibilityButton;

    float m_ArtificialLatency;
    float m_NetworkSpawnTimeout;

    Dictionary<ulong, ClientUI> m_Clients = new Dictionary<ulong, ClientUI>();

    // internal enum used for testing
    internal enum LoadStatus
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

        internal TemplateContainer GetClientTemplateInstance()
        {
            return m_ClientInstance;
        }
    }

    void Awake()
    {
        SetupConnectionsUI();

        // register buttons to methods using callbacks for when they're clicked 
        m_ButtonResetScene.clickable.clicked += ResetScene;

        if (m_ShowLoadAllAsyncButton)
        {
            m_ButtonLoadAllAsync.clickable.clicked += LoadAllPrefabsAsync;
        }

        if (m_ShowTrySpawnSynchronouslyButton)
        {
            m_ButtonSpawnSynchronously.clickable.clicked += TrySpawnSynchronously;
        }

        if (m_ShowSpawnUsingVisibilityButton)
        {
            m_ButtonSpawnWithVisibility.clickable.clicked += SpawnInvisible;
        }

        SetUIElementVisibility(m_ButtonLoadAllAsync, m_ShowLoadAllAsyncButton);
        SetUIElementVisibility(m_ButtonSpawnSynchronously, m_ShowSpawnUsingVisibilityButton);
        SetUIElementVisibility(m_ButtonSpawnWithVisibility, m_ShowTrySpawnSynchronouslyButton);

        m_SliderArtificialLatency.RegisterValueChangedCallback(OnArtificialLatencySliderChanged);
        m_SliderNetworkSpawnTimeout.RegisterValueChangedCallback(OnNetworkSpawnTimeoutSliderChanged);
        m_TextFieldArtificialLatency.RegisterValueChangedCallback(OnArtificialLatencyInputChanged);
        m_TextFieldLabelNetworkSpawnTimeout.RegisterValueChangedCallback(OnNetworkSpawnTimeoutInputChanged);
    }

    void OnDestroy()
    {
        // un-register buttons from methods using callbacks for when they're clicked 
        m_ButtonResetScene.clickable.clicked -= ResetScene;
        m_ButtonLoadAllAsync.clickable.clicked -= LoadAllPrefabsAsync;
        m_ButtonSpawnSynchronously.clickable.clicked -= TrySpawnSynchronously;
        m_ButtonSpawnWithVisibility.clickable.clicked -= SpawnInvisible;
    }

    void Start()
    {
        m_ArtificialLatency = 1000;
        m_NetworkSpawnTimeout = 3000;
        m_SliderArtificialLatency.value = m_ArtificialLatency;
        m_SliderNetworkSpawnTimeout.value = m_NetworkSpawnTimeout;
        m_TextFieldArtificialLatency.value = m_ArtificialLatency.ToString();
        m_TextFieldLabelNetworkSpawnTimeout.value = m_NetworkSpawnTimeout.ToString();
        SetUIElementVisibility(m_InGameUIRoot, false);
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

    void OnClientLoadedPrefabEvent(ulong clientId, int prefabHash, string prefabName, LoadStatus loadStatus)
    {
        var clientUI = GetClientUI(clientId);
        clientUI.SetRow(prefabHash, prefabName, LoadStatus.Loaded);
    }

    void AddConnectionUIInstance(ulong clientID, int[] prefabHashes, string[] prefabNames)
    {
        var clientUI = GetClientUI(clientID);
        for (int i = 0; i < prefabHashes.Length; i++)
        {
            clientUI.SetRow(prefabHashes[i], prefabNames[i], LoadStatus.Loading);
        }
    }

    void RemoveConnectionUIInstance(ulong clientID)
    {
        var clientUI = GetClientUI(clientID);
        if (m_Clients.ContainsKey(clientID))
        {
            clientUI.RemoveClientUI(m_ConnectionsTemplatesHolder);
            m_Clients.Remove(clientID);
        }
    }

    void ModifyConnectionsUIRow(ulong clientID, int prefabHashes, string prefabName, LoadStatus loadStatus)
    {
        var clientUI = GetClientUI(clientID);
        clientUI.SetRow(prefabHashes, prefabName, loadStatus);
    }

    void RemoveConnectionUIRow(ulong clientID, int prefabHash)
    {
        var clientUI = GetClientUI(clientID);
        if (m_Clients.ContainsKey(clientID))
        {
            clientUI.RemoveRow(prefabHash);
        }
    }

    void ResetScene()
    {
        // scene reset logic can be hooked up here
        Debug.Log("Reset scene button clicked");
    }

    void LoadAllPrefabsAsync()
    {
        // load all prefabs logic can be hooked up here
        Debug.Log("Load all prefabs async button clicked");
    }

    void TrySpawnSynchronously()
    {
        // try spawn synced logic can be hooked up here
        Debug.Log("Try spawn synchronously button clicked");
    }

    void SpawnInvisible()
    {
        // spawn invisible logic can be hooked up here
        Debug.Log("Spawn using server visibility button clicked");
    }

    void OnArtificialLatencySliderChanged(ChangeEvent<float> evt)
    {
        m_ArtificialLatency = evt.newValue;
        m_TextFieldArtificialLatency.value = m_ArtificialLatency.ToString();
    }

    void OnNetworkSpawnTimeoutSliderChanged(ChangeEvent<float> evt)
    {
        m_NetworkSpawnTimeout = evt.newValue;
        m_TextFieldLabelNetworkSpawnTimeout.value = m_NetworkSpawnTimeout.ToString();
    }

    void OnArtificialLatencyInputChanged(ChangeEvent<string> evt)
    {
        m_ArtificialLatency = int.Parse(evt.newValue);
        Math.Clamp(m_ArtificialLatency, 0, 9999);
        m_SliderArtificialLatency.value = m_ArtificialLatency;
    }

    void OnNetworkSpawnTimeoutInputChanged(ChangeEvent<string> evt)
    {
        m_NetworkSpawnTimeout = int.Parse(evt.newValue);
        Math.Clamp(m_NetworkSpawnTimeout, 0, 99999);
        m_SliderNetworkSpawnTimeout.value = m_NetworkSpawnTimeout;
    }

    void SetUIElementVisibility(VisualElement visualElement, bool isVisible)
    {
        visualElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void SetupConnectionsUI()
    {
        m_HostUIRoot = m_ConnectionsUIDoc.rootVisualElement;
        m_InGameUIRoot = m_InGameUIDoc.rootVisualElement;
        m_ConnectionsTemplatesHolder = m_HostUIRoot.Q<VisualElement>("ConnectionsHolder");
        m_ButtonLoadAllAsync = m_InGameUIRoot.Q<Button>("LoadAsync");
        m_ButtonSpawnSynchronously = m_InGameUIRoot.Q<Button>("SpawnSync");
        m_ButtonSpawnWithVisibility = m_InGameUIRoot.Q<Button>("SpawnInvisible");
        m_ButtonResetScene = m_InGameUIRoot.Q<Button>("ResetScene");
        m_SliderArtificialLatency = m_InGameUIRoot.Q<Slider>("SliderArtificialLatency");
        m_SliderNetworkSpawnTimeout = m_InGameUIRoot.Q<Slider>("SliderSpawnTimeout");
        m_TextFieldArtificialLatency = m_InGameUIRoot.Q<TextField>("ArtificialLatencyValue");
        m_TextFieldLabelNetworkSpawnTimeout = m_InGameUIRoot.Q<TextField>("NetworkTimeoutValue");
    }

    // test methods
    [ContextMenu("TurnOnInGameUI")]
    void TestVisibilityOn()
    {
        SetUIElementVisibility(m_InGameUIRoot, true);
    }

    [ContextMenu("TurnOffInGameUI")]
    void TestVisibilityOff()
    {
        SetUIElementVisibility(m_InGameUIRoot, false);
    }

    [ContextMenu("Add Client 1234")]
    void TestAddClient()
    {
        AddConnectionUIInstance(1234, new int[] {0, 1, 2, 3}, new String[] {"prefab 0", "prefab 1", "prefab 2", "prefab 3"});
    }

    [ContextMenu("Add Client 5678")]
    void TestAddClient2()
    {
        AddConnectionUIInstance(5678, new int[] {0, 1, 2, 3}, new String[] {"prefab 0", "prefab 1", "prefab 2", "prefab 3"});
    }

    [ContextMenu("Add Client 9101112")]
    void TestAddClient3()
    {
        AddConnectionUIInstance(9101112, new int[] {0, 1, 2, 3}, new String[] {"prefab 0", "prefab 1", "prefab 2", "prefab 3"});
    }

    [ContextMenu("Remove Client 5678")]
    void TestRemoveClient2()
    {
        RemoveConnectionUIInstance(5678);
    }

    [ContextMenu("Remove Row 2 from Client 5678")]
    void TestRemoveRow()
    {
        RemoveConnectionUIRow(5678, 2);
    }

    [ContextMenu("Add Row to Client 5678")]
    void TestAddRow()
    {
        ModifyConnectionsUIRow(5678, 10, "added prefab", LoadStatus.Loading);
    }
}
