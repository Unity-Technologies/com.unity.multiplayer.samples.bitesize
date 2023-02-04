using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class ConnectionsUI : MonoBehaviour
{
    // UI Documents
    [SerializeField]
    UIDocument m_ConnectionsUIDoc;
    [SerializeField]
    UIDocument m_ButtonsUIDoc;
    [SerializeField]
    UIDocument m_SlidersUIDoc;

    // UI Roots
    VisualElement m_ButtonsUIRoot;
    VisualElement m_HostUIRoot;
    VisualElement m_SlidersUIRoot;

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
    
    [SerializeField]
    int totalClients = 4;
    [SerializeField]
    ulong[] clientIDs;

    internal struct ClientUI
    {
        VisualTreeAsset m_RowTemplatePrefabReference;
        
        Dictionary<int, TemplateContainer> m_RowInstances;
        TemplateContainer m_ClientInstance;
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

        internal void SetRow(int prefabHash, string prefabName, DynamicPrefabManager.LoadStatus loadStatus)
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

    Dictionary<ulong, ClientUI> m_Clients = new Dictionary<ulong, ClientUI>();

    void Awake()
    {
        clientIDs = new ulong[totalClients];
        Array.Clear(clientIDs, 0, clientIDs.Length);
        for (int clientIndex = 0; clientIndex < totalClients; clientIndex++)
        {
            var id = Random.Range(0, 1000000000);
            clientIDs[clientIndex] = (ulong) id;
        }
    }
    
    void Start()
    {
        SetupUIRoots();
        SetupButtonsUI();
        FindObjectOfType<DynamicPrefabManager>().clientLoadedTemplateEvent += OnClientLoadedPrefabEvent;
        ToggleVisibility(m_ButtonsUIRoot, false);
        ToggleVisibility(m_HostUIRoot, false);
        ToggleVisibility(m_SlidersUIRoot, false);
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

    void OnClientLoadedPrefabEvent(ulong clientId, int prefabHash, string prefabName, DynamicPrefabManager.LoadStatus loadStatus)
    {
        var clientUI = GetClientUI(clientId);
        clientUI.SetRow(prefabHash, prefabName, DynamicPrefabManager.LoadStatus.Loaded);
    }

    static int[] testPrefabHashes = new int[] {0, 1, 2, 3};
    static string[] testPrefabNames = new String[] {"name 0","name 1","name 2","name 3"};
    ulong testClientID = 0;

    //todo: add debug logs for when buttons are clicked
    //todo: add comments

    void AddConnectionUIInstance(ulong clientID, int[] prefabHashes, string[] prefabNames)
    {
        var clientUI = GetClientUI(clientID);
        for (int i = 0; i < prefabHashes.Length; i++)
        {
            clientUI.SetRow(prefabHashes[i], prefabNames[i], DynamicPrefabManager.LoadStatus.Loading);
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

    void ModifyConnectionsUIRow(ulong clientID, int prefabHashes, string prefabName, DynamicPrefabManager.LoadStatus loadStatus)
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
    
    void SetupUIRoots()
    {
        m_HostUIRoot = m_ConnectionsUIDoc.rootVisualElement;
        m_ButtonsUIRoot = m_ButtonsUIDoc.rootVisualElement;
        m_SlidersUIRoot = m_SlidersUIDoc.rootVisualElement;
        m_ConnectionsTemplatesHolder = m_HostUIRoot.Q<VisualElement>("ConnectionsHolder");
    }

    void SetupButtonsUI()
    {
        m_ButtonLoadAllAsync = m_ButtonsUIRoot.Q<Button>("LoadAsync");
        m_ButtonSpawnSynchronously = m_ButtonsUIRoot.Q<Button>("SpawnSync");
        m_ButtonSpawnWithVisibility = m_ButtonsUIRoot.Q<Button>("SpawnInvisible");
        m_ButtonResetScene = m_ButtonsUIRoot.Q<Button>("ResetScene");
    }

    void ToggleVisibility(VisualElement element, bool isVisible)
    {
        element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void SetupSliderUI()
    {
        
    }
}
