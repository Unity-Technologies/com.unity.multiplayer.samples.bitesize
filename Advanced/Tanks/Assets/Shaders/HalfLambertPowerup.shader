Shader "Custom/HalfLambert-Powerup" {
	Properties {
		_MainTex("Main Tex", 2D) = "white" {}
		_HighlightTex("Highlight", 2D) = "white" {}
		_GlowScroll("ScrollDir", Vector) = (0.1, -0.5, 0, 0)
		_Color ("Color", Color) = (1,1,1,1)
		_HighlightColour ("Highlight Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_LUT ("LightLookup", 2D) = "white"
		_BumpMap("Normal Map", 2D) = "bump" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf CustomStandard fullforwardshadows vertex:vert
		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _ALBEDOMAP

		#include "UnityPBSLighting.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input {
			half2 uv_MainTex;
			half2 uv_BumpMap;
			half2 glowUV;
		};

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _LUT;
		sampler2D _HighlightTex;
		fixed4 _HighlightColour;
		fixed4 _Color;
		half _Metallic;
		half _Glossiness;
		half2 _GlowScroll;

		void vert (inout appdata_full v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.glowUV = (half2)v.texcoord + (half)_Time.y * _GlowScroll.xy;
		}

		// Modified BRDF3
		half3 BRDF3_Direct_Toon(half3 diffColor, half3 specColor, half rlPow4, half oneMinusRoughness)
		{
			// Lookup texture to save instructions
			half specular = tex2D(_LUT, half2(rlPow4 * oneMinusRoughness, 0)).a;
			return diffColor + specular * specColor;
		}

		half4 BRDF3_Unity_PBS_Toon (half3 diffColor, half3 specColor, half oneMinusReflectivity, half oneMinusRoughness,
			half3 normal, half3 viewDir,
			UnityLight light, UnityIndirect gi)
		{
			half3 reflDir = reflect (viewDir, normal);

			half nl = light.ndotl;
			half nv = DotClamped (normal, viewDir);

			// Vectorize Pow4 to save instructions
			half2 rlPow4AndFresnelTerm = Pow4 (half2(dot(reflDir, light.dir), 1-nv));  // use R.L instead of N.H to save couple of instructions
			half rlPow4 = rlPow4AndFresnelTerm.x; // power exponent must match kHorizontalWarpExp in NHxRoughness() function in GeneratedTextures.cpp
			half fresnelTerm = rlPow4AndFresnelTerm.y;

			half grazingTerm = saturate(oneMinusRoughness + (1-oneMinusReflectivity));

			half3 color = BRDF3_Direct_Toon(diffColor, specColor, rlPow4, oneMinusRoughness);
			color *= light.color * nl;

			// Indirect (no env reflections)
			color += gi.diffuse * diffColor;

			// Rim
			color += _HighlightColour * fresnelTerm;

			return half4(color, 1);
		}

		inline half4 LightingCustomStandard (SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
		{
			s.Normal = normalize(s.Normal);

			// Toon light
			half ndotl = dot(gi.light.dir, s.Normal);
			ndotl = (ndotl * 0.5) + 0.5;
			gi.light.ndotl = tex2D(_LUT, half2(ndotl, 1)).a;

			half oneMinusReflectivity;
			half3 specColor;
			s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

			// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
			// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
			half outputAlpha;
			s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

			half4 c = BRDF3_Unity_PBS_Toon (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
			c.a = outputAlpha;
			return c;
		}

		inline void LightingCustomStandard_GI (
			SurfaceOutputStandard s,
			UnityGIInput data,
			inout UnityGI gi)
		{
			UNITY_GI(gi, s, data);
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
#ifdef _ALBEDOMAP
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
#else
			// Albedo is just colour value
			fixed4 c = _Color;
#endif
			c += tex2D(_HighlightTex, IN.glowUV).a * _HighlightColour;
			o.Albedo = c.rgb;

#ifdef _NORMALMAP
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
#endif

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}

	FallBack "Diffuse"
	CustomEditor "Tanks.Editor.HalfLambertPowerupShaderGUI"
}
