/// <summary>
/// Inspector for the Aggregation portion of the Heatmapper.
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityAnalytics;
using System.Linq;

namespace UnityAnalyticsHeatmap
{
    public class AggregationInspector
    {
        const string k_UrlKey = "UnityAnalyticsHeatmapDataExportUrlKey";
        const string k_DataPathKey = "UnityAnalyticsHeatmapDataPathKey";
        const string k_UseCustomDataPathKey = "UnityAnalyticsHeatmapUsePersistentDataPathKey";

        const string k_SpaceKey = "UnityAnalyticsHeatmapAggregationSpace";
        const string k_KeyToTime = "UnityAnalyticsHeatmapAggregationTime";
        const string k_RotationKey = "UnityAnalyticsHeatmapAggregationRotation";
        const string k_SmoothSpaceKey = "UnityAnalyticsHeatmapAggregationAggregateSpace";
        const string k_SmoothTimeKey = "UnityAnalyticsHeatmapAggregationAggregateTime";
        const string k_SmoothRotationKey = "UnityAnalyticsHeatmapAggregationAggregateRotation";

        const string k_SeparateUsersKey = "UnityAnalyticsHeatmapAggregationAggregateUserIDs";
        const string k_SeparateSessionKey = "UnityAnalyticsHeatmapAggregationAggregateSessionIDs";
        const string k_SeparateDebugKey = "UnityAnalyticsHeatmapAggregationAggregateDebug";
        const string k_SeparatePlatformKey = "UnityAnalyticsHeatmapAggregationAggregatePlatform";
        const string k_SeparateCustomKey = "UnityAnalyticsHeatmapAggregationAggregateCustom";

        const string k_ArbitraryFieldsKey = "UnityAnalyticsHeatmapAggregationArbitraryFields";
        const string k_EventsKey = "UnityAnalyticsHeatmapAggregationEvents";

        const string k_RemapColorKey = "UnityAnalyticsHeatmapRemapColorKey";
        const string k_RemapOptionIndexKey = "UnityAnalyticsHeatmapRemapOptionIndexKey";
        const string k_RemapColorFieldKey = "UnityAnalyticsHeatmapRemapColorFieldKey";

        const float k_DefaultSpace = 10f;
        const float k_DefaultTime = 10f;
        const float k_DefaultRotation = 15f;

        string m_RawDataPath = "";
        string m_DataPath = "";
        bool m_UseCustomDataPath = true;

        Dictionary<string, HeatPoint[]> m_HeatData;

        public delegate void AggregationHandler(string jsonPath);

        AggregationHandler m_AggregationHandler;

        HeatmapAggregator m_Aggregator;

        private GUIContent[] m_SmootherOptionsContent;
        Texture2D darkSkinUnionIcon = EditorGUIUtility.Load("Assets/Editor/Heatmaps/Textures/union_dark.png") as Texture2D;
        Texture2D darkSkinNumberIcon = EditorGUIUtility.Load("Assets/Editor/Heatmaps/Textures/number_dark.png") as Texture2D;
        Texture2D darkSkinNoneIcon = EditorGUIUtility.Load("Assets/Editor/Heatmaps/Textures/none_dark.png") as Texture2D;

        Texture2D lightSkinUnionIcon = EditorGUIUtility.Load("Assets/Editor/Heatmaps/Textures/union_light.png") as Texture2D;
        Texture2D lightSkinNumberIcon = EditorGUIUtility.Load("Assets/Editor/Heatmaps/Textures/number_light.png") as Texture2D;
        Texture2D lightSkinNoneIcon = EditorGUIUtility.Load("Assets/Editor/Heatmaps/Textures/none_light.png") as Texture2D;

        private GUIContent m_UseCustomDataPathContent = new GUIContent("Use custom data path", "By default, will use Application.persistentDataPath");
        private GUIContent m_DataPathContent = new GUIContent("Input path", "Where to retrieve data (defaults to Application.persistentDataPath");
        private GUIContent m_DatesContent = new GUIContent("Dates", "ISO-8601 datetimes (YYYY-MM-DD)");
        private GUIContent m_AddFieldContent = new GUIContent("+", "Add field");
        private GUIContent m_RemapColorContent = new GUIContent("Remap color to field", "By default, heatmap color is determined by event density. Checking this box allows you to remap to a specific field (e.g., use to identify fps drops.)");
        private GUIContent m_RemapColorFieldContent = new GUIContent("Field","Name the field to remap");
        private GUIContent m_RemapOptionIndexContent = new GUIContent("Remap operation", "How should the remapped variable aggregate?");
        private GUIContent m_PercentileContent = new GUIContent("Percentile", "A value between 0 and 100");

