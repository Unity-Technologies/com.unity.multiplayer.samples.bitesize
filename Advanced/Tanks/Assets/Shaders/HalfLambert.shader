Shader "Custom/HalfLambert" {
	Properties {
		_MainTex("Main Tex", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
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
		#pragma surface surf CustomStandard fullforwardshadows
		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _ALBEDOMAP

		#include "UnityPBSLighting.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input {
			half2 uv_MainTex;
			half2 uv_BumpMap;
		};

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _LUT;
		fixed4 _Color;
		half _Metallic;
		half _Glossiness;

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

			// Vectorize Pow4 to save instructions
			half2 rlPow4AndFresnelTerm = Pow4 (dot(reflDir, light.dir));  // use R.L instead of N.H to save couple of instructions
			half rlPow4 = rlPow4AndFresnelTerm.x; // power exponent must match kHorizontalWarpExp in NHxRoughness() function in GeneratedTextures.cpp

			half grazingTerm = saturate(oneMinusRoughness + (1-oneMinusReflectivity));

			half3 color = BRDF3_Direct_Toon(diffColor, specColor, rlPow4, oneMinusRoughness);
			color *= light.color * nl;

			// Indirect (no env reflections)
			color += gi.diffuse * diffColor;

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
	CustomEditor "Tanks.Editor.HalfLambertShaderGUI"
}
