// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexLit With Shadows" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader 
	{
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque"}

		Pass 
		{
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma fragmentoption ARB_precision_hint_fastest
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			fixed4 _Color;

			struct v2f
			{
				half4   pos         : SV_POSITION;
				fixed4  col         : TEXCOORD0;
				fixed4  ambient     : TEXCOORD1;
				LIGHTING_COORDS(2,3)
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				
				o.pos = UnityObjectToClipPos( v.vertex);
				o.col = _Color * v.color;

				o.ambient = UNITY_LIGHTMODEL_AMBIENT * o.col;

				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 lightDir = normalize(UnityWorldSpaceLightDir(o.pos));
				fixed nl = DotClamped(worldNormal, lightDir);
				o.col.rgb *= nl * _LightColor0.rgb;

				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed atten = SHADOW_ATTENUATION(i); // Shadows ONLY.
				return atten * i.col + i.ambient;
			}
			ENDCG
		}
	}
	FallBack "VertexLit"
}