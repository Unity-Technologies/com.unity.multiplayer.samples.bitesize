/// <summary>
/// Heat map mesh renderer.
/// </summary>
/// This is the default renderer that comes with the Heat Maps package.
/// It procedurally constructs a mesh to display Heat Map data. You
/// might consider writing your own renderer. If you do, we recommend
/// following the defined IHeatmapRenderer interface.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityAnalyticsHeatmap;
using UnityEngine;

[RequireComponent(typeof(GradientContainer))]
public class HeatmapMeshRenderer : MonoBehaviour, IHeatmapRenderer
{

    const int k_NotRendering = 0;
    const int k_BeginRenderer = 1;
    const int k_RenderInProgress = 2;
    const int k_UpdateMaterials = 4;

    // Unity limit of vectors per mesh
    const int k_VerticesPerMesh = 65000;

    // Density Thresholds
    float m_HighThreshold;
    float m_LowThreshold;

    // Time limits
    float m_StartTime = 0f;
    float m_EndTime = 1.0f;

    [Range(0.1f, 100f)]
    float m_ParticleSize = 1.0f;

    bool m_Tips = false;

    // Particle Rendering Data
    HeatPoint[] m_Data;
    float m_MaxDensity = 0f;
    float m_LowX = 0f;
    float m_LowY = 0f;
    float m_LowZ = 0f;
    float m_HighX = 1f;
    float m_HighY = 1f;
    float m_HighZ = 1f;

    RenderShape m_RenderStyle = RenderShape.Cube;
    RenderDirection m_RenderDirection = RenderDirection.YZ;

    Shader m_Shader;
    public Material[] m_Materials;
    Gradient m_Gradient;

    int m_RenderState = k_NotRendering;

    List<GameObject> m_GameObjects = new List<GameObject>();

    void Start()
    {
        m_Shader = Shader.Find("Heatmaps/Particles/AlphaBlend");
    }

    public void UpdatePointData(HeatPoint[] newData, float newMaxDensity)
    {
        m_Data = newData;
        m_MaxDensity = newMaxDensity;
        m_RenderState = k_BeginRenderer;
    }

    public void UpdateColors(Color[] colors)
    {
        // No-op
    }
    
    public void UpdateThresholds(float[] threshholds)
    {
        //No-op
    }

    public void UpdateGradient(Gradient gradient)
    {
        if (m_Materials == null || m_Materials.Length == 0)
        {
            m_Shader = Shader.Find("Heatmaps/Particles/AlphaBlend");
            m_Materials = new Material[1];
            m_Materials[0] = new Material(m_Shader);
        }
        if (gradient == null || !CompareGradients(gradient, m_Gradient))
        {
            m_Gradient = gradient;
            m_RenderState = k_UpdateMaterials;
        }
    }

    public void UpdateRenderMask(float lowX, float highX, float lowY, float highY, float lowZ, float highZ)
    {
        if (lowX != m_LowX)
        {
            m_LowX = lowX;
            m_RenderState = k_BeginRenderer;
        }
        if (lowY != m_LowY)
        {
            m_LowY = lowY;
            m_RenderState = k_BeginRenderer;
        }
        if (lowZ != m_LowZ)
        {
            m_LowZ = lowZ;
            m_RenderState = k_BeginRenderer;
        }
        if (highX != m_HighX)
        {
            m_HighX = highX;
            m_RenderState = k_BeginRenderer;
        }
        if (highY != m_HighY)
        {
            m_HighY = highY;
            m_RenderState = k_BeginRenderer;
        }
        if (highZ != m_HighZ)
        {
            m_HighZ = highZ;
            m_RenderState = k_BeginRenderer;
        }
    }

    public float pointSize
    {
        get
        {
            return m_ParticleSize;
        }
        set
        {
            if (m_ParticleSize != value)
            {
                m_ParticleSize = value;
                m_RenderState = k_BeginRenderer;
            }
        }
    }

    public bool activateTips
    {
        get
        {
            return m_Tips;
        }
        set
        {
            if (m_Tips != value)
            {
                m_Tips = value;
                if (m_Tips)
                {
                    m_RenderState = k_BeginRenderer;
                }
                else
                {
                    //Remove colliders
                    MeshCollider[] children = transform.GetComponentsInChildren<MeshCollider>();
                    foreach (MeshCollider child in children)
                    {
                        DestroyImmediate(child.gameObject.GetComponent<MeshCollider>());
                    }
                }
            }
        }
    }

    public bool allowRender{ get; set; }

    public int currentPoints { get; set; }

    public int totalPoints { get; set; }

