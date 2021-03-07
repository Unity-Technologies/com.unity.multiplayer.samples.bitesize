using System;
using UnityEngine;
using System.Collections.Generic;
using UnityAnalyticsHeatmap;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class HeatmapSubmap : MonoBehaviour
{

    public List<HeatPoint> m_PointData;
    public int m_TrianglesPerShape;

    void Start()
    {
        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GetComponent<MeshRenderer>().receiveShadows = false;

        #if UNITY_5_4_OR_NEWER
        GetComponent<MeshRenderer>().lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        #else
        GetComponent<MeshRenderer>().useLightProbes = false;
        #endif
        GetComponent<MeshRenderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }
}
