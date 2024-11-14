#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

//#if defined(SHADERGRAPH_PREVIEW)
//#else
//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
//#pragma multi_compile_fragment _ _SHADOWS_SOFT
//#pragma multi_compile _ SHADOWS_SHADOWMASK
//#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
//#pragma multi_compile _ LIGHTMAP_ON
//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
//#endif

void MainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out float ShadowAtten)
{
#if defined(SHADERGRAPH_PREVIEW)
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
    ShadowAtten = 1;
#else
	float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);

    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;

	//#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
	//	ShadowAtten = 1.0h;
    //#else
	    //ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
	    //float shadowStrength = GetMainLightShadowStrength();
	    //ShadowAtten = SampleShadowmap(shadowCoord, TEXTURE2D_ARGS(_MainLightShadowmapTexture,
	    //sampler_MainLightShadowmapTexture),
	    //shadowSamplingData, shadowStrength, false);

        ShadowAtten = mainLight.shadowAttenuation;
    //#endif
#endif
}


void DirectSpecular_float(float Smoothness, float3 Direction, float3 WorldNormal, float3 WorldView, out float3 Out)
{
    float4 White = 1;

#if defined(SHADERGRAPH_PREVIEW)
    Out = 0;
#else
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);
    Out = LightingSpecular(White, Direction, WorldNormal, WorldView, White, Smoothness);
#endif
}

void AdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, out float3 Diffuse, out float3 Specular)
{
    float3 diffuseColor = 0;
    float3 specularColor = 0;
    float4 White = 1;

    #if !defined(SHADERGRAPH_PREVIEW)

    #if defined(_ADDITIONAL_LIGHTS)
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, WorldPosition);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, White, Smoothness);
    }
    #endif

    //InputData is used in the LIGHT_LOOP_BEGIN macro define in RealtimeLights.hlsl
    InputData inputData;
    inputData.positionWS = WorldPosition;
    inputData.normalWS = 0;
    inputData.viewDirectionWS = 0;
    inputData.shadowCoord = 0;
    inputData.fogCoord = 0;
    inputData.vertexLighting = 0;
    inputData.bakedGI = 0;
    float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
    inputData.shadowMask = 0;

    LIGHT_LOOP_BEGIN(pixelLightCount)
    Light light = GetAdditionalLight(lightIndex, WorldPosition);
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
    specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, White, Smoothness);
    LIGHT_LOOP_END

    #endif

    #endif

    Diffuse = diffuseColor;
    Specular = specularColor;
}

#endif