        string m_StartDate = "";
        string m_EndDate = "";
        float m_Space = k_DefaultSpace;
        float m_Time = k_DefaultTime;
        float m_Rotation = k_DefaultRotation;

        const int SMOOTH_VALUE = 0;
        const int SMOOTH_NONE = 1;
        const int SMOOTH_UNION = 2;

        int m_SmoothSpaceToggle = SMOOTH_VALUE;
        int m_SmoothTimeToggle = SMOOTH_UNION;
        int m_SmoothRotationToggle = SMOOTH_UNION;

        bool m_SeparateUsers = false;
        bool m_SeparateSessions = false;
        bool m_SeparatePlatform = false;
        bool m_SeparateDebug = false;
        bool m_SeparateCustomField = false;

        bool m_RemapColor;
        string m_RemapColorField = "";
        int m_RemapOptionIndex = 0;
        float m_Percentile = 50f;
        GUIContent[] m_RemapOptions = new GUIContent[]{
            new GUIContent("Increment"),
            new GUIContent("Cumulative"),
            new GUIContent("Average"),
            new GUIContent("Min"),
            new GUIContent("Max"),
            new GUIContent("First"),
            new GUIContent("Last"),
            new GUIContent("Percentile")
        };
        AggregationMethod[] m_RemapOptionIds = new AggregationMethod[]{
            AggregationMethod.Increment,
            AggregationMethod.Cumulative,
            AggregationMethod.Average,
            AggregationMethod.Min,
            AggregationMethod.Max,
            AggregationMethod.First,
            AggregationMethod.Last,
            AggregationMethod.Percentile
        };

        List<string> m_ArbitraryFields = new List<string>{ };

        public AggregationInspector(HeatmapAggregator aggregator)
        {
            m_Aggregator = aggregator;

            // Restore cached paths
            m_RawDataPath = EditorPrefs.GetString(k_UrlKey);
            m_UseCustomDataPath = EditorPrefs.GetBool(k_UseCustomDataPathKey);
            m_DataPath = EditorPrefs.GetString(k_DataPathKey);

            // Set dates based on today (should this be cached?)
            m_EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.UtcNow);
            m_StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.UtcNow.Subtract(new TimeSpan(5, 0, 0, 0)));

            // Restore other options
            m_Space = EditorPrefs.GetFloat(k_SpaceKey) == 0 ? k_DefaultSpace : EditorPrefs.GetFloat(k_SpaceKey);
            m_Time = EditorPrefs.GetFloat(k_KeyToTime) == 0 ? k_DefaultTime : EditorPrefs.GetFloat(k_KeyToTime);
            m_Rotation = EditorPrefs.GetFloat(k_RotationKey) == 0 ? k_DefaultRotation : EditorPrefs.GetFloat(k_RotationKey);
            m_SmoothSpaceToggle = EditorPrefs.GetInt(k_SmoothSpaceKey);
            m_SmoothTimeToggle = EditorPrefs.GetInt(k_SmoothTimeKey);
            m_SmoothRotationToggle = EditorPrefs.GetInt(k_SmoothRotationKey);
            m_SeparateUsers = EditorPrefs.GetBool(k_SeparateUsersKey);
            m_RemapColor = EditorPrefs.GetBool(k_RemapColorKey);
            m_RemapColorField = EditorPrefs.GetString(k_RemapColorFieldKey);
            m_RemapOptionIndex = EditorPrefs.GetInt(k_RemapOptionIndexKey);

            // Restore list of arbitrary separation fields
            string loadedArbitraryFields = EditorPrefs.GetString(k_ArbitraryFieldsKey);
            string[] arbitraryFieldsList;
            if (string.IsNullOrEmpty(loadedArbitraryFields))
            {
                arbitraryFieldsList = new string[]{ };
            }
            else
            {
                arbitraryFieldsList = loadedArbitraryFields.Split('|');
            }
            m_ArbitraryFields = new List<string>(arbitraryFieldsList);


