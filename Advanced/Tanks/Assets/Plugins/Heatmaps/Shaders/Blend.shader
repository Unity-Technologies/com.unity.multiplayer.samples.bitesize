
Shader "Heatmaps/Particles/AlphaBlend" {
    Category {
     BindChannels { 
         Bind "Color", color 
         Bind "Vertex", vertex
     }
     SubShader { 
         Lighting Off 
         Fog { Mode Off }
         Blend SrcAlpha OneMinusSrcAlpha
         Cull Off
         Tags { Queue = Transparent }
         Pass { }
     }
 }
}
