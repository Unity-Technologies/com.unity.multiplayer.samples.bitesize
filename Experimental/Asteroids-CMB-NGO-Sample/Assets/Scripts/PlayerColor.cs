using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Only to be placed on player prefabs. This provides the mechanism to assigning each player a unique color
/// and assists in conveying the player's color to other players.
/// <see cref="ObjectOwnerColor"/> requires this component to be attached to player prefabs for it to work properly.
/// </summary>
public class PlayerColor : NetworkBehaviour
{
    private static Color[] s_Colors = { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, };

    private static Dictionary<ulong, Color> s_PlayerColors = new Dictionary<ulong, Color>();

    public static void Reset()
    {
        s_PlayerColors.Clear();
    }
    [Tooltip("If enabled, the root MeshRenderer this component is attached to and any children MeshRenderers will have the player's color applied. " +
        "When disabled only the root MeshRenderer this component is attached to will have the player's color applied.")]
    public bool ApplyColorToChildren;

    [Tooltip("When enabled this will preserve the alpha values. When disabled it will not.")]
    public bool PreserveChildAlpha;
    
    [Tooltip("This value is multiplied with all of the relative player's color. When PreserveChildAlpha is enabled this will not be applied to the existing Meshrender's color's alpha.")]
    [Range(0.25f, 1.0f)]
    public float ColorSmooth = 0.85f;

    [Tooltip("Any child GameObject in this list will not have the player color applied to it.")]
    public List<GameObject> Ignore;

    private NetworkVariable<Color> m_Color = new NetworkVariable<Color>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [HideInInspector]
    public Color SmoothedColor { get; internal set; }

    public static Color GetPlayerColor(ulong clientId)
    {
        if (s_PlayerColors.ContainsKey(clientId))
        {
            return s_PlayerColors[clientId];
        }
        return s_Colors[(int)(clientId % Convert.ToUInt64(s_Colors.Length))];
    }

    public static Action PlayerColorsChanged;

    public static bool HasPlayerColor(ulong clientId)
    {
        return s_PlayerColors.ContainsKey(clientId);
    }

    private void OnNetworkClientInitialized(ulong clientId)
    {
        NetworkManager.OnClientConnectedCallback -= OnNetworkClientInitialized;
        AssignAndApplyColor();
    }

    private void AssignAndApplyColor()
    {
        if (NetworkManager.LocalClient.NetworkTopologyType == NetworkTopologyTypes.ClientServer)
        {
            m_Color.Value = s_Colors[(int)(NetworkObject.OwnerClientId % Convert.ToUInt64(s_Colors.Length))];
        }
        else
        {
            var assignedColors = new List<Color>();
            foreach (var player in NetworkManager.SpawnManager.PlayerObjects)
            {
                if (player.OwnerClientId == NetworkManager.LocalClientId)
                {
                    continue;
                }
                var playerColor = player.GetComponent<PlayerColor>();

                assignedColors.Add(playerColor.m_Color.Value);
            }
            var remainingColors = s_Colors.ToList();
            foreach (var color in assignedColors)
            {
                remainingColors.Remove(color);
            }
            if (remainingColors.Count == 0)
            {
                Debug.LogWarning($"There are {NetworkManager.SpawnManager.PlayerObjects.Count} players and only {s_Colors.Length} colors! Rolling over and duplicating colors!!!!");
                m_Color.Value = s_Colors[(int)(NetworkObject.OwnerClientId % Convert.ToUInt64(s_Colors.Length))];
            }
            else
            {
                m_Color.Value = remainingColors.FirstOrDefault();
            }

        }
        SetPlayerColor();
        OnApplyPlayerColor();
    }

    public void ApplyPlayerColor()
    {
        OnApplyPlayerColor();
    }

