using UnityEngine;
using UnityEditor;

namespace Tanks.Editor
{
	public class HalfLambertPowerupShaderGUI : ShaderGUI
	{
		private static class Styles
		{
			public static GUIStyle s_OptionsButton = "PaneOptions";
			public static GUIContent s_UVSetLabel = new GUIContent("UV Set");

			public static string s_EmptyTootip = "";
			public static GUIContent s_AlbedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
			public static GUIContent s_GlowText = new GUIContent("Glow", "Scrolling glow and rim colour (RGB)");
			public static GUIContent s_NormalMapText = new GUIContent("Normal Map", "Normal Map");

			public static string s_WhiteSpaceString = " ";
			public static string s_PrimaryMapsText = "Main Maps";
		}

		MaterialProperty albedoMap = null;
		MaterialProperty albedoColor = null;
		MaterialProperty highlightMap = null;
		MaterialProperty highlightColor = null;
		MaterialProperty highlightScroll = null;
		MaterialProperty metallic = null;
		MaterialProperty smoothness = null;
		MaterialProperty bumpMap = null;

		MaterialEditor m_MaterialEditor;

		bool m_FirstTimeApply = true;

		public void FindProperties(MaterialProperty[] props)
		{
			albedoMap = FindProperty("_MainTex", props);
			albedoColor = FindProperty("_Color", props);
			highlightMap = FindProperty("_HighlightTex", props);
			highlightColor = FindProperty("_HighlightColour", props);
			highlightScroll = FindProperty("_GlowScroll", props);
			metallic = FindProperty("_Metallic", props, false);
			smoothness = FindProperty("_Glossiness", props);
			bumpMap = FindProperty("_BumpMap", props);
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			ShaderPropertiesGUI(material);

			// Make sure that needed keywords are set up if we're switching some existing
			// material to a standard shader.
			if (m_FirstTimeApply)
			{
				SetMaterialKeywords(material);
				m_FirstTimeApply = false;
			}
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;

			// Detect any changes to the material
			EditorGUI.BeginChangeCheck();
			{
				// Primary properties
				GUILayout.Label(Styles.s_PrimaryMapsText, EditorStyles.boldLabel);
				DoAlbedoArea();
				m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
				m_MaterialEditor.TexturePropertySingleLine(Styles.s_NormalMapText, bumpMap, null);
				DoSpecularMetallicArea();

				EditorGUILayout.Space();
			}
			if (EditorGUI.EndChangeCheck())
			{
				MaterialChanged(material);
			}
		}

		void DoAlbedoArea()
		{
			m_MaterialEditor.TexturePropertySingleLine(Styles.s_AlbedoText, albedoMap, albedoColor);
			m_MaterialEditor.TexturePropertySingleLine(Styles.s_GlowText, highlightMap, highlightColor);
			m_MaterialEditor.VectorProperty(highlightScroll, "Scroll direction");
		}

		void DoSpecularMetallicArea()
		{
			m_MaterialEditor.RangeProperty(metallic, "Metallic");
			m_MaterialEditor.RangeProperty(smoothness, "Smoothness");
		}

		static void SetMaterialKeywords(Material material)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
			// (MaterialProperty value might come from renderer material property block)
			SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
			SetKeyword(material, "_ALBEDOMAP", material.GetTexture("_MainTex"));
		}

		static void MaterialChanged(Material material)
		{
			SetMaterialKeywords(material);
		}

		static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}
	}
}
