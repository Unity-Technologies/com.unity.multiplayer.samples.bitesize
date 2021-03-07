/// <summary>
/// Heatmapper inspector.
/// </summary>
/// This code drives the Heatmapper inspector
/// The HeatmapDataParser handles loading and parsing the data.
/// The HeatmapRendererInspector speaks to the Renderer to achieve a desired look.

using System.Collections.Generic;
using UnityAnalyticsHeatmap;
using UnityEditor;
using UnityEngine;

public class Heatmapper : EditorWindow
{

    [MenuItem("Window/Unity Analytics/Heatmapper #%h")]
    static void HeatmapperMenuOption()
    {
        EditorWindow.GetWindow(typeof(Heatmapper));
    }

    public Heatmapper()
    {
        m_DataPath = "";
        m_Aggregator = new HeatmapAggregator(m_DataPath);
    }

    // Views
    AggregationInspector m_AggregationView;
    HeatmapDataParserInspector m_ParserView;
    HeatmapRendererInspector m_RenderView;

    // Data handlers
    HeatmapAggregator m_Aggregator;

    GameObject m_HeatMapInstance;

    bool m_ShowAggregate = false;
    bool m_ShowRender = false;

    HeatPoint[] m_HeatData;
    string m_DataPath = "";

    Vector2 m_ScrollPosition;

    void OnEnable()
    {
        m_RenderView = HeatmapRendererInspector.Init(this);
        m_AggregationView = AggregationInspector.Init(m_Aggregator);
        m_ParserView = HeatmapDataParserInspector.Init(OnPointData);
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.Layout)
        {
            if (m_HeatMapInstance == null)
            {
                AttemptReconnectWithHeatmapInstance();
            }
            if (m_RenderView != null)
            {
                m_RenderView.SetGameObject(m_HeatMapInstance);
            }
        }

        using (var scroll = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
        {
            m_ScrollPosition = scroll.scrollPosition;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset"))
                    {
                        SystemReset();
                    }
                    if (GUILayout.Button("Documentation"))
                    {
                        Application.OpenURL("https://bitbucket.org/Unity-Technologies/heatmaps/wiki/Home");
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                m_ShowAggregate = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_ShowAggregate, "Data", true);
                if (m_ShowAggregate)
                {
                    m_AggregationView.OnGUI();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Process"))
                        {
                            SystemProcess();
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                m_ShowRender = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), m_ShowRender, "Render", true);
                if (m_ShowRender && m_ParserView != null)
                {
                    m_ParserView.OnGUI();
                    m_RenderView.OnGUI();
                }
            }
        }
    }

    void Update()
    {
        if (m_HeatMapInstance != null)
        {
            m_HeatMapInstance.GetComponent<IHeatmapRenderer>().RenderHeatmap();
        }
        if (m_RenderView != null)
        {
            m_RenderView.Update();
        }

        if (m_HeatData != null)
        {
            if (m_HeatMapInstance == null)
            {
                CreateHeatmapInstance();
            }

            if (m_RenderView != null)
            {
                m_RenderView.SetGameObject(m_HeatMapInstance);
                m_RenderView.SetLimits(m_HeatData);

                m_RenderView.Update(true);
            }

            m_HeatData = null;
        }
    }

    void SystemProcess()
    {
        if (m_HeatMapInstance == null)
        {
            CreateHeatmapInstance();
        }
        if (m_AggregationView != null)
        {
            m_AggregationView.Fetch(OnAggregation, true);
        }
    }

    void SystemReset()
    {
        if (m_AggregationView != null) {
            m_AggregationView.SystemReset();
        }
        if (m_RenderView != null) {
            m_RenderView.SystemReset();
        }
        if (m_HeatMapInstance)
        {
            m_HeatMapInstance.transform.parent = null;
            DestroyImmediate(m_HeatMapInstance);
        }
    }

    void OnAggregation(string jsonPath)
    {
        m_ParserView.SetDataPath(jsonPath);
    }

    void OnPointData(HeatPoint[] heatData)
    {
        // Creating this data allows the renderer to use it on the next Update pass
        m_HeatData = heatData;
    }

    /// <summary>
    /// Creates the heat map instance.
    /// </summary>
    /// We've hard-coded the Component here. Everywhere else, we use the interface.
    /// If you want to write a custom Renderer, this is the place to sub it in.
    void CreateHeatmapInstance()
    {
        m_HeatMapInstance = new GameObject();
        m_HeatMapInstance.tag = "EditorOnly";
        m_HeatMapInstance.name = "UnityAnalytics__Heatmap";
        m_HeatMapInstance.AddComponent<HeatmapMeshRenderer>();
        m_HeatMapInstance.GetComponent<IHeatmapRenderer>().allowRender = true;
    }

    /// <summary>
    /// Attempts to reconnect with a heatmap instance.
    /// </summary>
    void AttemptReconnectWithHeatmapInstance()
    {
        m_HeatMapInstance = GameObject.Find("UnityAnalytics__Heatmap");
    }
}
