using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;


public class MappingInfo : NetworkBehaviour
{
    private NetworkVariable<int> m_Counter = new NetworkVariable<int>();

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        // Get scene mapping information for all connected clients
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GetMappingsRpc(NetworkSceneManager.MapTypes.ServerToClient);
            LogMapping(NetworkManager.LocalClientId, NetworkManager.SceneManager.GetSceneMapping(NetworkSceneManager.MapTypes.ServerToClient));

        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GetMappingsRpc(NetworkSceneManager.MapTypes.ClientToServer);
            LogMapping(NetworkManager.LocalClientId, NetworkManager.SceneManager.GetSceneMapping(NetworkSceneManager.MapTypes.ClientToServer));
        }

        if (Input.GetKeyDown(KeyCode.Tab) && HasAuthority)
        {
            m_Counter.Value++;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            NetworkLog.LogInfoServer("Testing info log message!");
            NetworkLog.LogWarningServer("Testing warning log message!");
            NetworkLog.LogErrorServer("Testing error log message!");
        }
    }

    public override void OnNetworkSpawn()
    {
        m_Counter.OnValueChanged = OnCounterChanged;
        base.OnNetworkSpawn();
    }

    private void OnCounterChanged(int previous, int current)
    {
        NetworkManagerHelper.Instance.LogMessage($"[Counter ({m_Counter.Value})][HasAuthority: {HasAuthority}]");
    }

    public override void OnNetworkDespawn()
    {
        NetworkManagerHelper.Instance.LogMessage("Despawning MappingInfo");
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        NetworkManagerHelper.Instance.LogMessage("Destroying MappingInfo");
        base.OnDestroy();
    }


    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    private void GetMappingsRpc(NetworkSceneManager.MapTypes mapType, RpcParams rpcParams = default)
    {
        var mappings = NetworkManager.SceneManager.GetSceneMapping(mapType);
        SendMappingsRpc(mappings.ToArray(), RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
    private void SendMappingsRpc(NetworkSceneManager.SceneMap[] mappings, RpcParams rpcParams = default)
    {
        LogMapping(rpcParams.Receive.SenderClientId, mappings.ToList());
    }

    private Dictionary<ulong, List<NetworkSceneManager.SceneMap>> m_ClientMappings = new Dictionary<ulong, List<NetworkSceneManager.SceneMap>>();

    private void LogMapping(ulong clientId, List<NetworkSceneManager.SceneMap> mappings) 
    {
        var builder = new StringBuilder();
        if (!m_ClientMappings.ContainsKey(clientId))
        {
            m_ClientMappings.Add(clientId, mappings);
        }
        else
        {
            m_ClientMappings[clientId] = mappings;
        }
        var sessionOwner = NetworkManager.ConnectedClients[clientId].IsSessionOwner ? "[Session Owner]" : string.Empty;
        builder.AppendLine($"[Mappings][Client-{clientId}]{sessionOwner}");
        foreach(var map in mappings )
        {
            var warning = string.Empty;
            if (map.LocalHandle != map.MappedLocalHandle) 
            {
                warning = "[!!!!!]";
            }

            if (map.MapType == NetworkSceneManager.MapTypes.ServerToClient) 
            {
                builder.AppendLine($"{warning}[{System.Enum.GetName(typeof(NetworkSceneManager.MapTypes), map.MapType)}][{map.SceneName}][{map.ScenePresent}][Server-Map: {map.ServerHandle}][Local-Map: {map.MappedLocalHandle}][Local: {map.LocalHandle}]");
            }
            else
            {
                builder.AppendLine($"{warning}[{System.Enum.GetName(typeof(NetworkSceneManager.MapTypes), map.MapType)}][{map.SceneName}][{map.ScenePresent}][Local: {map.LocalHandle}][Local-Map: {map.MappedLocalHandle}][Server-Map: {map.ServerHandle}]");
            }            
        }
        NetworkManagerHelper.Instance.LogMessage( builder.ToString() );
    }
}