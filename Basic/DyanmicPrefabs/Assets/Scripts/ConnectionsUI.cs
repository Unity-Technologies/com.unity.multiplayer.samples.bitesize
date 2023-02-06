using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = System.Random;

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

    Dictionary<ulong, ClientUI> m_Clients = new Dictionary<ulong, ClientUI>();

    // internal enum used for testing
    internal enum LoadStatus
    {
        Loaded,
        Loading
    }

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
        m_ButtonLoadAllAsync.clickable.clicked += LoadAllPrefabsAsync;
        m_ButtonSpawnSynchronously.clickable.clicked += TrySpawnSynchronously;
        m_ButtonSpawnWithVisibility.clickable.clicked += SpawnInvisible;
    }

    void OnDestroy()
    {
        // un-register buttons from methods using callbacks for when they're clicked 
        //m_ButtonResetScene.clickable.clicked -= ResetScene;
        //m_ButtonLoadAllAsync.clickable.clicked -= LoadAllPrefabsAsync;
        //m_ButtonSpawnSynchronously.clickable.clicked -= TrySpawnSynchronously;
        //m_ButtonSpawnWithVisibility.clickable.clicked -= SpawnInvisible;
    }

    void Start()
    {
        ToggleInGameUIVisibility(false);
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
    
    void ToggleInGameUIVisibility(bool isVisible)
    {
        m_ButtonsUIRoot.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        m_HostUIRoot.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        m_SlidersUIRoot.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void SetupConnectionsUI()
    {
        m_HostUIRoot = m_ConnectionsUIDoc.rootVisualElement;
        m_ButtonsUIRoot = m_ButtonsUIDoc.rootVisualElement;
        m_SlidersUIRoot = m_SlidersUIDoc.rootVisualElement;
        m_ConnectionsTemplatesHolder = m_HostUIRoot.Q<VisualElement>("ConnectionsHolder");
        m_ButtonLoadAllAsync = m_ButtonsUIRoot.Q<Button>("LoadAsync");
        m_ButtonSpawnSynchronously = m_ButtonsUIRoot.Q<Button>("SpawnSync");
        m_ButtonSpawnWithVisibility = m_ButtonsUIRoot.Q<Button>("SpawnInvisible");
        m_ButtonResetScene = m_ButtonsUIRoot.Q<Button>("ResetScene");
    }
    
    
    [ContextMenu("TurnOnInGameUI")]
    void TestVisibilityOn()
    {
        ToggleInGameUIVisibility(true);
    }
    [ContextMenu("TurnOffInGameUI")]
    void TestVisibilityOff()
    {
        ToggleInGameUIVisibility(false);
    }

    [ContextMenu("Add Client 1234")]
    void TestAddClient()
    {
        AddConnectionUIInstance(1234, new int []{0,1,2,3}, new String []{"prefab 0", "prefab 1", "prefab 2", "prefab 3"});
    }
    
    [ContextMenu("Add Client 5678")]
    void TestAddClient2()
    {
        AddConnectionUIInstance(5678, new int []{0,1,2,3}, new String []{"prefab 0", "prefab 1", "prefab 2", "prefab 3"});
    }
    [ContextMenu("Add Client 9101112")]
    void TestAddClient3()
    {
        AddConnectionUIInstance(9101112, new int []{0,1,2,3}, new String []{"prefab 0", "prefab 1", "prefab 2", "prefab 3"});
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
