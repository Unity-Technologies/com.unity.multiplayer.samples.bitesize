// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/WorldFont"
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags 
		{
			"RenderType"="Transparent"
			"Queue"="Transparent"
		}

		CGINCLUDE
		#include "UnityCG.cginc"

		fixed4 _Color;
		sampler2D _MainTex;
		half4 _MainTex_ST;

		struct v2f 
		{
			half4   pos     : SV_POSITION;
			half2   uv      : TEXCOORD0;
		};

		v2f vert(appdata_full v)
		{
			v2f o = (v2f)0;

			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.pos = UnityObjectToClipPos(v.vertex);

			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			return fixed4(_Color.rgb, tex2D(_MainTex, i.uv).a);
		}
		ENDCG

		Pass
		{
			LOD 200
	
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
			ENDCG
		}
	}
}
