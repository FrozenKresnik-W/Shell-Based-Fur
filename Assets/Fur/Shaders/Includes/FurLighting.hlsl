#ifndef FUR_LIGHTING_INCLUDED
#define FUR_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct FurSurfaceData
{
    half3 baseColor;
    half4 specular;
    half smoothness;
    Light light;
    half fur;
    half occlusion;
    half shadow;
    half3 vertexLighting;
};

inline void InitializeFurSurfaceData(out FurSurfaceData outSurfaceData, half3 baseColor, half4 specular, half smoothness, Light light, half fur, half occlusion, half shadow, half3 vertexLighting)
{
    outSurfaceData.baseColor = baseColor;
    outSurfaceData.specular = specular;
    outSurfaceData.smoothness = smoothness;
    outSurfaceData.light = light;
    outSurfaceData.fur = fur;
    outSurfaceData.occlusion = occlusion;
    outSurfaceData.shadow = shadow;
    outSurfaceData.vertexLighting = vertexLighting;
}

half3 AdditionalLights(half3 additionalLightsColor, half3 albedo, half3 positionWS, half3 normalWS)
{
    uint pixelLightCount = GetAdditionalLightsCount();
    uint meshRenderingLayers = GetMeshRenderingLightLayer();

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light additionalLight =  GetAdditionalLight(lightIndex, positionWS);
    if (IsMatchingLightLayer(additionalLight.layerMask, meshRenderingLayers))
    {
        half3 attenuatedLightColor = additionalLight.color * (additionalLight.distanceAttenuation * additionalLight.shadowAttenuation);
        half3 lightColor = LightingLambert(attenuatedLightColor, additionalLight.direction, normalWS);
        lightColor *= albedo;
        additionalLightsColor += lightColor;
    }
    LIGHT_LOOP_END

    return additionalLightsColor;
}
half3 FurLighting(FurSurfaceData furSurfaceData, half3 normalWS, half3 positionWS)
{
    half3 N = normalize(normalWS);
    half3 V = GetWorldSpaceNormalizeViewDir(positionWS);
    float3 L = furSurfaceData.light.direction;
    half3 baseColor = furSurfaceData.baseColor;
    half AO = furSurfaceData.occlusion;
    half shadow = furSurfaceData.light.shadowAttenuation * AO;
    half3 additionalLightsColor = 0.0;

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    additionalLightsColor += furSurfaceData.vertexLighting;
#else
    #if defined(_ADDITIONAL_LIGHTS)
    additionalLightsColor += AdditionalLights(additionalLightsColor, baseColor, positionWS, normalWS);
    #endif
#endif

    half fresnel = 1.0 - max(0.0, dot(N, V));
    half rimLight = fresnel * fresnel;
    half3 backLight = smoothstep(0.5, 1.0, dot(-V, L+N)) * furSurfaceData.specular.rgb;
    half3 lightingSpecular = LightingSpecular(furSurfaceData.light.color, furSurfaceData.light.direction, N, V, furSurfaceData.specular, furSurfaceData.smoothness);
    half3 directLightingDiffuse = LightingLambert(furSurfaceData.light.color, L, N) * baseColor.rgb * shadow + additionalLightsColor;
    half3 directLightingSpecular = saturate((lightingSpecular + backLight) * furSurfaceData.light.color * furSurfaceData.light.color * shadow + rimLight * 0.2);
    half3 indirectLightingDiffuse = SampleSH(N) * baseColor.rgb;
    half3 finalLighting = directLightingDiffuse + directLightingSpecular + indirectLightingDiffuse;
    return finalLighting;
}
#endif
