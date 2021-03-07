Shader "Everyplay/Crossfade" {
	Properties {
		_MainTex ("RGBA Texture Image", 2D) = "black" {}
		_MainTex2 ("RGBA Texture Image", 2D) = "black" {}
		_Blend ( "Blend", Range ( 0, 1 ) ) = 0.0
	}
	SubShader {
		Tags { "Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha 
			Cull back ZWrite off Lighting Off Fog { Mode Off }

			GLSLPROGRAM
				uniform sampler2D _MainTex;
				uniform sampler2D _MainTex2;
				uniform float _Blend;
				uniform mediump vec4 _MainTex_ST;
				#ifdef VERTEX
				varying vec2 textureCoordinates;
				void main()
				{
					textureCoordinates =  gl_MultiTexCoord0.xy * _MainTex_ST.xy;
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				}
				#endif

				#ifdef FRAGMENT
				varying vec2 textureCoordinates;
				void main()
				{
					gl_FragColor = texture2D(_MainTex, vec2(textureCoordinates)) * (1.0 - _Blend) + texture2D(_MainTex2, vec2(textureCoordinates)) * _Blend;
					gl_FragColor.a = 1.0;
				}
				#endif
			ENDGLSL
		}
	}
	FallBack "Unlit/Texture"
}
