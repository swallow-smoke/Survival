Shader "Survival/Toon/DOTS Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Toon Shadow)]
        _ShadowColor("Shadow Color", Color) = (0.45, 0.5, 0.6, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0, 0.25)) = 0.025

        [Header(Ambient)]
        _AmbientColor("Ambient Color", Color) = (0.12, 0.12, 0.14, 1)
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.25

        [Header(Toon Specular)]
        _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecThreshold("Specular Threshold", Range(0, 1)) = 0.92
        _SpecSoftness("Specular Softness", Range(0.001, 0.25)) = 0.02
        _SpecStrength("Specular Strength", Range(0, 4)) = 0.5

        [Header(Rim Light)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.65
        _RimSoftness("Rim Softness", Range(0.001, 0.5)) = 0.1
        _RimStrength("Rim Strength", Range(0, 4)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        // =========================================================
        // Forward Toon Pass
        // =========================================================
        Pass
        {
            Name "ToonForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

            // GPU / DOTS instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // URP shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;

                float4 _BaseColor;

                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSoftness;

                float4 _AmbientColor;
                float _AmbientStrength;

                float4 _SpecColor;
                float _SpecThreshold;
                float _SpecSoftness;
                float _SpecStrength;

                float4 _RimColor;
                float _RimThreshold;
                float _RimSoftness;
                float _RimStrength;

            CBUFFER_END

            // ---------------------------------------------------------
            // ECS per-instance properties
            // ---------------------------------------------------------

            #ifdef UNITY_DOTS_INSTANCING_ENABLED

                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)

                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                    UNITY_DOTS_INSTANCED_PROP(float4, _ShadowColor)

                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)

            #endif

            float4 GetToonBaseColor()
            {
                #ifdef UNITY_DOTS_INSTANCING_ENABLED
                    return UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(
                        float4,
                        _BaseColor
                    );
                #else
                    return _BaseColor;
                #endif
            }

            float4 GetToonShadowColor()
            {
                #ifdef UNITY_DOTS_INSTANCING_ENABLED
                    return UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(
                        float4,
                        _ShadowColor
                    );
                #else
                    return _ShadowColor;
                #endif
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;

                float fogFactor   : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                float3 normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS =
                    TransformWorldToHClip(positionWS);

                output.positionCS = positionCS;
                output.positionWS = positionWS;
                output.normalWS = normalWS;

                output.uv =
                    input.uv * _BaseMap_ST.xy +
                    _BaseMap_ST.zw;

                output.fogFactor =
                    ComputeFogFactor(positionCS.z);

                return output;
            }

            half4 ToonFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // -----------------------------------------------------
                // Surface
                // -----------------------------------------------------

                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );

                half4 baseColor = GetToonBaseColor();

                half3 albedo =
                    textureColor.rgb *
                    baseColor.rgb;

                float3 normalWS =
                    normalize(input.normalWS);

                float3 viewDirectionWS =
                    SafeNormalize(
                        GetCameraPositionWS() -
                        input.positionWS
                    );

                // -----------------------------------------------------
                // Main light
                // -----------------------------------------------------

                float4 shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS
                    );

                Light mainLight =
                    GetMainLight(shadowCoord);

                float3 lightDirectionWS =
                    normalize(mainLight.direction);

                half NdotL =
                    saturate(
                        dot(
                            normalWS,
                            lightDirectionWS
                        )
                    );

                // -----------------------------------------------------
                // Toon band
                // -----------------------------------------------------

                half shadowSoftness =
                    max(
                        (half)_ShadowSoftness,
                        0.0001h
                    );

                half toonBand =
                    smoothstep(
                        _ShadowThreshold - shadowSoftness,
                        _ShadowThreshold + shadowSoftness,
                        NdotL
                    );

                half attenuation =
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                toonBand *= attenuation;

                half3 toonShadowColor =
                    albedo *
                    GetToonShadowColor().rgb;

                half3 toonLightColor =
                    albedo *
                    mainLight.color;

                half3 color =
                    lerp(
                        toonShadowColor,
                        toonLightColor,
                        saturate(toonBand)
                    );

                // -----------------------------------------------------
                // Ambient
                // -----------------------------------------------------

                color +=
                    albedo *
                    _AmbientColor.rgb *
                    _AmbientStrength;

                // -----------------------------------------------------
                // Toon Specular
                // -----------------------------------------------------

                float3 halfDirectionWS =
                    SafeNormalize(
                        lightDirectionWS +
                        viewDirectionWS
                    );

                half NdotH =
                    saturate(
                        dot(
                            normalWS,
                            halfDirectionWS
                        )
                    );

                half specSoftness =
                    max(
                        (half)_SpecSoftness,
                        0.0001h
                    );

                half specular =
                    smoothstep(
                        _SpecThreshold - specSoftness,
                        _SpecThreshold + specSoftness,
                        NdotH
                    );

                specular *=
                    attenuation *
                    saturate(toonBand);

                color +=
                    _SpecColor.rgb *
                    specular *
                    _SpecStrength;

                // -----------------------------------------------------
                // Rim Light
                // -----------------------------------------------------

                half NdotV =
                    saturate(
                        dot(
                            normalWS,
                            viewDirectionWS
                        )
                    );

                half rimValue =
                    1.0h - NdotV;

                half rimSoftness =
                    max(
                        (half)_RimSoftness,
                        0.0001h
                    );

                half rim =
                    smoothstep(
                        _RimThreshold,
                        _RimThreshold + rimSoftness,
                        rimValue
                    );

                color +=
                    _RimColor.rgb *
                    rim *
                    _RimStrength;

                // -----------------------------------------------------
                // Fog
                // -----------------------------------------------------

                color =
                    MixFog(
                        color,
                        input.fogFactor
                    );

                return half4(
                    color,
                    textureColor.a *
                    baseColor.a
                );
            }

            ENDHLSL
        }

        // =========================================================
        // Shadow Caster
        // =========================================================
        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVertex(
                ShadowAttributes input
            )
            {
                ShadowVaryings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );

                float3 lightDirectionWS;

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    lightDirectionWS =
                        normalize(
                            _LightPosition -
                            positionWS
                        );

                #else

                    lightDirectionWS =
                        _LightDirection;

                #endif

                float4 positionCS =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            positionWS,
                            normalWS,
                            lightDirectionWS
                        )
                    );

                #if UNITY_REVERSED_Z

                    positionCS.z =
                        min(
                            positionCS.z,
                            positionCS.w *
                            UNITY_NEAR_CLIP_VALUE
                        );

                #else

                    positionCS.z =
                        max(
                            positionCS.z,
                            positionCS.w *
                            UNITY_NEAR_CLIP_VALUE
                        );

                #endif

                output.positionCS =
                    positionCS;

                return output;
            }

            half4 ShadowFragment(
                ShadowVaryings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}