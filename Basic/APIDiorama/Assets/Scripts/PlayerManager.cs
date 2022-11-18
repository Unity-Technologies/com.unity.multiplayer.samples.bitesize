using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField]
    GameObject spawnedObjectPrefab;
    GameObject lastSpawnedObjectInstance;

    [SerializeField]
    PlayerInput inputManager;

    NetworkVariable<int> syncedNumber = new NetworkVariable<int>(); //must be initialized on declaration
    NetworkVariable<SyncableCustomData> syncedCustomData = new NetworkVariable<SyncableCustomData>(writePerm: NetworkVariableWritePermission.Owner); //you can adjust who can write to it with parameters

    struct SyncableCustomData : INetworkSerializable //can only contain value types
    {
        public float floaty;
        public bool booly;
        public FixedString128Bytes message; //value-type version of string with fixed allocation

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref floaty);
            serializer.SerializeValue(ref booly);
            serializer.SerializeValue(ref message);
        }
    }


    [SerializeField]
    float moveSpeed = 1;

    //----- These are basically Awake & OnDestroy for Network initialization/deInitialization
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        syncedNumber.OnValueChanged += OnNumberChanged;
        syncedCustomData.OnValueChanged += OnCustomDataChanged;
        if (inputManager)
        {
            inputManager.enabled = IsOwner;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        syncedNumber.OnValueChanged -= OnNumberChanged;
        syncedCustomData.OnValueChanged -= OnCustomDataChanged;
    }
    //----------------------------------

    void OnNumberChanged(int previousValue, int newValue)
    {
        Debug.Log($"{OwnerClientId} -> Previous: {previousValue}, New: {newValue}");
    }

    void OnCustomDataChanged(SyncableCustomData previousValue, SyncableCustomData newValue)
    {
        Debug.Log($"{OwnerClientId} -> Previous: {previousValue.booly} | {previousValue.floaty} | {previousValue.message}, New: {newValue.booly} | {newValue.floaty} | {newValue.message}");
    }

    void Update()
    {
        /*
         * note: NGO is server-authoritative by default, which means that even if clients are owners, their position
         * will be dictated by the server when using a NetworkTransform. The alternative is to use a ClientNetworkTransform, which is avilable separately.
         * More info here: https://youtu.be/3yuBOB3VrCk?t=1220
         * 
         * That component should be used for games where player movement is kind of ok to be cheated (I.E: casual co-op games), otherwise you should use
         * prediction + reconciliation
         * 
         */
        if (!IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (IsServer)
            {
                syncedNumber.Value = Random.Range(0, 100);
            }
            syncedCustomData.Value = new SyncableCustomData
            {
                floaty = Random.Range(0f, 1.0f),
                booly = !syncedCustomData.Value.booly,
                message = "Try out the game named 'Ariokan'!"
            };
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnServerDoSomethingServerRpc($"Your lucky number is {Random.Range(5, 10)}");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            OnClientDoSomethingClientRpc($"Your unlucky number is {Random.Range(5, 10)}");
            OnClientDoSomethingOnTargetsClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { 1 } } }); //send only to client with ID 2
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            lastSpawnedObjectInstance = OnServerSpawnObject(spawnedObjectPrefab);
        }

        var moveDir = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            moveDir.z += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDir.z -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir.x += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDir.x -= 1;
        }

        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }

    GameObject OnServerSpawnObject(GameObject prefab)
    {
        if (!IsServer)
        {
            return null;
        }
        if (lastSpawnedObjectInstance)
        {
            OnServerDespawnObject(lastSpawnedObjectInstance);
        }
        //note: spawning networked objects can only be done by the server. If you run this code on the client, you'll see the object locally but errors will be thrown
        GameObject spawnedObject = Instantiate(prefab);
        spawnedObject.transform.position = new Vector3(Random.Range(-3, 3), 1, Random.Range(-3, 3));
        var networkedObject = spawnedObject.GetComponent<NetworkObject>();
        networkedObject.Spawn(true); //the parameter ties the object to the scene's lifetime
        return spawnedObject;
    }

    void OnServerDespawnObject(GameObject instance)
    {
        Destroy(instance); //it's as simple as that!
        //instance.GetComponent<NetworkObject>().Despawn(); //alternatively, you can use this to removethe object from the network but keeping it on the server
        //There's also a flag called "Dont Destroy with Owner" on the Network Obejct, which will keep objects around when the client that owns them disconnects
    }

    [ServerRpc] //the equivalent of a [Command] in Mirror, will be called on the server when the client invokes it
    void OnServerDoSomethingServerRpc(string messageAsString) //weirdly, this can accept a string
    {
        Debug.Log($"[S] OnServerDoSomething {OwnerClientId} > {messageAsString}");
    }

    [ClientRpc] //the equivalent of a [ClientRpc] in Mirror, will be called on the clients when the server invokes it
    void OnClientDoSomethingClientRpc(string messageAsString) //weirdly, this can accept a string
    {
        Debug.Log($"[C] OnClientDoSomething {OwnerClientId} > {messageAsString}");
    }

    [ClientRpc] //the equivalent of a [TargetRpc] in Mirror, will be called on specific clients when the server invokes it
    void OnClientDoSomethingOnTargetsClientRpc(ClientRpcParams parameters)
    {
        Debug.Log($"[C] OnClientDoSomethingOnTargets {OwnerClientId}");
    }
}