    public void UpdateTimeLimits(float startTime, float endTime)
    {
        if (m_StartTime != startTime || m_EndTime != endTime)
        {
            m_StartTime = startTime;
            m_EndTime = endTime;
            m_RenderState = k_BeginRenderer;
        }
    }

    public void UpdateRenderStyle(RenderShape style, RenderDirection direction)
    {
        if (style != m_RenderStyle || direction != m_RenderDirection)
        {
            m_RenderDirection = direction;
            m_RenderStyle = style;
            m_RenderState = k_BeginRenderer;
        }
    }

    public void RenderHeatmap()
    {
        if (allowRender)
        {
            switch (m_RenderState)
            {
                case k_BeginRenderer:
                    m_RenderState = k_RenderInProgress;
                    CreatePoints();
                    break;
                case k_RenderInProgress:
                    if (hasData())
                    {
                        // No-op for now.
                        //UpdateRenderCycle (0, data.Length, renderMaterials);
                    }
                    break;
                case k_UpdateMaterials:
                    if (hasData())
                    {
                        int pt = 0;         // cursor that increments each time we find a point in the time range
                        int currentSubmap = 0;
                        int oldSubmap = -1;
                        int verticesPerShape = GetVecticesForShape();
                        GameObject go = null;
                        Material[] materials = null;
                        for (int a = 0; a < m_Data.Length; a++)
                        {
                            if (m_Data[a].time >= m_StartTime && m_Data[a].time <= m_EndTime)
                            {

                                currentSubmap = (pt * verticesPerShape) / k_VerticesPerMesh;

                                if (currentSubmap != oldSubmap)
                                {
                                    if (go != null && materials != null)
                                    {
                                        go.GetComponent<Renderer>().materials = materials;
                                    }
                                    go = m_GameObjects[currentSubmap];
                                    materials = go.GetComponent<Renderer>().sharedMaterials;
                                }
                                materials = m_Materials;
                                oldSubmap = currentSubmap;
                                pt++;
                            }
                        }
                        if (go != null && materials != null)
                        {
                            go.GetComponent<Renderer>().materials = materials;
                        }

                        CreatePoints();
                    }
                    break;
            }
        }
    }

    void CreatePoints()
    {
        if (hasData())
        {
            totalPoints = m_Data.Length;
            currentPoints = 0;

            var submaps = new List<List<HeatPoint>>();
            int currentSubmap = 0;
            int verticesPerShape = GetVecticesForShape();

            for (int a = 0; a < m_Data.Length; a++)
            {
                // FILTER FOR TIME & POSITION
                var pt = m_Data[a];
                if (pt.time >= m_StartTime && pt.time <= m_EndTime &&
                    pt.position.x >= m_LowX && pt.position.x <= m_HighX &&
                    pt.position.y >= m_LowY && pt.position.y <= m_HighY &&
                    pt.position.z >= m_LowZ && pt.position.z <= m_HighZ)
                {
                    currentPoints++;
                    if (submaps.Count <= currentSubmap)
                    {
                        submaps.Add(new List<HeatPoint>());
                    }
                    submaps[currentSubmap].Add(pt);
                    currentSubmap = (currentPoints * verticesPerShape) / k_VerticesPerMesh;
                }
            }

            if (currentPoints == 0)
            {
                m_RenderState = k_NotRendering;
                return;
            }

            int neededSubmaps = submaps.Count;
            int currentSubmaps = m_GameObjects.Count;
            int addCount = neededSubmaps - currentSubmaps;

            if (addCount > 0)
            {
                // Add submaps if we need more
                for (int a = 0; a < addCount; a++)
                {
                    int submapID = currentSubmaps + a;
                    var go = new GameObject("Submap" + submapID);
                    go.AddComponent<HeatmapSubmap>();
                    go.GetComponent<MeshFilter>().sharedMesh = new Mesh();

                    go.transform.parent = gameObject.transform;
                    m_GameObjects.Add(go);
                }
            }
            else if (addCount < 0)
            {
                // Dispose of excess submaps
                for (var a = neededSubmaps; a < currentSubmaps; a++)
                {
                    Transform trans = gameObject.transform.Find("Submap" + a);
                    if (trans != null)
                    {
                        trans.parent = null;
                        m_GameObjects.Remove(trans.gameObject);
                        GameObject.DestroyImmediate(trans.gameObject);
                    }
                }
            }
            //Render submaps
            for (var a = 0; a < m_GameObjects.Count; a++)
            {
                Mesh renderMesh = m_GameObjects[a].GetComponent<MeshFilter>().sharedMesh;
                renderMesh.Clear();
                renderMesh.subMeshCount = submaps[a].Count;
                RenderSubmap(m_GameObjects[a], submaps[a]);
            }
            m_RenderState = k_NotRendering;
        }
    }

