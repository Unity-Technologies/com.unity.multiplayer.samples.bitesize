// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Particles/Alpha Blended Premultiply Coloured Fres" 
{
	Properties 
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_ScaleFactor ("Scale factor", Float) = 1
		_FresPow ("Fresnel power", Float) = 4
	}

	Category 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend One OneMinusSrcAlpha 
		Cull Off
		Lighting Off
		ZWrite Off

		SubShader 
		{
			Pass 
			{
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_particles

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				fixed4 _TintColor;
				half _ScaleFactor, _FresPow;
				
				struct appdata_t {
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					float3 normal: NORMAL;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					half fres : COLOR1;
				};
				
				float4 _MainTex_ST;

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color * _TintColor;
					o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

					half3 worldNorm = normalize(mul((half3x3)unity_ObjectToWorld, v.normal));
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					half3 viewDir = normalize(_WorldSpaceCameraPos - worldPos.xyz);
					o.fres = pow(saturate(dot(worldNorm, viewDir)), _FresPow);

					return o;
				}
				
				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 result = i.color * tex2D(_MainTex, i.texcoord);
					result.rgb *= i.color.a;
					result *= i.fres * _ScaleFactor;
					return result;
				}
				ENDCG 
			}
		}
	}
}
