using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// To be placed on a preconfigured child of any network prefab that can be owned by a player.
/// This will set any mesh at the current hierarchy scope to the player's color when display ownership
/// is enabled.
/// This requires the <see cref="PlayerColor"/> component to be attached to player prefabs to work properly.
/// </summary>
public class ObjectOwnerColor : NetworkBehaviour
{
    [Tooltip("If enabled, the root MeshRenderer this component is attached to and any children MeshRenderers will have the player's color applied. " +
    "When disabled only the root MeshRenderer this component is attached to will have the player's color applied.")]
    public bool ApplyColorToChildren;

    [Tooltip("If enabled, the ownership mesh will always be visible. When disabled, it only becomes visible when the display ownership state is active.")]
    public bool AlwaysVisualize;

    private static List<ObjectOwnerColor> ActiveInstances = new List<ObjectOwnerColor>();
    private static Dictionary<ulong, List<ObjectOwnerColor>> s_OwnerActiveInstances = new Dictionary<ulong, List<ObjectOwnerColor>>();
    public static bool ActiveState;

    public static void Reset()
    {
        ActiveInstances.Clear();
        s_OwnerActiveInstances.Clear();
        ActiveState = false;
    }

    public static void ToggleOwnerColorSphere(bool areActive)
    {
        ActiveState = areActive;
        foreach (var instance in ActiveInstances)
        {
            instance.Visualize(areActive);
        }
    }

    public static void UpdateOwnerColors()
    {
        foreach (var instance in ActiveInstances)
        {
            instance.ApplyOwnerColor();
        }
    }

    public static void RemoveOwner(ulong clientId)
    {
        if (!s_OwnerActiveInstances.ContainsKey(clientId))
        {
            return;
        }

        foreach (var instance in s_OwnerActiveInstances[clientId])
        {
            instance.ApplyOwnerColor();
        }

        s_OwnerActiveInstances.Remove(clientId);
    }

    public static void UpdateOwnerColors(ulong clientId)
    {
        if (!s_OwnerActiveInstances.ContainsKey(clientId))
        {
            return;
        }
        foreach (var instance in s_OwnerActiveInstances[clientId])
        {
            instance.ApplyOwnerColor();
        }
    }

    private MeshRenderer m_MeshRenderer;

    private void Awake()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
    }

    public void Visualize(bool isEnabled)
    {
        if (m_MeshRenderer != null)
        {
            m_MeshRenderer.enabled = AlwaysVisualize ? true : isEnabled;
        }
    }

    public override void OnNetworkSpawn()
    {
        ActiveInstances.Add(this);
        if (!s_OwnerActiveInstances.ContainsKey(OwnerClientId))
        {
            s_OwnerActiveInstances.Add(OwnerClientId, new List<ObjectOwnerColor>());
        }
        s_OwnerActiveInstances[OwnerClientId].Add(this);
        Visualize(ActiveState);
        if (PlayerColor.HasPlayerColor(OwnerClientId))
        {
            ApplyOwnerColor();
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        ActiveInstances.Remove(this);
        if (s_OwnerActiveInstances.ContainsKey(OwnerClientId))
        {
            s_OwnerActiveInstances[OwnerClientId].Remove(this);
        }
        base.OnNetworkDespawn();
    }

    protected override void OnOwnershipChanged(ulong previousOwner, ulong newOwner)
    {
        if (!s_OwnerActiveInstances.ContainsKey(newOwner))
        {
            s_OwnerActiveInstances.Add(newOwner, new List<ObjectOwnerColor>());
        }
        if (!s_OwnerActiveInstances[newOwner].Contains(this))
        {
            s_OwnerActiveInstances[newOwner].Add(this);
        }
        ApplyOwnerColor(newOwner);
        base.OnOwnershipChanged(previousOwner, newOwner);
    }

    private void ApplyOwnerColor()
    {
        ApplyOwnerColor(OwnerClientId);
    }

    private void ApplyOwnerColor(ulong ownerId)
    {
        if (!m_MeshRenderer)
        {
            return;
        }

        var color = PlayerColor.GetPlayerColor(ownerId);

        var currentColor = m_MeshRenderer.material.color;
        currentColor.r = color.r;
        currentColor.g = color.g;
        currentColor.b = color.b;
        m_MeshRenderer.material.color = currentColor;
        if (ApplyColorToChildren)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var childMeshRenderer in meshRenderers)
            {
                childMeshRenderer.material.color = color;
            }
        }
    }
}