    protected virtual void OnApplyPlayerColor()
    {
        if (IsLocalPlayer)
        {
            var gameObject = GameObject.Find("ClientDisplay");
            if (gameObject != null)
            {
                var serverHost = gameObject.GetComponent<ServerHostClientText>();
                serverHost?.SetColor(m_Color.Value);
            }
        }

        // Get root MeshRenderer
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        // If we have one at the root then colorize it
        if (meshRenderer != null)
        {
            meshRenderer.material.color = SmoothedColor;
        }

        // Colorize MeshRenderers on the children
        if (ApplyColorToChildren)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            var checkIgnor = Ignore.Count > 0;
            foreach (var childMeshRenderer in meshRenderers)
            {
                if (checkIgnor && Ignore.Contains(childMeshRenderer.gameObject))
                {
                    continue;
                }
                var childColor = SmoothedColor;
                if (PreserveChildAlpha)
                {
                    childColor.a = childMeshRenderer.material.color.a;
                }
                childMeshRenderer.material.color = childColor;
            }
        }
    }

    private void SetPlayerColor()
    {
        if (!s_PlayerColors.ContainsKey(OwnerClientId))
        {
            SmoothedColor = m_Color.Value * ColorSmooth;
            s_PlayerColors.Add(OwnerClientId, m_Color.Value);
            ObjectOwnerColor.UpdateOwnerColors(OwnerClientId);
        }
        else
        {
            if (s_PlayerColors[OwnerClientId] != m_Color.Value)
            {
                if (IsLocalPlayer)
                {
                    s_PlayerColors[OwnerClientId] = m_Color.Value;
                    SmoothedColor = m_Color.Value * ColorSmooth;
                }
                else
                {
                    m_Color.Value = s_PlayerColors[OwnerClientId];
                    ObjectOwnerColor.UpdateOwnerColors(OwnerClientId);
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    protected override void OnNetworkPostSpawn()
    {
        if (IsLocalPlayer)
        {
            if (NetworkManager.LocalClient.NetworkTopologyType == NetworkTopologyTypes.ClientServer)
            {
                AssignAndApplyColor();
            }
            else
            {
                // DANGO-TODO: Since the POC Service spawns players after everything else, we have to add a 
                // delay to color assignment in order for it to work properly
                if (NetworkManager.DistributedAuthorityMode && NetworkManager.CMBServiceConnection)
                {
                    StartCoroutine(DelayColorAssignmentLocalPlayer());
                }
                else
                {
                    NetworkManager.OnClientConnectedCallback += OnNetworkClientInitialized;
                }
            }
        }
        else if (NetworkManager.LocalClient.NetworkTopologyType == NetworkTopologyTypes.DistributedAuthority)
        {
            m_Color.OnValueChanged += OnFullColorChanged;

            // DANGO-TODO: Since the POC Service spawns players after everything else, we have to check
            // to see if the player exists and if not wait for the player to announce its color
            if (NetworkManager.CMBServiceConnection)
            {
                StartCoroutine(DelayColorAssignmentRemotePlayer());
            }
            else
            {
                SetPlayerColor();
                OnApplyPlayerColor();
            }
        }

        if (OwnerClientId == NetworkManager.LocalClientId && !IsLocalPlayer)
        {
            Debug.LogWarning($"PlayerColor is on non-player object {name}!");
        }

        base.OnNetworkPostSpawn();
    }


    private IEnumerator DelayColorAssignmentRemotePlayer()
    {
        yield return new WaitForSeconds(0.500f);
        SetPlayerColor();
        OnApplyPlayerColor();
    }

    private IEnumerator DelayColorAssignmentLocalPlayer()
    {
        yield return new WaitForSeconds(0.500f);
        OnNetworkClientInitialized(OwnerClientId);
    }

    public override void OnNetworkDespawn()
    {
        if (IsLocalPlayer)
        {
            s_PlayerColors.Remove(OwnerClientId);
            ObjectOwnerColor.RemoveOwner(OwnerClientId);
        }

        base.OnNetworkDespawn();
    }

    private void OnFullColorChanged(Color previous, Color current)
    {
        if (!s_PlayerColors.ContainsKey(OwnerClientId))
        {
            s_PlayerColors.Add(OwnerClientId, current);
        }
        else
        {
            s_PlayerColors[OwnerClientId] = current;
        }
        SmoothedColor = current * ColorSmooth;
        OnApplyPlayerColor();
        ObjectOwnerColor.UpdateOwnerColors(OwnerClientId);
    }
}
