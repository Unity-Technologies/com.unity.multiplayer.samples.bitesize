/// <summary>
/// Heat map data parser.
/// </summary>
/// This portion of the Heatmapper opens a JSON file and processes it into an array
/// of point data.
/// OnGUI functionality displays the state of the data in the Heatmapper inspector.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnalyticsHeatmap
{
    public class HeatmapDataParserInspector
    {
        const string k_DataPathKey = "UnityAnalyticsHeatmapDataPath";

        string m_Path = "";

        Dictionary<string, HeatPoint[]> m_HeatData;
        Vector3 m_LowSpace;
        Vector3 m_HighSpace;

        bool m_KeyFound = true;
        int m_OptionIndex = 0;
        string[] m_OptionKeys;

        List<List<string>> m_SeparatedLists;
        List<int> m_Options;

        public delegate void PointHandler(HeatPoint[] heatData);

        PointHandler m_PointHandler;

        HeatmapDataParser m_DataParser = new HeatmapDataParser();


        public HeatmapDataParserInspector(PointHandler handler)
        {
            m_PointHandler = handler;
            m_Path = EditorPrefs.GetString(k_DataPathKey);
        }

        public static HeatmapDataParserInspector Init(PointHandler handler)
        {
            return new HeatmapDataParserInspector(handler);
        }

        void Dispatch()
        {
            m_KeyFound = true;
            string key = BuildKey();
            if (m_HeatData.ContainsKey(key))
            {
                m_PointHandler(m_HeatData[key]);
            }
            else
            {
                m_KeyFound = false;
            }
        }

        public void SetDataPath(string jsonPath)
        {
            m_Path = jsonPath;
            m_DataParser.LoadData(m_Path, ParseHandler);
        }

        public void OnGUI()
        {
            if (m_HeatData != null && m_OptionKeys != null && m_OptionIndex > -1 && m_OptionIndex < m_OptionKeys.Length && m_HeatData.ContainsKey(m_OptionKeys[m_OptionIndex]))
            {
                string oldKey = BuildKey();

                for(int a = 0; a < m_SeparatedLists.Count; a++)
                {
                    var listArray = m_SeparatedLists[a].ToArray();
                    m_Options[a] = EditorGUILayout.Popup(m_Options[a], listArray);
                }
                if (BuildKey() != oldKey)
                {
                    Dispatch();
                }
            }
            if (!m_KeyFound)
            {
                EditorGUILayout.HelpBox("No matching combination.", MessageType.Warning);
            }
        }

        void ParseHandler(Dictionary<string, HeatPoint[]> heatData, string[] options)
        {
            m_HeatData = heatData;
            if (heatData != null)
            {
                if (m_OptionKeys != null)
                {
                    string opt = m_OptionIndex > m_OptionKeys.Length ? "" : m_OptionKeys[m_OptionIndex];
                    ArrayList list = new ArrayList(options);
                    int idx = list.IndexOf(opt);
                    m_OptionIndex = idx == -1 ? 0 : idx;
                }
                else
                {
                    m_OptionIndex = 0;
                }
                ParseOptionList(options);
                m_OptionKeys = options;
                Dispatch();
            }
        }

        void ParseOptionList(string[] options)
        {
            string[] oldKey = BuildKey().Split('~');
            m_SeparatedLists = new List<List<string>>();
            m_Options = new List<int>();

            foreach(string opt in options)
            {
                string[] parts = opt.Split('~');

                for (int a = 0; a < parts.Length; a++)
                {
                    if (m_SeparatedLists.Count <= a)
                    {
                        m_SeparatedLists.Add(new List<string>());
                    }
                    if (m_SeparatedLists[a].IndexOf(parts[a]) == -1)
                    {
                        m_SeparatedLists[a].Add(parts[a]);
                    }
                }
            }
            for (int a = 0; a < m_SeparatedLists.Count; a++)
            {
                // Restore old indices when possible
                int index = 0;
                if (oldKey.Length > a)
                {
                    index = m_SeparatedLists[a].IndexOf(oldKey[a]);
                    index = Math.Max(0, index);
                }
                m_Options.Add(index);
            }
        }

        string BuildKey()
        {
            string retv = "";
            if (m_SeparatedLists != null)
            {
                for (int a = 0; a < m_SeparatedLists.Count; a++)
                {
                    retv += m_SeparatedLists[a][m_Options[a]];
                    if (a < m_SeparatedLists.Count - 1)
                    {
                        retv += "~";
                    }
                }
            }
            return retv;
        }
    }
}
