Shader "Aperture/InstancingShellBasedFur"
{
    Properties
    {
        [MainTexture]_BaseMap("Base Color", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.2
        [NoScaleOffset]_FurPatternMap("Fur Pattern", 2D) = "white" {}
        _FurPatterScale("Fur Pattern Scale", Float) = 20.0
        _FurLength("Fur Length", Range(0.0, 1.0)) = 0.05
        _Thickness("Fur Thickness", Float) = 1.0
        _BaseMove("Fur Orientation", Vector) = (1.0, 1.0, 1.0, 1.0)
        _Occlusion("Occlusion", Range(0.0, 1.0)) = 0.0


        [Enum(None, 0, Front, 1, Back, 2)]_Cull("CullMode", float) = 2.0
        [Enum(Off, 0, On, 1)]_ZWrite ("ZWrite", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "ShaderModel" = "4.5"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Cull[_Cull]
            ZWrite[_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/FurLighting.hlsl"

            #pragma target 4.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            // #pragma multi_compile_fragment _ _LIGHT_LAYERS
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            // #pragma multi_compile _ _CLUSTERED_RENDERING

            // -------------------------------------
            // Unity defined keywords
            // #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            // #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            // #pragma multi_compile _ LIGHTMAP_ON
            // #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            //#pragma multi_compile_fragment _ DEBUG_DISPLAY


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            half4 _BaseMap_ST;
            TEXTURE2D(_FurPatternMap);
            SAMPLER(sampler_FurPatternMap);

            half4 _BaseColor;
            half4 _SpecularColor;
            half _Smoothness;
            half _FurLength;
            half _LayerCount;
            half _FurPatterScale;
            half4 _BaseMove;
            half _Occlusion;
            half _Thickness;

            float4x4 _ObjectToWorld;
            float4x4 _WorldToObject;

            void Setup()
            {
                unity_ObjectToWorld = _ObjectToWorld;
                unity_WorldToObject = _WorldToObject;
            }

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 normal   : NORMAL;
                float4 color   : COLOR;
                float2 texcoord     : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                    float4 positionCS   : SV_POSITION;
                    float2 uv           : TEXCOORD0;
                    float3 mixData         : TEXCOORD1;//x=layer  y=Thickness  z=smoothness(exp2)
                    float3 normalWS     : NORMAL;
                    float3 positionWS   : TEXCOORD2;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord 			: TEXCOORD3;
                #endif
                half    fogCoord                : TEXCOORD4;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half4 fogFactorAndVertexLight  : TEXCOORD5; // x: fogFactor, yzw: vertex light
                #else
                    half  fogFactor                 : TEXCOORD6;
                #endif
            };
            Varyings LitPassVertex(Attributes input)
            {
                Setup();
                Varyings output = (Varyings)0;
                float3 positionOS = input.positionOS.xyz;
                half3 normalOS = input.normal.xyz;
                float2 uv = input.texcoord;
                half4 color = input.color;

                output.mixData.x = (input.instanceID + 1.0) / (_LayerCount + 1.0);
                output.mixData.y = pow(saturate(output.mixData.x), _Thickness);
                output.mixData.z = exp2(10 * _Smoothness + 1);
                output.normalWS = TransformObjectToWorldNormal(normalOS.xyz);
                half moveFactor = output.mixData.x;//pow(output.layer, _BaseMove.w);
                half3 dir = normalize(normalOS.xyz + color.rgb * _BaseMove.xyz * moveFactor);
                half distance = (output.mixData.x) * _FurLength * max(color.a, 0.01);
                float3 positoinWS = mul(unity_ObjectToWorld, float4(positionOS + dir * distance, 1.0)).xyz;
                output.positionCS = TransformWorldToHClip(positoinWS);
                output.uv = uv;
                output.positionWS = positoinWS;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
            #endif

            #if defined(_FOG_FRAGMENT)
                half fogFactor = 0;
            #else
                half fogFactor = ComputeFogFactor(output.positionCS.z);
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3 vertexLight = VertexLighting(output.positionWS, output.normalWS);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
            #else
                output.fogFactor = fogFactor;
            #endif
                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                float4 shadowCoords = float4(0, 0, 0, 0);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                shadowCoords = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                shadowCoords = TransformWorldToShadowCoord(input.positionWS);
            #endif
                Light light = GetMainLight(shadowCoords);

                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw).rgb * _BaseColor.rgb;
                half fur =  SAMPLE_TEXTURE2D(_FurPatternMap, sampler_FurPatternMap, input.uv * _FurPatterScale).r;
                half AO = saturate(lerp(1.0, input.mixData.x + 0.2, _Occlusion));
                half shadow = light.shadowAttenuation * AO;
                half3 vertexLighting = 0.0;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                input.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                vertexLighting = input.fogFactorAndVertexLight.yzw;
            #else
                input.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                vertexLighting = half3(0, 0, 0);
            #endif
                FurSurfaceData furSurfaceData;
                InitializeFurSurfaceData(furSurfaceData, baseColor, _SpecularColor, input.mixData.z, light, fur, AO, shadow, vertexLighting);
                half3 final = FurLighting(furSurfaceData, input.normalWS, input.positionWS);
                final = MixFog(final, input.fogCoord);
                half alpha = saturate(fur - input.mixData.y);
                return half4(final, alpha);
            }
            ENDHLSL
        }
    }
}