    void RenderSubmap(GameObject go, List<HeatPoint> submap)
    {
        var allTris = new List<int[]>();
        var allVectors = new List<Vector3[]>();
        var allColors = new List<Color32[]>();
        Vector3[] vector3 = null;
        var materials = new Material[submap.Count];

        for (int a = 0; a < submap.Count; a++)
        {
            materials[a] = m_Materials[0];
            Vector3 position = submap[a].position;
            Vector3 rotation = submap[a].rotation;
            Vector3 destination = submap[a].destination;

            switch (m_RenderStyle)
            {
                case RenderShape.Cube:
                    vector3 = AddCubeVectorsToMesh(position.x, position.y, position.z);
                    allVectors.Add(vector3);
                    allTris.Add(AddCubeTrisToMesh(a * vector3.Length));
                    break;
                case RenderShape.Arrow:
                    vector3 = AddArrowVectorsToMesh(position, rotation);
                    allVectors.Add(vector3);
                    allTris.Add(AddArrowTrisToMesh(a * vector3.Length));
                    break;
                case RenderShape.Square:
                    vector3 = AddSquareVectorsToMesh(position.x, position.y, position.z);
                    allVectors.Add(vector3);
                    allTris.Add(AddSquareTrisToMesh(a * vector3.Length));
                    break;
                case RenderShape.Triangle:
                    vector3 = AddTriVectorsToMesh(position.x, position.y, position.z);
                    allVectors.Add(vector3);
                    allTris.Add(AddTriTrisToMesh(a * vector3.Length));
                    break;
                case RenderShape.PointToPoint:
                    vector3 = AddP2PVectorsToMesh(position, destination);
                    allVectors.Add(vector3);
                    allTris.Add(AddP2PTrisToMesh(a * vector3.Length));
                    break;
            }
            allColors.Add(AddColorsToMesh(vector3.Length, submap[a]));
        }
        Vector3[] combinedVertices = allVectors.SelectMany(x => x).ToArray<Vector3>();
        Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
        mesh.vertices = combinedVertices;
        mesh.colors32 = allColors.SelectMany(x => x).ToArray<Color32>();

        for (int j = 0; j < allTris.Count; j++)
        {
            int[] t = allTris[j];
            mesh.SetTriangles(t, j);
        }
        go.GetComponent<Renderer>().materials = materials;
        go.GetComponent<HeatmapSubmap>().m_PointData = submap;
        go.GetComponent<HeatmapSubmap>().m_TrianglesPerShape = GetTrianglesForShape();

        if (m_Tips)
        {
            if (go.GetComponent<MeshCollider>() == null)
            {
                go.AddComponent<MeshCollider>();
            }

            go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }

    Color32[] AddColorsToMesh(int count, HeatPoint pt)
    {
        Color32[] colors = new Color32[count];
        float pct = (pt.density/m_MaxDensity);
        if (float.IsInfinity(pct)) {
            pct = 0f;
        }
        Color color = PickGradientColor(pct);
        for (int b = 0 ; b < count ; b++)
        {
            colors[b] = new Color (color.r, color.g, color.b, color.a) ; 
        }
        return colors;
    }

    Vector3[] AddCubeVectorsToMesh(float x, float y, float z)
    {
        float halfP = m_ParticleSize / 2;

        var p0 = new Vector3(x - halfP, y - halfP, z - halfP);
        var p1 = new Vector3(x + halfP, y - halfP, z - halfP);
        var p2 = new Vector3(x + halfP, y + halfP, z - halfP);
        var p3 = new Vector3(x - halfP, y + halfP, z - halfP);

        var p4 = new Vector3(x - halfP, y - halfP, z + halfP);
        var p5 = new Vector3(x + halfP, y - halfP, z + halfP);
        var p6 = new Vector3(x + halfP, y + halfP, z + halfP);
        var p7 = new Vector3(x - halfP, y + halfP, z + halfP);

        return new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };
    }

    // Generate a cube mesh procedurally
    int[] AddCubeTrisToMesh(int offset)
    {
        var tris = new int[]
        {
            0, 1, 2,    // bottom
            0, 2, 3,
            4, 6, 5,    // top
            4, 7, 6,
            1, 6, 2,    // right
            1, 5, 6,

            3, 4, 0,    // left
            3, 7, 4,
            2, 7, 3,    // back
            2, 6, 7,
            0, 4, 5,    // front
            0, 5, 1
        };
        for (int a = 0; a < tris.Length; a++)
        {
            tris[a] += offset;
        }
        return tris;
    }

