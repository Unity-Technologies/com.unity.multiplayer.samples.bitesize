using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkListExample : NetworkBehaviour
{
    NetworkList<FixedString64Bytes> list;

    void Awake()
    {
        list = new NetworkList<FixedString64Bytes>();
    }

    void Start()
    {
        /*At this point, the object has not been network spawned yet, so you're not allowed to edit network variables! */
        //list.Add(new FixedString64Bytes("testtest"));
    }

    void Update()
    {
        if (!IsServer) { return; }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            list.Add(new FixedString64Bytes($"Name: {list.Count}"));
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsClient)
        {
            list.OnListChanged += OnClientListChanged;
        }
        if (IsServer)
        {
            list.OnListChanged += OnServerListChanged;
            list.Add(new FixedString64Bytes("testtest"));
        }
    }

    void OnServerListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        Debug.Log($"[S] The list changed and now contains {list.Count} elements");
    }

    void OnClientListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        Debug.Log($"[C] The list changed and now contains {list.Count} elements");
    }
}
