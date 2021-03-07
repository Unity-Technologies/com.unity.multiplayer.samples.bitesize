Shader "Custom/CratePaint"
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Alpha ("Alpha",Range(0,1)) = 0.35
		_Emission ("Emission",Range(0,1)) = 0.1
	}

	SubShader 
	{
		Tags { "Queue"="Geometry+50" "RenderType"="Opaque" }
		LOD 300
		
		CGPROGRAM
		#pragma surface surf Lambert decal:blend

		sampler2D _MainTex;
		fixed4 _Color;
		float _Alpha;
		float _Emission;

		struct Input 
		{
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

			o.Albedo.rgb = _Color.xyz;
			o.Alpha = c.a - _Alpha;
			o.Emission = _Color.rgb * _Emission;
		}
		ENDCG
	}

	FallBack "Legacy Shaders/Diffuse"
}