    Vector3[] AddArrowVectorsToMesh(Vector3 position, Vector3 rotation)
    {
        float thirdP = m_ParticleSize / 3f;

        var p0 = new Vector3(-thirdP, 0f, 0f);
        var p1 = new Vector3(-thirdP, 0f, -m_ParticleSize * 2f);
        var p2 = new Vector3(-m_ParticleSize, 0f, -m_ParticleSize * 2f);
        var p3 = new Vector3(0f, 0f, -m_ParticleSize * 3f);
        var p4 = new Vector3(m_ParticleSize, 0f, -m_ParticleSize * 2f);
        var p5 = new Vector3(thirdP, 0f, -m_ParticleSize * 2f);
        var p6 = new Vector3(thirdP, 0f, 0f);

        var v = new Vector3[] { p0, p1, p2, p3, p4, p5, p6 };

        Quaternion q = Quaternion.Euler(rotation);

        for (int a = 0; a < v.Length; a++)
        {
            Matrix4x4 m = Matrix4x4.TRS(position, q, Vector3.one);
            v[a] = m.MultiplyPoint3x4(v[a]);
        }
        return v;
    }

    //Generate a pyramid mesh procedurally
    int[] AddArrowTrisToMesh(int offset)
    {
        var tris = new int[]
        {
            0, 1, 5,    // left
            6, 0, 5,    //right
            3, 4, 2	    // head
        };
        for (int a = 0; a < tris.Length; a++)
        {
            tris[a] += offset;
        }
        return tris;
    }

    Vector3[] AddSquareVectorsToMesh(float x, float y, float z)
    {
        float halfP = m_ParticleSize / 2;

        Vector3 p0, p1, p2, p3;

        switch (m_RenderDirection)
        {
            case RenderDirection.YZ:
                p0 = new Vector3(x, y - halfP, z - halfP);
                p1 = new Vector3(x, y + halfP, z - halfP);
                p2 = new Vector3(x, y + halfP, z + halfP);
                p3 = new Vector3(x, y - halfP, z + halfP);
                break;

            case RenderDirection.XZ:
                p0 = new Vector3(x - halfP, y, z - halfP);
                p1 = new Vector3(x + halfP, y, z - halfP);
                p2 = new Vector3(x + halfP, y, z + halfP);
                p3 = new Vector3(x - halfP, y, z + halfP);
                break;

            default:
                p0 = new Vector3(x - halfP, y - halfP, z);
                p1 = new Vector3(x + halfP, y - halfP, z);
                p2 = new Vector3(x + halfP, y + halfP, z);
                p3 = new Vector3(x - halfP, y + halfP, z);
                break;
        }

        return new Vector3[] { p0, p1, p2, p3 };
    }

    //Generate a procedural square
    int[] AddSquareTrisToMesh(int offset)
    {
        var tris = new int[]
        {
            offset, offset + 2, offset + 1, // top
            offset, offset + 3, offset + 2
        };
        return tris;
    }

    Vector3[] AddTriVectorsToMesh(float x, float y, float z)
    {
        float halfP = m_ParticleSize / 2;

        Vector3 p0, p1, p2;

        switch (m_RenderDirection)
        {
            case RenderDirection.YZ:
                p0 = new Vector3(x, y - halfP, z - halfP);
                p1 = new Vector3(x, y, z + halfP);
                p2 = new Vector3(x, y + halfP, z - halfP);
                break;

            case RenderDirection.XZ:
                p0 = new Vector3(x - halfP, y, z - halfP);
                p1 = new Vector3(x, y, z + halfP);
                p2 = new Vector3(x + halfP, y, z - halfP);
                break;

            default:
                p0 = new Vector3(x - halfP, y - halfP, z);
                p1 = new Vector3(x, y + halfP, z);
                p2 = new Vector3(x + halfP, y - halfP, z);
                break;
        }
        return new Vector3[] { p0, p1, p2 };
    }

    //Generate a procedural tri
    int[] AddTriTrisToMesh(int offset)
    {
        var tris = new int[]
        {
            offset, offset + 2, offset + 1  // top
        };
        return tris;
    }

