Shader "Custom/OutlineShadow"
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)
		_OutlineWidth ("Thickness", Float) = 1
	}

	SubShader
	{
		CGINCLUDE
		#include "UnityCG.cginc"

		fixed4 _Color;
		float _OutlineWidth;

		struct v2f 
		{
			float4 pos: SV_POSITION;
		};


		v2f vert(appdata_base v)
		{
			v2f o = (v2f)0;

			// Into view space first
			float4 projPos = UnityObjectToClipPos(v.vertex);//mul((float4x4)UNITY_MATRIX_MVP, (float4)v.vertex);
			float2 projNorm = mul((float3x3)UNITY_MATRIX_IT_MV, (float3)v.normal).xy;
			projNorm = normalize(mul((float2x2)UNITY_MATRIX_P, projNorm));

			// For perspective
			// UNITY_MATRIX_P[3][3] is 1 for ortho
			projNorm *= max(projPos.z, UNITY_MATRIX_P[3][3]);

			projPos.xy += (projNorm.xy * (_ScreenParams.zw - 1) * 2 * _OutlineWidth);

			o.pos = projPos;
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			return _Color;
		}
		ENDCG

		Pass
		{
			LOD 200
			Tags 
			{
				"RenderType"="Opaque"
				"LightMode"="ForwardBase"
				"Queue"="Geometry-100"
			}
			Blend One Zero
			Cull Front
			ZWrite On

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
			ENDCG
		}

		//use Unity built-in shadow caster pass from legacy vertex shader
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
