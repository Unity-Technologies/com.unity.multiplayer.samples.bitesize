using Unity.Netcode;
using Unity.Netcode.Components;

#if UNITY_EDITOR
using UnityEditor;
// This bypases the default custom editor for NetworkTransform
// and lets you modify your custom NetworkTransform's properties
// within the inspector view
[CustomEditor(typeof(DASmartTransform), true)]
public class DASmartTransformEditor : Editor
{
}
#endif

public class DASmartTransform : NetworkTransform
{
    public bool DisableInClientServer = false;

    /// <summary>
    /// In distributed authority mode it switches to owner authoritative.
    /// In client-server mode it uses the original server authoritative.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return NetworkManager.DistributedAuthorityMode ? false : true;
    }

    protected override void OnInitialize(ref NetworkTransformState replicatedState)
    {
        if (DisableInClientServer && !NetworkManager.DistributedAuthorityMode)
        {
            enabled = false;
            Destroy(this);
        }
        base.OnInitialize(ref replicatedState);
    }

    protected override void Update()
    {
        if (DisableInClientServer && !NetworkManager.DistributedAuthorityMode)
        {
            return;
        }
        base.Update();
    }
}
