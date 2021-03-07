// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/CratePoster"
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Background Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		CGINCLUDE
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"
		#pragma multi_compile_fwdbase

		fixed4 _Color;
		sampler2D _MainTex;
		half4 _MainTex_ST;

		struct v2f 
		{
			half4   pos     : SV_POSITION;
			fixed4  col     : TEXCOORD0;
			fixed4  bgCol   : TEXCOORD1;
			fixed4  ambient : TEXCOORD2;
			half2   uv      : TEXCOORD3;
			LIGHTING_COORDS(4, 5)
		};

		v2f vert(appdata_full v)
		{
			v2f o = (v2f)0;

			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.col = v.color;

			o.ambient = UNITY_LIGHTMODEL_AMBIENT * o.col;

			fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
			fixed3 lightDir = normalize(UnityWorldSpaceLightDir(o.pos));
			fixed nl = DotClamped(worldNormal, lightDir);
			o.col.rgb *= nl * _LightColor0.rgb;

			o.bgCol = o.col * _Color; // BG Colour is lighting * _Color

			TRANSFER_VERTEX_TO_FRAGMENT(o);
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			fixed atten = SHADOW_ATTENUATION(i); // Shadows ONLY.
			fixed4 tex = tex2D(_MainTex, i.uv) * i.col;

			tex.rgb *= tex.a;
			tex.rgb += i.bgCol.rgb * (1 - tex.a);

			return tex * atten + i.ambient;
		}
		ENDCG

		Pass
		{
			LOD 200
			Tags 
			{
				"RenderType"="Opaque"
				"LightMode"="ForwardBase"
				"Queue"="Geometry"
			}
			Blend One Zero
			ZWrite On

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
			ENDCG
		}
	}
}
