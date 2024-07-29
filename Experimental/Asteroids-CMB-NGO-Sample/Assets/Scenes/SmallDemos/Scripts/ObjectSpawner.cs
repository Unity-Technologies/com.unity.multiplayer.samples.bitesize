using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


public class ObjectSpawner : NetworkBehaviour
{
    public static ObjectSpawner Instance;

    public GameObject NormalSpawn;
    public GameObject RequestRequiredSpawn;


    private List<NetworkObject> SpawnedObjects = new List<NetworkObject>();

    private List<ulong> SpawnObservers = new List<ulong>();

    public bool ApproveRequest = true;

    public int MaximumOwnedObjects = 15;

    private void Awake()
    {
        Instance = this;
    }

    private Vector2 m_ScrollPosition = Vector2.zero;



    private void OnGUI()
    {
        if (!IsSpawned) return;

        GUILayout.BeginArea(new Rect(Screen.width - 200, 50, 190, 400));
        if (NetworkManager.LocalClient.OwnedObjects.Length < MaximumOwnedObjects)
        {
            if (GUILayout.Button("Spawn"))
            {
                SpawnObject();
            }
        }
        else
        {
            GUILayout.Label("Max Owned Objects!");
        }

        ApproveRequest = GUILayout.Toggle(ApproveRequest, "Approve Requests");
        if (NetworkManager.LocalClient.OwnedObjects.Length < 15)
        {
            if (GUILayout.Button("Spawn Request-Req"))
            {
                SpawnObject(true);
            }
        }


        if (SpawnedObjects.Count > 0)
        {
            if (GUILayout.Button("Despawn Object"))
            {
                DespawnObject();
            }
        }

        if (m_SelectedNonOwnedObject != null)
        {
            if (GUILayout.Button("Request Ownership"))
            {
                var requestStatus = m_SelectedNonOwnedObject.NetworkObject.RequestOwnership();
                NetworkManagerHelper.Instance.LogMessage($"Request Status: {requestStatus}");
            }
        }

        if (NetworkManager.ConnectedClientsIds.Count > 1)
        {
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(140), GUILayout.Height(100));
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }
                var include = SpawnObservers.Contains(clientId);
                include = GUILayout.Toggle(include, new GUIContent($"Client-{clientId}"));
                if (include && !SpawnObservers.Contains(clientId))
                {
                    SpawnObservers.Add(clientId);
                }
                else if (!include && SpawnObservers.Contains(clientId))
                {
                    SpawnObservers.Remove(clientId);
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Show objects"))
            {
                ShowHideObjects(true);
            }
            if (GUILayout.Button("Hide objects"))
            {
                ShowHideObjects(false);
            }
        }
        GUILayout.EndArea();
    }

    /// <summary>
    /// If a client instance (or other client owned objects) could potentially
    /// collide with objects hidden from the client, this method demonstrates 
    /// how you can prevent collisions from ocurring.
    /// </summary>
    private void IgnoreCollision(GameObject objectA, GameObject objectB, bool shouldIgnore)
    {
        var rootA = objectA.transform.root.gameObject;
        var rootB = objectB.transform.root.gameObject;

        var collidersA = rootA.GetComponentsInChildren<Collider>();
        var collidersB = rootB.GetComponentsInChildren<Collider>();

        foreach (var colliderA in collidersA)
        {
            foreach (var colliderB in collidersB)
            {
                Physics.IgnoreCollision(colliderA, colliderB, shouldIgnore);
            }
        }
    }

    private void ShowHideObjects(bool show)
    {
        foreach (var networkObject in MoverScript.SelectedObjects)
        {
            foreach (var clientId in SpawnObservers)
            {
                var client = NetworkManager.ConnectedClients[clientId];
                if (show)
                {
                    networkObject.NetworkShow(clientId);
                    // Resume collisions with the client when the object is visible
                    IgnoreCollision(client.PlayerObject.gameObject, networkObject.gameObject, false);
                }
                else
                {
                    networkObject.NetworkHide(clientId);
                    // Ignore collisions with the client when the object is invisible
                    IgnoreCollision(client.PlayerObject.gameObject, networkObject.gameObject, true);
                }
            }
        }
    }

    private void Update()
    {
        UpdateInput();
    }

    private void UpdateInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }
        var selection = MouseSelectObject.SelectObject<MoverScript>();
        if (selection == null)
        {
            return;
        }
        if (selection.NetworkObject.IsOwner)
        {
            selection.SetObjectSelected();
            return;
        }
        else
        {
            if (m_SelectedNonOwnedObject != null)
            {
                m_SelectedNonOwnedObject.SetNonOwnerObjectSelected();
                if (m_SelectedNonOwnedObject == selection)
                {
                    m_SelectedNonOwnedObject = null;
                    return;
                }
            }

            if (m_SelectedNonOwnedObject != selection && selection.NetworkObject.HasOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired))
            {
                selection.SetNonOwnerObjectSelected();
                m_SelectedNonOwnedObject = selection;
            }
        }
    }

    private void UpdateInputOld()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.current == null)
            {
                Camera.SetupCurrent(Camera.allCameras[0]);
            }

            var screenRay = Camera.current.ScreenPointToRay(Input.mousePosition);

            //Grab the information we need
            var hitInfo = Physics.RaycastAll(screenRay);
            if (hitInfo == null || hitInfo.Length == 0)
            {
                return;
            }

            foreach (var hit in hitInfo)
            {
                var moverScript = hit.collider.GetComponent<MoverScript>();
                if (moverScript == null)
                {
                    if (hit.transform.parent != null)
                    {
                        moverScript = hit.transform.parent.GetComponent<MoverScript>();
                        if (moverScript == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                if (moverScript.NetworkObject.IsOwner)
                {
                    moverScript.SetObjectSelected();
                    break;
                }
                else
                {
                    if (m_SelectedNonOwnedObject != null)
                    {
                        m_SelectedNonOwnedObject.SetNonOwnerObjectSelected();
                        if (m_SelectedNonOwnedObject == moverScript)
                        {
                            m_SelectedNonOwnedObject = null;
                            return;
                        }
                    }

                    if (m_SelectedNonOwnedObject != moverScript && moverScript.NetworkObject.HasOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired))
                    {
                        moverScript.SetNonOwnerObjectSelected();
                        m_SelectedNonOwnedObject = moverScript;
                    }
                }
            }
        }
    }

    private MoverScript m_SelectedNonOwnedObject;

    public override void OnNetworkDespawn()
    {
        SpawnedObjects.Clear();
        base.OnNetworkDespawn();
    }

    private void SpawnObject(bool requestRequired = false)
    {
        var objectToSpawm = requestRequired ? RequestRequiredSpawn : NormalSpawn;

        if (objectToSpawm == null)
        {
            NetworkLog.LogErrorServer($"[Cannot Spawn] No network prefab is assigned to {name}!");
            return;
        }

        var instance = Instantiate(objectToSpawm);
        var networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true);
            SpawnedObjects.Add(networkObject);
        }
    }

    public void RegisterMoverScript(MoverScript moverScript)
    {
        moverScript.OwnershipChangedNotification += MoverScript_OwnershipChangedNotification;
    }

    private void MoverScript_OwnershipChangedNotification(MoverScript moverScript)
    {
        if (moverScript.IsOwner)
        {
            if (!SpawnedObjects.Contains(moverScript.NetworkObject))
            {
                SpawnedObjects.Add(moverScript.NetworkObject);
            }

            if (m_SelectedNonOwnedObject == moverScript)
            {
                moverScript.SetNonOwnerObjectSelected(true);
                m_SelectedNonOwnedObject = null;
            }
        }
        else
        {
            if (SpawnedObjects.Contains(moverScript.NetworkObject))
            {
                SpawnedObjects.Remove(moverScript.NetworkObject);
            }
        }
    }

    private void DespawnObject()
    {
        try
        {
            var first = SpawnedObjects[0];
            if (first == NetworkObject)
            {
                throw new System.Exception("Trying to despawn the spawner!");
            }
            if (first != null && first.IsSpawned)
            {
                if (MoverScript.SelectedObjects.Contains(first))
                {
                    MoverScript.SelectedObjects.Remove(first);
                }
                SpawnedObjects.Remove(first);
                first.Despawn(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
