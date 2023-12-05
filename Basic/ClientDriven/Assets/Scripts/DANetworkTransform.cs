using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
// This bypases the default custom editor for NetworkTransform
// and lets you modify your custom NetworkTransform's properties
// within the inspector view
[CustomEditor(typeof(DANetworkTransform), true)]
public class DANetworkTransformEditor : Editor
{
}
#endif
public class DANetworkTransform : NetworkTransform
{
    public enum AuthorityModes
    {
        Owner,
        Server
    }

    public AuthorityModes AuthorityMode;

    protected override bool OnIsServerAuthoritative()
    {
        return AuthorityMode == AuthorityModes.Server;
    }
}
