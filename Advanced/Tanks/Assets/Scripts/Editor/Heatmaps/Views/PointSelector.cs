
using System;
using UnityEditor;
using UnityEngine;
using UnityAnalyticsHeatmap;


[CustomEditor(typeof(HeatmapSubmap))]
public class PointSelector : Editor
{

    Vector3 m_LabelPosition;
    HeatmapSubmap m_Target;
    HeatPoint m_Point = new HeatPoint();
    int m_DisplayTime = 0;
    int m_MaxDisplayTime = 10;


    float m_TooltipWidth = 180f;
    float m_TooltipHeight = 70f;
    GUIStyle m_BgStyle = new GUIStyle();
    GUIStyle m_TextStyle = new GUIStyle();

    void OnEnable()
    {
        m_Target = (HeatmapSubmap)target;
        m_BgStyle.normal.background = MakeTex((int)m_TooltipWidth, (int)m_TooltipHeight, new Color(0f, 0f, 0f, 0.5f));
        m_TextStyle.normal.textColor = Color.white;
        m_TextStyle.contentOffset = new Vector2(2f,2f);
    }

    void OnSceneGUI()
    {
        if (m_Target.GetComponent<MeshCollider>() == null || m_Target.m_PointData == null)
        {
            return;
        }

        if (m_DisplayTime > 0)
        {
            m_DisplayTime --;
            Handles.BeginGUI();

            string text = BuildText(m_Point);
            GUIContent content = new GUIContent(text);

            Vector2 size = m_TextStyle.CalcSize(content) * 1.05f;


            float xPos = m_LabelPosition.x > Screen.width * .5f ? m_LabelPosition.x - size.x - 10f : m_LabelPosition.x + 10f;
            float yPos = m_LabelPosition.y > Screen.height * .5f ? m_LabelPosition.y - size.y - 10f : m_LabelPosition.y + 10f;


            GUILayout.BeginArea(new Rect(xPos, yPos, size.x, size.y), m_BgStyle);
            GUILayout.Label(content, m_TextStyle);
            GUILayout.EndArea();
            Handles.EndGUI();
        }
        if (Event.current != null && Event.current.type == EventType.MouseMove)
        {
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (m_Target.GetComponent<MeshCollider>().Raycast(ray, out hit, float.MaxValue))
            {
                int idx = hit.triangleIndex / m_Target.m_TrianglesPerShape;
                m_Point = m_Target.m_PointData[idx];
                m_LabelPosition = Event.current.mousePosition;
                m_DisplayTime = m_MaxDisplayTime;
            }
        }
    }

    string BuildText(HeatPoint pt)
    {
        string text = "Position x: " + pt.position.x;
        text +=  " y: " + pt.position.y;
        text +=  " z: " + pt.position.z + "\n";
        text += "Rotation x: " + pt.position.x;
        text +=  " y: " + pt.position.y;
        text +=  " z: " + pt.position.z + "\n";
        text += "Destination x: " + pt.destination.x;
        text +=  " y: " + pt.destination.y;
        text +=  " z: " + pt.destination.z + "\n";
        text += "Time: " + pt.time + "\n";
        text += "Density: " + pt.density;
        return text;
    }

    Texture2D MakeTex(int width, int height, Color color) {
        var pixels = new Color[width * height];
        for (int a = 0; a < pixels.Length; a++) {
            pixels[a] = color;
        }
        var result = new Texture2D(width, height);
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }
}
