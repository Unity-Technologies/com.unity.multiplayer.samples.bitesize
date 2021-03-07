// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Particles/Shadows/Blended Premultiply" 
{
	Properties 
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_Cutoff ("Shadow Cutoff", Float) = 0.8
	}

	Category 
	{
		Tags
		{
			"Queue"="Transparent"
		}
		SubShader 
		{
			Pass 
			{
				Tags 
				{
					"IgnoreProjector"="True"
					"RenderType"="Transparent"
				}
				Blend One OneMinusSrcAlpha 
				Cull Off
				ZWrite Off

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_particles

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				fixed4 _TintColor;
				
				struct appdata_t {
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
				};
				
				float4 _MainTex_ST;

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color * _TintColor;
					o.texcoord = v.texcoord;
					return o;
				}
				
				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 result = i.color * tex2D(_MainTex, i.texcoord);
					result.rgb *= i.color.a;

					return result;
				}
				ENDCG 
			}

			// Pass to render object as a shadow caster
			Pass 
			{
				Name "Caster"
				Tags { "LightMode" = "ShadowCaster" }
				
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_shadowcaster
				#include "UnityCG.cginc"

				// #define UNITY_STANDARD_USE_DITHER_MASK 1

				struct v2f
				{ 
					V2F_SHADOW_CASTER_NOPOS
					half2  tex : TEXCOORD1;
				};

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform fixed _Cutoff;

				// Dithered shadows from Standard shadow caster
				#ifdef UNITY_STANDARD_USE_DITHER_MASK
				sampler3D	_DitherMaskLOD;
				#endif

				void vert( appdata_base v,
					out v2f o,
					out half4 opos : SV_POSITION )
				{
					TRANSFER_SHADOW_CASTER_NOPOS(o, opos)
					o.tex = v.texcoord;
				}

				float4 frag
				( 
					v2f i
					#ifdef UNITY_STANDARD_USE_DITHER_MASK
					, UNITY_VPOS_TYPE vpos : VPOS
					#endif
				) : SV_Target
				{
					fixed texAlpha = tex2D( _MainTex, i.tex ).a * _TintColor.a;

					#if defined(UNITY_STANDARD_USE_DITHER_MASK)
					half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,texAlpha*0.9375)).a;
					clip (alphaRef - 0.01);
					#else
					clip (texAlpha - _Cutoff );
					#endif
					
					SHADOW_CASTER_FRAGMENT(i)
				}
				ENDCG

			}
		}
	}
}