    Vector3[] AddP2PVectorsToMesh(Vector3 fromVector, Vector3 toVector)
    {
        Vector3 relativePos = toVector - fromVector;
        Quaternion q = (relativePos == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(relativePos);
        float distance = Vector3.Distance(fromVector, toVector);

        float arrowBaseZ = distance - (m_ParticleSize * 1.5f);
        float halfP = m_ParticleSize * .5f;

        // base
        var p0 = new Vector3(-m_ParticleSize, 0f, -halfP);
        var p1 = new Vector3(-m_ParticleSize, 0f, halfP);
        var p2 = new Vector3(m_ParticleSize, 0f, halfP);
        var p3 = new Vector3(m_ParticleSize, 0f, -halfP);
        // stem
        var p4 = new Vector3(-.5f * m_ParticleSize, 0f, -halfP);
        var p5 = new Vector3(-.5f * m_ParticleSize, 0f, arrowBaseZ);
        var p6 = new Vector3(.5f * m_ParticleSize, 0f, arrowBaseZ);
        var p7 = new Vector3(.5f * m_ParticleSize, 0f, -halfP);
        // arrow
        var p8 = new Vector3(-m_ParticleSize, 0f, arrowBaseZ);
        var p9 = new Vector3(0f, 0f, distance);
        var p10 = new Vector3(m_ParticleSize, 0f, arrowBaseZ);
        // head
        var p11 = new Vector3(-m_ParticleSize, 0f, distance);
        var p12 = new Vector3(-m_ParticleSize, 0f, distance + halfP);
        var p13 = new Vector3(m_ParticleSize, 0f, distance + halfP);
        var p14 = new Vector3(m_ParticleSize, 0f, distance);

        var v = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14 };
        for (int a = 0; a < v.Length; a++)
        {
            Matrix4x4 m = Matrix4x4.TRS(fromVector, q, Vector3.one);
            v[a] = m.MultiplyPoint3x4(v[a]);
        }
        return v;
    }

    //Generate a procedural P2P
    int[] AddP2PTrisToMesh(int offset)
    {
        var tris = new int[]
        {
            // base
            offset, offset + 1, offset + 2,
            offset, offset + 2, offset + 3,
            // stem
            offset + 4, offset + 5, offset + 6,
            offset + 4, offset + 6, offset + 7,
            // arrow
            offset + 8, offset + 9, offset + 10,
            // head
            offset + 11, offset + 12, offset + 13,
            offset + 11, offset + 13, offset + 14
        };
        return tris;
    }

    bool hasData()
    {
        return m_Data != null && m_Data.Length > 0;
    }

    int GetVecticesForShape()
    {
        // Verts is the number of UNIQUE vertices in each shape
        int verts = 0;
        switch (m_RenderStyle)
        {
            case RenderShape.Cube:
                verts = 8;
                break;
            case RenderShape.Arrow:
                verts = 7;
                break;
            case RenderShape.Square:
                verts = 4;
                break;
            case RenderShape.Triangle:
                verts = 3;
                break;
            case RenderShape.PointToPoint:
                verts = 15;
                break;
        }
        return verts;
    }

    int GetTrianglesForShape()
    {
        // Verts is the number of UNIQUE vertices in each shape
        int verts = 0;
        switch (m_RenderStyle)
        {
            case RenderShape.Cube:
                verts = 12;
                break;
            case RenderShape.Arrow:
                verts = 3;
                break;
            case RenderShape.Square:
                verts = 2;
                break;
            case RenderShape.Triangle:
                verts = 1;
                break;
            case RenderShape.PointToPoint:
                verts = 7;
                break;
        }
        return verts;
    }

    Color PickGradientColor(float percent)
    {
        return m_Gradient.Evaluate(percent);
    }

    public static bool CompareGradients(Gradient gradient, Gradient otherGradient)
    {
        if (gradient == null || otherGradient == null)
        {
            return false;
        }

        // Compare the lengths before checking actual colors and alpha components
        if (gradient.colorKeys.Length != otherGradient.colorKeys.Length || gradient.alphaKeys.Length != otherGradient.alphaKeys.Length) {
            return false;
        }
        
        // Compare all the colors
        for (int a = 0; a < gradient.colorKeys.Length; a++)
        {
            // Test if the color and alpha is the same
            GradientColorKey key = gradient.colorKeys[a];
            GradientColorKey otherKey = otherGradient.colorKeys[a];
            if (key.color != otherKey.color || key.time != otherKey.time) {
                return false;
            }
        }
        
        // Compare all the alphas
        for (int a = 0; a < gradient.alphaKeys.Length; a++)
        {
            // Test if the color and alpha is the same
            GradientAlphaKey key = gradient.alphaKeys[a];
            GradientAlphaKey otherKey = otherGradient.alphaKeys[a];
            if (key.alpha != otherKey.alpha || key.time != otherKey.time)
            {
                return false;
            }
        }
        
        // They're the same
        return true;
    }
}