            var unionIcon = lightSkinUnionIcon;
            var smoothIcon = lightSkinNumberIcon;
            var noneIcon = lightSkinNoneIcon;
            if (EditorPrefs.GetInt("UserSkin") == 1)
            {
                unionIcon = darkSkinUnionIcon;
                smoothIcon = darkSkinNumberIcon;
                noneIcon = darkSkinNoneIcon;
            }

            m_SmootherOptionsContent = new GUIContent[] {
                new GUIContent(smoothIcon, "Smooth to value"),
                new GUIContent(noneIcon, "No smoothing"),
                new GUIContent(unionIcon, "Unify all")
            };
        }

        public static AggregationInspector Init(HeatmapAggregator aggregator)
        {
            return new AggregationInspector(aggregator);
        }

        public void SystemReset()
        {
            //TODO
        }

        public void Fetch(AggregationHandler handler, bool localOnly)
        {
            m_AggregationHandler = handler;

            EditorPrefs.SetString(k_UrlKey, m_RawDataPath);
            DateTime start, end;
            try
            {
                start = DateTime.Parse(m_StartDate).ToUniversalTime();
            }
            catch
            {
                throw new Exception("The start date is not properly formatted. Correct format is YYYY-MM-DD.");
            }
            try
            {
                // Add one day to include the whole of that day
                end = DateTime.Parse(m_EndDate).ToUniversalTime().Add(new TimeSpan(24, 0, 0));
            }
            catch
            {
                throw new Exception("The end date is not properly formatted. Correct format is YYYY-MM-DD.");
            }

            RawDataClient.GetInstance().m_DataPath = m_DataPath;
            var fileList = RawDataClient.GetInstance().GetFiles(new UnityAnalyticsEventType[]{ UnityAnalyticsEventType.custom }, start, end);
            ProcessAggregation(fileList);
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            bool oldUseCustomDataPath = m_UseCustomDataPath;
            m_UseCustomDataPath = EditorGUILayout.Toggle(m_UseCustomDataPathContent, m_UseCustomDataPath);
            if (oldUseCustomDataPath != m_UseCustomDataPath)
            {
                EditorPrefs.SetBool(k_UseCustomDataPathKey, m_UseCustomDataPath);
            }
            if (GUILayout.Button("Open Folder"))
            {
                EditorUtility.RevealInFinder(m_DataPath);
            }
            GUILayout.EndHorizontal();

            if (!m_UseCustomDataPath)
            {
                m_DataPath = Application.persistentDataPath;
            }
            else
            {
                string oldDataPath = m_DataPath;
                m_DataPath = EditorGUILayout.TextField(m_DataPathContent, m_DataPath);
                if (string.IsNullOrEmpty(m_DataPath))
                {
                    m_DataPath = Application.persistentDataPath;
                }
                if (oldDataPath != m_DataPath )
                {
                    EditorPrefs.SetString(k_DataPathKey, m_DataPath);
                }
            }

            m_Aggregator.SetDataPath(m_DataPath);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_DatesContent, GUILayout.Width(35));
            m_StartDate = EditorGUILayout.TextField(m_StartDate);
            EditorGUILayout.LabelField("-", GUILayout.Width(10));
            m_EndDate = EditorGUILayout.TextField(m_EndDate);
            GUILayout.EndHorizontal();


            // SMOOTHERS (SPACE, ROTATION, TIME)
            GUILayout.BeginVertical("box");
            GUILayout.Label("Smooth/Unionize", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            // SPACE
            SmootherControl(ref m_SmoothSpaceToggle, ref m_Space, "Space", "Divider to smooth out x/y/z data", k_SmoothSpaceKey, k_SpaceKey, 2);
            // ROTATION
            SmootherControl(ref m_SmoothRotationToggle, ref m_Rotation, "Rotation", "Divider to smooth out angular data", k_SmoothRotationKey, k_RotationKey);
            // TIME
            SmootherControl(ref m_SmoothTimeToggle, ref m_Time, "Time", "Divider to smooth out passage of game time", k_SmoothTimeKey, k_KeyToTime);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // SEPARATION
            GUILayout.Label("Separate", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GroupControl(ref m_SeparateUsers,
                "Users", "Separate each user into their own list. NOTE: Separating user IDs can be quite slow!",
                k_SeparateUsersKey);
            GroupControl(ref m_SeparateSessions,
                "Sessions", "Separate each session into its own list. NOTE: Separating unique sessions can be astonishly slow!",
                k_SeparateSessionKey);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GroupControl(ref m_SeparateDebug,
                "Is Debug", "Separate debug devices from non-debug devices",
                k_SeparateDebugKey);
            GroupControl(ref m_SeparatePlatform,
                "Platform", "Separate data based on platform",
                k_SeparatePlatformKey);
            GUILayout.EndHorizontal();


            GroupControl(ref m_SeparateCustomField,
                "On Custom Field", "Separate based on one or more parameter fields",
                k_SeparateCustomKey);


            if (m_SeparateCustomField)
            {
                string oldArbitraryFieldsString = string.Join("|", m_ArbitraryFields.ToArray());
                if (m_ArbitraryFields.Count == 0)
                {
                    m_ArbitraryFields.Add("Field name");
                }
                for (var a = 0; a < m_ArbitraryFields.Count; a++)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("-", GUILayout.MaxWidth(20f)))
                    {
                        m_ArbitraryFields.RemoveAt(a);
                        break;
                    }
                    m_ArbitraryFields[a] = EditorGUILayout.TextField(m_ArbitraryFields[a]);
                    if (a == m_ArbitraryFields.Count-1 && GUILayout.Button(m_AddFieldContent))
                    {
                        m_ArbitraryFields.Add("Field name");
                    }
                    GUILayout.EndHorizontal();
                }
                string currentArbitraryFieldsString = string.Join("|", m_ArbitraryFields.ToArray());
                if (oldArbitraryFieldsString != currentArbitraryFieldsString)
                {
                    EditorPrefs.SetString(k_ArbitraryFieldsKey, currentArbitraryFieldsString);
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            bool oldRemapColor = m_RemapColor;
            m_RemapColor = EditorGUILayout.Toggle(m_RemapColorContent, m_RemapColor);
            if (oldRemapColor != m_RemapColor)
            {
                EditorPrefs.SetBool(k_RemapColorKey, m_RemapColor);
            }
            if (m_RemapColor)
            {
                string oldRemapField = m_RemapColorField;
                int oldOptionIndex = m_RemapOptionIndex;
                m_RemapColorField = EditorGUILayout.TextField(m_RemapColorFieldContent, m_RemapColorField);
                m_RemapOptionIndex = EditorGUILayout.Popup(m_RemapOptionIndexContent, m_RemapOptionIndex, m_RemapOptions);

                if (m_RemapOptionIds[m_RemapOptionIndex] == AggregationMethod.Percentile)
                {
                    m_Percentile = Mathf.Clamp(EditorGUILayout.FloatField(m_PercentileContent, m_Percentile), 0, 100f);
                }
                if (oldRemapField != m_RemapColorField)
                {
                    EditorPrefs.SetString(k_RemapColorFieldKey, m_RemapColorField);
                }
                if (oldOptionIndex != m_RemapOptionIndex)
                {
                    EditorPrefs.SetInt(k_RemapOptionIndexKey, m_RemapOptionIndex);
                }
            }
            GUILayout.EndVertical();
        }

        void SmootherControl(ref int toggler, ref float value, string label, string tooltip, string toggleKey, string valueKey, int endIndex = -1)
        {
            GUILayout.BeginVertical();

            var options = endIndex == -1 ? m_SmootherOptionsContent : 
                m_SmootherOptionsContent.Take(endIndex).ToArray();

            int oldToggler = toggler;
            toggler = GUILayout.Toolbar(
                toggler, options, GUILayout.MaxWidth(100));
            float oldValue = value;

            float lw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;
            float fw = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.fieldWidth = 20;
            EditorGUI.BeginDisabledGroup(toggler != SMOOTH_VALUE);
            value = EditorGUILayout.FloatField(new GUIContent(label, tooltip), value);
            value = Mathf.Max(0, value);
            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = lw;
            EditorGUIUtility.fieldWidth = fw;

            if (oldValue != value || oldToggler != toggler)
            {
                EditorPrefs.SetInt(toggleKey, toggler);
                EditorPrefs.SetFloat(valueKey, value);
            }
            GUILayout.EndVertical();
        }

        void GroupControl(ref bool groupParam, string label, string tooltip, string key)
        {
            bool oldValue = groupParam;
            groupParam = EditorGUILayout.Toggle(new GUIContent(label, tooltip), groupParam);
            if (groupParam != oldValue)
            {
                EditorPrefs.SetBool(key, groupParam);
            }
        }

        void ProcessAggregation(List<string> fileList)
        {
            if (fileList.Count == 0)
            {
                Debug.LogWarning("No matching data found.");
            }
            else
            {
                DateTime start, end;
                try
                {
                    start = DateTime.Parse(m_StartDate).ToUniversalTime();
                }
                catch
                {
                    start = DateTime.Parse("2000-01-01").ToUniversalTime();
                }
                try
                {
                    end = DateTime.Parse(m_EndDate).ToUniversalTime().Add(new TimeSpan(24,0,0));
                }
                catch
                {
                    end = DateTime.UtcNow;
                }
                if (m_RemapColor && string.IsNullOrEmpty(m_RemapColorField))
                {
                    Debug.LogWarning("You have selected 'Remap color to field' but haven't specified a field name. No remapping can occur.");
                }

                // When these are the same, points where these values match will be aggregated to the same point
                var aggregateOn = new List<string>(){ "x", "y", "z", "t", "rx", "ry", "rz", "dx", "dy", "dz", "z" };
                // Specify groupings for unique lists
                var groupOn = new List<string>(){ "eventName" };

                // userID is optional
                if (m_SeparateUsers)
                {
                    aggregateOn.Add("userID");
                    groupOn.Add("userID");
                }
                if (m_SeparateSessions)
                {
                    aggregateOn.Add("sessionID");
                    groupOn.Add("sessionID");
                }
                if (m_SeparateDebug)
                {
                    aggregateOn.Add("debug");
                    groupOn.Add("debug");
                }
                if (m_SeparatePlatform)
                {
                    aggregateOn.Add("platform");
                    groupOn.Add("platform");
                }
                // Arbitrary Fields are included if specified
                if (m_SeparateCustomField)
                {
                    aggregateOn.AddRange(m_ArbitraryFields);
                    groupOn.AddRange(m_ArbitraryFields);
                }

                // Specify smoothing properties (must be a subset of aggregateOn)
                var smoothOn = new Dictionary<string, float>();
                // Smooth space
                if (m_SmoothSpaceToggle == SMOOTH_VALUE || m_SmoothSpaceToggle == SMOOTH_NONE)
                {
                    float spaceSmoothValue = (m_SmoothSpaceToggle == SMOOTH_NONE) ? 0f : m_Space;
                    smoothOn.Add("x", spaceSmoothValue);
                    smoothOn.Add("y", spaceSmoothValue);
                    smoothOn.Add("z", spaceSmoothValue);
                    smoothOn.Add("dx", spaceSmoothValue);
                    smoothOn.Add("dy", spaceSmoothValue);
                    smoothOn.Add("dz", spaceSmoothValue);
                }
                // Smooth rotation
                if (m_SmoothRotationToggle == SMOOTH_VALUE || m_SmoothRotationToggle == SMOOTH_NONE)
                {
                    float rotationSmoothValue = (m_SmoothRotationToggle == SMOOTH_NONE) ? 0f : m_Rotation;
                    smoothOn.Add("rx", rotationSmoothValue);
                    smoothOn.Add("ry", rotationSmoothValue);
                    smoothOn.Add("rz", rotationSmoothValue);
                }
                // Smooth time
                if (m_SmoothTimeToggle == SMOOTH_VALUE || m_SmoothTimeToggle == SMOOTH_NONE)
                {
                    float timeSmoothValue = (m_SmoothTimeToggle == SMOOTH_NONE) ? 0f : m_Time;
                    smoothOn.Add("t", timeSmoothValue);
                }

                string remapToField = m_RemapColor ? m_RemapColorField : "";
                int remapOption = m_RemapColor ? m_RemapOptionIndex : 0;

                m_Aggregator.Process(aggregationHandler, fileList, start, end,
                    aggregateOn, smoothOn, groupOn,
                    remapToField, m_RemapOptionIds[remapOption], m_Percentile);
            }
        }

        void aggregationHandler(string jsonPath)
        {
            m_AggregationHandler(jsonPath);
        }
    }
}
